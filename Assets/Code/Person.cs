﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public partial class Person
    {
        public int index = 0;
        public string firstName;
        public bool isMale = Eleven.random.Next(2) == 0;
        public List<Title> titles = new List<Title>();
        public TitleLanded title_land;
        public Society society;
        //public SavableMap_Person_RelObj relations = new SavableMap_Person_RelObj();
        public Dictionary<int, RelObj> relations = new Dictionary<int, RelObj>();
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
        public double awareness;
        public int letterWritingCharge = 0;
        public Action action;

        public ThreatItem threat_enshadowedNobles;
        public ThreatItem threat_agents;

        public double politics_militarism;

        public int investigationLastTurn = 0;
        public enum personState { normal,enthralled,broken,lightbringer,enthralledAgent};
        public personState state = personState.normal;
        public bool isDead;
        public Society rebellingFrom;
        public Unit unit;
        public Insanity madness;
        public House house;

        public Person(Society soc,House assignedHouse = null)
        {
            this.society = soc;
            index = World.staticMap.personIndexCount;
            World.staticMap.personIndexCount += 1;
            World.staticMap.persons.Add(this);//I can't believe this awful structure is required, but it is, for serializing. Not allowed references to others
            firstName = TextStore.getName(isMale);
            madness = new Insanity_Sane();

            maxSanity = Eleven.random.Next(map.param.insanity_maxSanity);
            sanity = maxSanity;

            if (World.logging)
            {
                log = new LogBox(this);
            }

            politics_militarism = Math.Pow(Eleven.random.NextDouble(), 2);//Bias towards 0
            //Chance of pacifism
            if (Eleven.random.NextDouble() < 0.33) {
                politics_militarism *= -1;
            }


            if (assignedHouse == null)
            {
                assignedHouse = society.houses[Eleven.random.Next(society.houses.Count)];
            }
            house = assignedHouse;

            //Add permanent threats
            threat_agents = new ThreatItem(map, this);
            threat_agents.form = ThreatItem.formTypes.AGENTS;
            threat_agents.responseCode = ThreatItem.RESPONSE_DARKNESSWITHIN;
            threatEvaluations.Add(threat_agents);

            threat_enshadowedNobles = new ThreatItem(map, this);
            threat_enshadowedNobles.form = ThreatItem.formTypes.ENSHADOWED_NOBLES;
            threat_enshadowedNobles.responseCode = ThreatItem.RESPONSE_DARKNESSWITHIN;
            threatEvaluations.Add(threat_enshadowedNobles);

            if (!map.simplified)
            {
                for (int i = 0; i < 3; i++)
                {
                    Trait add = map.globalist.getTrait(this);
                    if (add == null) { break; }
                    traits.Add(add);

                    if (Eleven.random.Next(2) == 0) { break; }//50% chance to add another trait
                }
            }
        }

        public void turnTick()
        {
            if (World.logging) { log.takeLine("---------Turn " + map.turn + "------------"); }
            
            this.targetPrestige = getTargetPrestige(null);
            if (Math.Abs(prestige-targetPrestige) < map.param.person_prestigeDeltaPerTurn)
            {
                prestige = targetPrestige;
            }
            else if (prestige < targetPrestige) { prestige += map.param.person_prestigeDeltaPerTurn; }
            else if (prestige > targetPrestige) { prestige -= map.param.person_prestigeDeltaPerTurn; }
            
            foreach (RelObj rel in relations.Values) {
                rel.turnTick(this);
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

            foreach (Trait t in traits)
            {
                t.turnTick(this);
            }

            processEnshadowment();
            computeAwareness();
            computeSuspicionGain();
            processActions();
            processThreats();
            processSanity();
        }

        public void processActions()
        {
            if (action != null)
            {
                action.turnTick(this);
                return;
            }

            if (state == personState.enthralled) { return; }
            if (state == personState.broken) { return; }

            //No action underway
            List<int> possibleActions = new List<int>();
            if (awareness >= map.param.awareness_canInvestigate && map.worldPanic >= map.param.panic_canInvestigate)
            {
                possibleActions.Add(0);
            }
            if (awareness > 0 && awareness < 1 && title_land != null && title_land.settlement is Set_University)
            {
                possibleActions.Add(1);
            }
            if (awareness < 1 && title_land != null && title_land.settlement is Set_University && map.worldPanic >= map.param.panic_researchAtUniWithoutAwareness)
            {
                possibleActions.Add(1);
            }
            if (map.worldPanic >= map.param.panic_letterWritingLevel && awareness >= map.param.awareness_letterWritingLevel)
            {
                possibleActions.Add(2);
            }
            if (map.worldPanic >= map.param.panic_letterWritingToAllLevel && awareness >= map.param.awareness_letterWritingLevel)
            {
                possibleActions.Add(3);
            }
            if (shadow > 0 && map.worldPanic >= map.param.panic_cleanseSoulLevel && awareness >= map.param.awareness_cleanseSoulLevel)
            {
                possibleActions.Add(4);
            }



            if (possibleActions.Count == 0) { return; }
            int act = possibleActions[Eleven.random.Next(possibleActions.Count)];

            switch (act) {
                case 0: { action = new Act_Investigate(); break; }
                case 1: { action = new Act_Research(); break; }
                case 2: { action = new Act_LetterToFriend(); break; }
                case 3: { action = new Act_LetterToFriend(); break; }
                case 4: { action = new Act_CleanseSoul(); break; }
            }
        }

        public void computeAwareness()
        {
            if (map.param.useAwareness != 1) { return; }
            if (this.state == personState.enthralled || this.state == personState.broken) { this.awareness = 0;this.action = null; return; }

            if (awareness > 0 && awareness < 1)
            {
                awareness -= map.param.awareness_decay;
            }
        }

        public double getTargetPrestige(List<string> reasons)
        {
            double prestige = map.param.person_defaultPrestige;
            if (reasons != null)
            {
                reasons.Add("Basic default: " + (int)(map.param.person_defaultPrestige));
            } 

            if (title_land != null)
            {
                prestige += title_land.settlement.getPrestige();
                if (reasons != null)
                {
                    reasons.Add("Land Title: " + (int)(title_land.settlement.getPrestige()));
                }
            }
            foreach (Title t in titles)
            {
                if (reasons != null)
                {
                    reasons.Add(t.getName() + " " + (int)(t.getPrestige()));
                }
                prestige += t.getPrestige();
            }

            foreach (Person p in this.getDirectSubordinates())
            {
                foreach (Trait t in p.traits)
                {
                    if (t.superiorPrestigeChange() != 0)
                    {
                        if (reasons != null)
                        {
                            reasons.Add(p.getFullName() + " (" + t.name + ") " + (int)(t.superiorPrestigeChange()));
                        }
                        prestige += t.superiorPrestigeChange();
                    }
                }
            }
            if (prestige < 0) { prestige = 0; }
            return prestige;
        }

        public bool getIsProvinceRuler()
        {
            foreach (Title t in titles)
            {
                if (t is Title_ProvinceRuler)
                    return true;
            }

            return false;
        }

        public Person getSuperiorIfAny()
        {
            foreach (Title t in titles)
            {
                if (t is Title_ProvinceRuler)
                {
                    return this.society.getSovreign();
                }
            }
            //Am not a duke
            foreach (Title t in society.titles)
            {
                if (t is Title_Sovreign)
                {
                    return null;
                }
                if (t is Title_ProvinceRuler)
                {
                    if (((Title_ProvinceRuler)t).province == this.getLocation().province)
                    {
                        return t.heldBy;
                    }
                }
            }
            //Am neither duke nor sovreign, nor do I live in a duke's province
            return society.getSovreign();
        }
        public Person getDirectSuperiorIfAny()
        {
            if (this == society.getSovreign()) { return null; }
            if (this.title_land == null) { return society.getSovreign(); }
            //if (society.getCapital() != null && society.getCapital().province == this.getLocation().province) { return society.getSovreign(); }
            foreach (Title t in society.titles)
            {
                if (t is Title_ProvinceRuler)
                {
                    Title_ProvinceRuler t2 = (Title_ProvinceRuler)t;
                    if (t2.province == this.getLocation().province)
                    {
                        if (t2.heldBy == this)
                            return society.getSovreign();
                        else
                            return t2.heldBy;
                    }
                }
            }
            return null;
        }
        public List<Person> getDirectSubordinates()
        {
            List<Person> subs = new List<Person>();
            foreach (Person p in society.people)
            {
                if (p.getDirectSuperiorIfAny() == this)
                {
                    subs.Add(p);
                }
            }
            return subs;
        }

        public void processSanity()
        {
            if (madness is Insanity_Sane) {
                if (sanity < 1)
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
                    double distance = 0;
                    if (this.getLocation() != null && this.getLocation().distanceToTarget.ContainsKey(sg)) { distance = getLocation().distanceToTarget[sg]; }
                    if (distance < 1) { distance = 1; }
                    distance = Math.Sqrt(distance);
                    double infoAvail = 1 / distance;
                    if (infoAvail < 0.2) { infoAvail = 0.2; }
                    RelObj rel = getRelation(p);
                    double evidenceMult = Math.Pow(p.evidence, map.param.person_evidenceExponent);//Make low evidence a bit slower to cause suspicion
                    //Give a bonus to the player, to allow their henchmen to be caught first
                    if (p.state == personState.enthralled)
                    {
                        evidenceMult *= map.param.person_evidenceReduceEnthralled;
                    }
                    double fromTraits = 1;
                    foreach (Trait t in traits) { fromTraits *= t.suspicionMult(); }
                    if (rel.suspicion > p.evidence * map.param.relObj_suspicionLimiterMult) { continue; }
                    rel.suspicion += infoAvail * evidenceMult * map.param.person_suspicionPerEvidence * fromTraits;
                }
            }
        }

        public Location getLocation()
        {
            if (unit != null) { return unit.location; }
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
            if (state == personState.lightbringer)
            {
                shadow = 0;
                return;
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
            //If you've not broken yet, decay the shadow away
            if (state != personState.broken && state != personState.enthralled)
            {
                shadow -= map.param.person_shadowDecayPerTurn;
                if (shadow < 0) { shadow = 0; }
            }
        }

        public double getAwarenessMult()
        {
            double v = (1 - this.shadow);
            if (this.state == personState.enthralled) { return 0 ; }

            if (this.title_land != null && this.title_land.settlement is Set_University) 
            {
                v *= map.param.awarenessUniversityBonusMult;
            }

            foreach (Trait t in traits)
            {
                v *= t.getAwarenessMult();
            }

            return v;
        }
        public bool enthrallable()
        {
            if (unit != null) { return false; }
            if (state == personState.broken) { return true; }

            Society soc = society;
            double minPrestige = 1000000;
            foreach (Person p in soc.people)
            {
                if (p.title_land == null) { continue; }
                if (p.targetPrestige < minPrestige)
                {
                    minPrestige = p.targetPrestige;
                }
            }

            //return soc.getEnthrallables().Contains(hex.location.settlement.title.heldBy);
            return targetPrestige < (1 + minPrestige);
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
                            item.reasons.Add(new ReasonMsg("Supicion of enshadowed nobles (+" + (int)(totalSus) + ")", totalSus));
                        }
                    }
                    if (item.form == ThreatItem.formTypes.AGENTS)
                    {
                        if (this.state == personState.broken) { continue; }//Broken minded can't fear the darkness
                        double mult = 1 - shadow;
                        item.threat += mult * society.dread_agents_evidenceFound;
                        item.reasons.Add(new ReasonMsg("Evidence found (+" + (int)(mult*society.dread_agents_evidenceFound) + ")",(mult*society.dread_agents_evidenceFound)));
                    }
                }
                else
                {
                    double value = item.group.getThreat(null);
                    item.reasons.Add(new ReasonMsg("Social Group's total threat (+" + (int)(value) + ")", value));
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

                    double distance = 0;
                    if (this.getLocation() != null)
                    {
                        if (this.getLocation().distanceToTarget.ContainsKey(item.group)) { distance = getLocation().distanceToTarget[item.group]; }
                    }
                    if (distance < 1) { distance = 1; }
                    int distanceI = (int)distance;
                    distance = Math.Sqrt(distance);
                    double infoAvail = 1 / distance;
                    if (infoAvail < 0.2) { infoAvail = 0.2; }
                    int intInfoAvailability = (int)(infoAvail * 100);
                    item.reasons.Add(new ReasonMsg("Distance (" + (int)(intInfoAvailability) + "% Multiplier)", intInfoAvailability));
                    value *= infoAvail;

                    double ourMilitary = society.currentMilitary + (society.maxMilitary/2);
                    double theirMilitary = item.group.currentMilitary + (item.group.maxMilitary/2);
                    double militaryStrengthMult = theirMilitary / (ourMilitary + 1);
                    //double militaryStrengthMult = 50 / ((society.currentMilitary + (society.maxMilitary / 2)) + 1);
                    if (militaryStrengthMult < 0.5) { militaryStrengthMult = 0.5; }
                    if (militaryStrengthMult > 2.5) { militaryStrengthMult = 2.5; }
                    item.reasons.Add(new ReasonMsg("Relative strengths of social-group's militaries (" + (int)(100 * militaryStrengthMult) + "% Multiplier)" , (int)(100 * militaryStrengthMult)));
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
                        item.reasons.Add(new ReasonMsg("Suspicion that nobles are enshadowed (+" + (int)(susThreat) + ")", (int)susThreat));
                        item.threat += susThreat;

                        if (soc.offensiveTarget == this.society && soc.posture == Society.militaryPosture.offensive)
                        {
                            double threatAdd = map.param.person_threatFromBeingOffensiveTarget * item.threat;
                            item.reasons.Add(new ReasonMsg("We are their offensive target (" + (int)(threatAdd) + ")", threatAdd));
                            item.threat += threatAdd;
                        }

                        bool hasKillOrder = false;
                        foreach (KillOrder order in soc.killOrders)
                        {
                            if (order.person == this)
                            {
                                hasKillOrder = true;
                            }
                        }
                        if (hasKillOrder)
                        {
                            item.reasons.Add(new ReasonMsg("They intend to execute me! (+50)", 50));
                            item.threat += 50;
                        }
                        else
                        {
                            if (item.threat > 75 && (soc.posture != Society.militaryPosture.offensive) && susThreat == 0 && (soc.isAtWar() == false))
                            {

                                item.reasons.Add(new ReasonMsg("They are at peace, non-offensive and have no suspected dark nobles (cap at 75)", 0));
                                item.threat = 75;
                            }
                            else if (item.threat > 125 && soc.offensiveTarget != this.society && susThreat == 0)
                            {

                                item.reasons.Add(new ReasonMsg("We are not their offensive target and no suspected dark nobles (cap at 125)", 0));
                                item.threat = 125;
                            }
                        }
                    }
                }

                item.threatBeforeTemporaryDread = item.threat;

                if (Math.Abs(item.temporaryDread) > 1)
                {
                    item.threat += item.temporaryDread;
                    item.reasons.Add(new ReasonMsg("Temporary Dread (+" + (int)(item.temporaryDread) + ")", item.temporaryDread));
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

        public double getRelBaseline(int other)
        {
            if (other == this.index) { return madness.selfLove; }
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
            string add = "";
            if (house != null && society.socType.usesHouses())
            {
                add += " " + house.name;
            }
            return getTitles() + " " + firstName + add;
        }

        public RelObj getRelation(Person other)
        {
            if (other == null) { throw new NullReferenceException(); }

            return getRelation(other.index);
        }

        public RelObj getRelation(int other)
        {

            if (relations.ContainsKey(other))
            {
                return relations[other];
            }
            RelObj rel = new RelObj(this, other);
            relations.Add(other, rel);

            return rel;
        }

        public string getTitles()
        {
            if (unit != null)
            {
                if (isMale)
                {
                    return unit.getTitleM();
                }
                return unit.getTitleF();
            }
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
            else if (state == Person.personState.lightbringer)
            {
                benefit = true;
                priority = MsgEvent.LEVEL_GREEN;
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
            }else if (society.hasEnthralled())
            {
                map.world.prefabStore.popMsg(this.getFullName() + " has died: " + v + "");

            }

            removeFromGame(v);
        }

        public void removeFromGame(string v) { 
            World.log(v);

            if (GraphicalMap.selectedSelectable == this)
            {
                GraphicalMap.selectedSelectable = null;
            }

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
            if (unit != null && unit.definesBackground()) { return unit.getPortraitBackground(); }
            if (house != null) { return map.world.textureStore.layerBack[house.background]; }
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
            if (state == personState.enthralledAgent)
            {
                return map.world.textureStore.person_dark;
            }
            if (state == personState.lightbringer)
            {
                return map.world.textureStore.person_lightbringer;
            }
            if (state == personState.broken)
            {
                return map.world.textureStore.person_halfDark;
            }

            return map.world.textureStore.person_basic;
        }
        public Sprite getImageFore()
        {
            if (unit != null && unit.definesForeground()) { return unit.getPortraitForeground(); }
            if (imgIndFore == -1)
            {
                imgIndFore = Eleven.random.Next(map.world.textureStore.layerFore.Count);
            }
            return map.world.textureStore.layerFore[imgIndFore];
        }
        public Sprite getImageBorder()
        {
            if (this == this.society.getSovreign()) { return map.world.textureStore.slotKing; }
            if (this.titles.Count > 0) { return map.world.textureStore.slotDuke; }
            if (this.title_land != null) { return map.world.textureStore.slotCount; }
            return map.world.textureStore.slotBasic;
        }
    }
}
