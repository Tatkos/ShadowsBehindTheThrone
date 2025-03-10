using UnityEngine;


namespace Assets.Code
{
    public class SG_Chthonians : SocialGroup
    {
        public enum warStates { ATTACK, DEFEND };
        public warStates warState = warStates.ATTACK; //TODO USE THESE STATES TO DETERMINE IF CHTHONIANS SHOULD COOP UP OR ATTACK
                                                        //TODO Great code to reuse in Flesh_Ganglion!

        public SG_Chthonians(Map map, Location startingLocation) : base(map)
        {
            float colourReducer = 0.25f;
            color = new Color(
                (float)(Eleven.random.NextDouble() * colourReducer) + (1 - colourReducer),
                (float)Eleven.random.NextDouble() * colourReducer,
                (float)Eleven.random.NextDouble() * colourReducer);
            color2 = new Color(
                (float)(Eleven.random.NextDouble() * colourReducer) + (1 - colourReducer),
                (float)Eleven.random.NextDouble() * colourReducer,
                (float)Eleven.random.NextDouble() * colourReducer);
            this.setName("Chthonians from " + startingLocation.shortName);

            startingLocation.soc = this;
            this.threat_mult = map.param.chthonianThreatMult;
        }

        public override bool hostileTo(Unit u)
        {
            if (u.society.isDark()) { return false; }
            return !u.isEnthralled();
        }

        public void DeclareWarIfNeighborIsWeak()
        {
            foreach (Location loc in map.locations)
            {
                if (loc.soc is SG_Chthonians)
                {
                    foreach (Location l2 in loc.getNeighbours())
                    {
                        if (l2.soc is Society && l2.soc.currentMilitary < loc.soc.currentMilitary)
                        {
                            if (loc.soc.isAtWar() == false)
                            {
                                map.declareWar(loc.soc, l2.soc);
                            }

                        }
                    }
                }
            }
        }


        public override bool isDark()
        {
            return true;
        }
        public override string getTypeName()
        {
            return "Player Controlled";
        }
        public override string getTypeDesc()
        {
            return "A nation of Chthonians to aid you in your bid to plunge the world to darkness.";
        }
        public override void turnTick()
        {
            base.turnTick();
            foreach (Location loc in map.locations)
            {
                if (loc.soc == this)
                {
                    if (loc.settlement == null)
                    {
                        loc.soc = null;
                    }
                    else
                    {
                        loc.hex.purity = 0;
                    }
                }
            }
            DeclareWarIfNeighborIsWeak();


        }
        public override bool hasEnthralled()
        {
            return true;
        }

        public override void takeLocationFromOther(SocialGroup def, Location taken)
        {
            base.takeLocationFromOther(def, taken);

            if (taken.settlement != null)
            {
                if (taken.person() != null)
                {
                    taken.person().die("Sacrificed by the Chtonians as they overran " + taken.getName(), true);
                }
                taken.settlement = new Set_EnslavedCity(taken);
                //taken.settlement.fallIntoRuin();
            }
        }
    }
}