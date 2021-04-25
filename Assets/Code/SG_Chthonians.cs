using UnityEngine;


namespace Assets.Code
{
    public class SG_Chtonians : SocialGroup
    {
        public enum warStates { ATTACK, DEFEND };
        public warStates warState = warStates.ATTACK;

        public SG_Chtonians(Map map, Location startingLocation) : base(map)
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
            this.threat_mult = map.param.chtonianThreatMult;
        }

        public override bool hostileTo(Unit u)
        {
            if (u.society.isDark()) { return false; }
            return !u.isEnthralled();
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