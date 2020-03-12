﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public partial class Person
    {
        public string firstName;
        public bool isMale = Eleven.random.Next(2) == 0;
        public List<Title> titles = new List<Title>();
        public TitleLanded title_land;
        public Society society;
        //public SavableMap_Person_RelObj relations = new SavableMap_Person_RelObj();
        public Dictionary<Person, RelObj> relations = new Dictionary<Person, RelObj>();
        public double prestige = 1;
        public double targetPrestige = 1;
        public int lastVoteProposalTurn;
        //public VoteIssue lastProposedIssue;
        public GraphicalSlot outer;
        public List<ThreatItem> threatEvaluations = new List<ThreatItem>();
        public LogBox log;
        public double evidence;
        public double shadow;
        public int imgIndBack = -1;
        public int imgIndFore = -1;
        public int maxSanity = 10;
        public double sanity = 0;
        public List<Trait> traits = new List<Trait>();

        public ThreatItem threat_enshadowedNobles;

        public double politics_militarism;

        public enum personState { normal,enthralled,broken};
        public personState state = personState.normal;
        public bool isDead;
        public Society rebellingFrom;

        public Insanity madness;

        public Person(Society soc)
        {
            this.society = soc;
            firstName = TextStore.getName(isMale);
            madness = soc.map.globalist.madness_sane;

            maxSanity = Eleven.random.Next(map.param.insanity_maxSanity);
            sanity = maxSanity;

            if (World.logging)
            {
                log = new LogBox(this);
            }

            politics_militarism = Math.Pow(Eleven.random.NextDouble(), 0.5);//Bias towards 0
            politics_militarism = 1 - politics_militarism;//0 to 1, bias towards 1
            politics_militarism = (2*politics_militarism)-1;//1 to -1, bias towards 1

            //Add permanent threats
            threat_enshadowedNobles = new ThreatItem(map,this);
            threat_enshadowedNobles.form = ThreatItem.formTypes.ENSHADOWED_NOBLES;
            threat_enshadowedNobles.responseCode = ThreatItem.RESPONSE_DARKNESSWITHIN;
            threatEvaluations.Add(threat_enshadowedNobles);

            
            for (int i = 0; i < 3; i++)
            {
                if (Eleven.random.Next(2) == 0) { break; }//50% chance to add another trait

                Trait add = map.globalist.getTrait(this);
                if (add == null) { break; }
                traits.Add(add);
            }
        }

        public void turnTick()
        {
            if (World.logging) { log.takeLine("---------Turn " + map.turn + "------------"); }

            processEnshadowment();

            this.targetPrestige = map.param.person_defaultPrestige;
            if (title_land != null)
            {
                targetPrestige += title_land.settlement.getPrestige();
            }
            foreach (Title t in titles) { targetPrestige += t.getPrestige(); }
            if (Math.Abs(prestige-targetPrestige) < map.param.person_prestigeDeltaPerTurn)
            {
                prestige = targetPrestige;
            }
            else if (prestige < targetPrestige) { prestige += map.param.person_prestigeDeltaPerTurn; }
            else if (prestige > targetPrestige) { prestige -= map.param.person_prestigeDeltaPerTurn; }
            
            foreach (RelObj rel in relations.Values) {
                rel.turnTick(this, rel.them);
            }

            List<Title> rems = new List<Title>();
            foreach (Title t in titles)
            {
                if (t.heldBy != this || t.society != this.society)
                {
                    rems.Add(t);
                }
            }
            foreach (Title t in rems) { titles.Remove(t); }

            computeSuspicionGain();
            processThreats();
            processSanity();
        }

        public void processSanity()
        {
            if (madness == map.globalist.madness_sane) {
                if (sanity <= 0)
                {
                    goInsane();
                }
                else
                {
                    sanity += map.param.insanity_sanityRegen;
                    if (sanity > maxSanity)
                    {
                        sanity = maxSanity;
                    }
                }
            }
            madness.process(this);
        }
        public void goInsane()
        {
            int q = Eleven.random.Next(map.globalist.allInsanities.Count);
            madness = map.globalist.allInsanities[q];
            map.addMessage(this.getFullName() + " has gone insane, and is now " + madness.name,MsgEvent.LEVEL_DARK_GREEN2,true);
        }

        private void computeSuspicionGain()
        {
            foreach (SocialGroup sg in map.socialGroups)
            {
                if (sg is Society == false) { continue; }
                Society soc = (Society)sg;
                foreach (Person p in soc.people)
                {
                    if (p == this) { continue; }
                    double infoAvail = map.getInformationAvailability(this.getLocation(), sg);
                    RelObj rel = getRelation(p);
                    double evidenceMult = Math.Pow(p.evidence, map.param.person_evidenceExponent);//Make low evidence a bit slower to cause suspicion
                    double fromTraits = 1;
                    foreach (Trait t in traits) { fromTraits *= t.suspicionMult(); }
                    rel.suspicion += infoAvail * evidenceMult * map.param.person_suspicionPerEvidence * fromTraits;
                }
            }
        }

        public Location getLocation()
        {
            if (this.title_land != null)
            {
                return this.title_land.settlement.location;
            }
            return this.society.getCapital();
        }

        private void processEnshadowment()
        {
            evidence += shadow * map.param.person_evidencePerShadow;
            if (evidence > 1)
            {
                evidence = 1;
            }
            if (state != personState.broken && state != personState.enthralled) {
                shadow -= map.param.person_shadowDecayPerTurn;
                if (shadow < 0) { shadow = 0; }
            }
            foreach (Person p in society.people)
            {
                if (p == this) { continue; }
                if (p.shadow == 0) { continue; }//Can't inherit if they don't have any, skip to save CPU
                if (p.shadow <= shadow) { continue; }
                if (p.prestige < prestige) { continue; }

                /*
                double basePrestige = 100;
                if (society.getSovreign() != null) { basePrestige = society.getSovreign().prestige; }
                if (basePrestige < 10) { basePrestige = 10; }
                double multFromPrestige = p.prestige / basePrestige;
                if (multFromPrestige < 0) { multFromPrestige = 0; }
                if (multFromPrestige > 1) { multFromPrestige = 1; }
                */

                double likingMult = Math.Max(0, this.getRelation(p).getLiking())/100;

                
                double shadowDelta = p.shadow * likingMult * map.param.person_shadowContagionMult;//You get enshadowed by people you like/trust
                this.shadow = Math.Min(p.shadow, shadow + shadowDelta);//Don't exceed your donor's shadow
                if (this.shadow > 1) { this.shadow = 1; }
            }
            if (society.isDarkEmpire)
            {
                //if (society.getSovreign() != null && society.getSovreign().shadow > shadow)
                //{
                    shadow += Eleven.random.NextDouble()*map.param.ability_avrgDarkEmpireShadowPerTurn;
                //}
                if (shadow > 1) { shadow = 1; }
            }

            if (state == personState.normal && shadow == 1)
            {
                this.state = personState.broken;
                map.turnMessages.Add(new MsgEvent(this.getFullName() + " has been fully enshadowed, their soul can no longer resist the dark", MsgEvent.LEVEL_GREEN,true));
            }
        }

        public bool enthrallable()
        {
            if (state == personState.broken) { return true; }

            Society soc = society;
            double minPrestige = 1000000;
            foreach (Person p in soc.people)
            {
                if (p.title_land == null) { continue; }
                if (p.prestige < minPrestige)
                {
                    minPrestige = p.prestige;
                }
            }

            //return soc.getEnthrallables().Contains(hex.location.settlement.title.heldBy);
            return prestige < (1 + minPrestige);
        }
        public void computeThreats()
        {
            //Actually do the evaluations here
            foreach (ThreatItem item in threatEvaluations)
            {
                item.threat = 0;
                item.reasons.Clear();
                if (item.group == null)
                {
                    if (item.form == ThreatItem.formTypes.ENSHADOWED_NOBLES)
                    {
                        if (this.state == personState.broken) { continue; }//Broken minded can't fear the darkness
                        double totalSus = 0;
                        foreach (Person p in this.society.people)
                        {
                            RelObj rel = this.getRelation(p);
                            double sus = rel.suspicion * map.param.person_threatFromSuspicion;
                            item.threat += sus;
                            totalSus += sus;
                        }
                        if (totalSus > 1)
                        {
                            item.reasons.Add(new ReasonMsg("Supicion of enshadowed nobles", totalSus));
                        }
                    }
                }
                else
                {
                    double value = item.group.getThreat(null);
                    item.reasons.Add(new ReasonMsg("Social Group's total threat: ", value));
                    Location sourceLoc = null;
                    //Fear things which are nearby
                    if (this.title_land != null)
                    {
                        sourceLoc = title_land.settlement.location;
                    }
                    //If you don't have a landed title you live in the capital
                    if (sourceLoc == null)
                    {
                        sourceLoc = society.getCapital();
                    }
                    //Fallback to just use the first location, to avoid null exceptions in extreme edge cases
                    if (sourceLoc == null)
                    {
                        sourceLoc = map.locations[0];
                    }

                    double infoAvailability = map.getInformationAvailability(sourceLoc, item.group);
                    int intInfoAvailability = (int)(infoAvailability * 100);
                    item.reasons.Add(new ReasonMsg("Information (% kept)", intInfoAvailability));
                    value *= infoAvailability;

                    double militaryStrengthMult = 50 / ((society.currentMilitary + (society.maxMilitary / 2)) + 1);
                    if (militaryStrengthMult < 0.5) { militaryStrengthMult = 0.5; }
                    item.reasons.Add(new ReasonMsg("Military Strength Comparison (% multiplier)", (int)(100 * militaryStrengthMult)));
                    value *= militaryStrengthMult;

                    item.threat = value;

                    if (item.group is Society)
                    {
                        double susThreat = 0;
                        Society soc = (Society)item.group;
                        foreach (Person p in soc.people)
                        {
                            susThreat += this.getRelation(p).suspicion*100;
                        }
                        if (susThreat > 200)
                        {
                            susThreat = 200;
                        }
                        item.reasons.Add(new ReasonMsg("Suspicion that nobles are enshadowed", (int)susThreat));
                        item.threat += susThreat;
                    }
                }

                if (Math.Abs(item.temporaryDread) > 1)
                {
                    item.threat += item.temporaryDread;
                    item.reasons.Add(new ReasonMsg("Temporary Dread", item.temporaryDread));
                }

                if (item.threat < 0) { item.threat = 0; }
                if (item.threat > 200) { item.threat = 200; }
            }
            threatEvaluations.Sort();
        }
        public void processThreats()
        {
            //First up, see if anything needs to be added/removed
            List<ThreatItem> rems = new List<ThreatItem>();
            HashSet<SocialGroup> groups = new HashSet<SocialGroup>();
            foreach (ThreatItem item in threatEvaluations)
            {
                if (item.group != null)
                {
                    if (item.group.isGone())
                    {
                        rems.Add(item);
                    }
                    else if (item.group == society)
                    {
                        rems.Add(item);
                    }
                    else
                    {
                        groups.Add(item.group);
                    }
                }
                item.turnTick();
            }
            foreach (ThreatItem item in rems) { threatEvaluations.Remove(item); }

            foreach (SocialGroup sg in map.socialGroups)
            {
                if (groups.Contains(sg) == false && (sg != society))
                {
                    ThreatItem item = new ThreatItem(map, this);
                    item.group = sg;
                    threatEvaluations.Add(item);
                }
            }
            
            foreach (ThreatItem item in threatEvaluations)
            {
                item.temporaryDread *= map.param.threat_temporaryDreadDecay;
            }

            computeThreats();
        }

        public ThreatItem getGreatestThreat()
        {
            ThreatItem g = null;
            double s = 0.0;

            foreach (ThreatItem t in threatEvaluations)
            {
                if (t.threat > s)
                {
                    g = t;
                    s = t.threat;
                }
            }

            return g;
        }

        public double getRelBaseline(Person other)
        {
            if (other == this) { return madness.selfLove; }
            return map.param.relObj_defaultLiking;
        }

        public Map map { get { return society.map; } }

        public string getFullName()
        {
            if (state == personState.enthralled)
            {
                return "Dark " + getTitles() + " " + firstName;
            }
            if (state == personState.broken)
            {
                return "Broken " + getTitles() + " " + firstName;
            }
            if (madness is Insanity_Sane == false)
            {
                return "Mad " + getTitles() + " " + firstName;
            }
            return getTitles() + " " + firstName;
        }

        public RelObj getRelation(Person other)
        {
            if (other == null) { throw new NullReferenceException(); }

            if (relations.ContainsKey(other)){
                return relations[other];
            }
            RelObj rel = new RelObj(this, other);
            relations.Add(other,rel);
            
            return rel;
        }

        public string getTitles()
        {
            double bestPrestige = 0;
            Title bestTitle = null;
            foreach (Title t in titles)
            {
                if (t.getPrestige() > bestPrestige)
                {
                    bestPrestige = t.getPrestige();
                    bestTitle = t;
                }
            }
            if (isMale)
            {
                if (bestTitle != null) { return bestTitle.nameM; }
                if (title_land != null) { return title_land.titleM; }
                return "Lord";
            }
            else
            {
                if (bestTitle != null) { return bestTitle.nameF; }
                if (title_land != null) { return title_land.titleF; }
                return "Lady";
            }
        }

        public string getMilitarismInfo()
        {
            int m = (int)(100*politics_militarism) + 100;
            if (m <= 40)
                return "Very Pacifist";
            else if (m <= 80)
                return "Somewhat Pacifist";
            else if (m <= 120)
                return "No Tendency";
            else if (m <= 160)
                return "Somewhat Walike";
            else
                return "Very Warlike";
        }

        public void die(string v)
        {
            double priority = 0;
            bool benefit = true;
            if (state == Person.personState.enthralled)
            {
                benefit = false;
                priority = MsgEvent.LEVEL_RED;
            }
            else if (state == Person.personState.broken)
            {
                benefit = false;
                priority = MsgEvent.LEVEL_ORANGE;
            }
            else
            {
                benefit = true;
                priority = MsgEvent.LEVEL_DARK_GREEN;
            }
            map.turnMessages.Add(new MsgEvent(this.getFullName() + " dies! " + v, priority, benefit));

            if (state == personState.enthralled)
            {
                map.world.prefabStore.popMsg(this.getFullName() + " has died: " + v + "\n\nTheir death is not the end, you may enthrall a new noble, and continue your work through a new vessel.");
            }
            removeFromGame(v);
        }

        public void removeFromGame(string v) { 
            World.log(v);

            society.people.Remove(this);
            if (this.title_land != null)
            {
                this.title_land.heldBy = null;
            }
            foreach (Title t in titles)
            {
                t.heldBy = null;
            }
            isDead = true;
            if (this == map.overmind.enthralled) { map.overmind.enthralled = null; }
        }

        public Sprite getImageBack()
        {
            if (imgIndBack == -1)
            {
                imgIndBack = Eleven.random.Next(map.world.textureStore.layerBack.Count);
            }
            return map.world.textureStore.layerBack[imgIndBack];
        }

        public Sprite getImageMid()
        {
            if (state == personState.normal)
            {
                return map.world.textureStore.person_basic;
            }
            if (state == personState.enthralled)
            {
                return map.world.textureStore.person_dark;
            }
            if (state == personState.broken)
            {
                return map.world.textureStore.person_halfDark;
            }

            return map.world.textureStore.person_basic;
        }
        public Sprite getImageFore()
        {
            if (imgIndFore == -1)
            {
                imgIndFore = Eleven.random.Next(map.world.textureStore.layerFore.Count);
            }
            return map.world.textureStore.layerFore[imgIndFore];
        }
    }
}
