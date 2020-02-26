﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Code
{
    public class God_Omni : God
    {
        public override string getDescFlavour()
        {
            return "Flavour here";
        }

        public override string getDescMechanics()
        {
            return "mechanics here";
        }

        public override string getName()
        {
            return "Omnipresent Darkness";
        }

        public override List<Ability> getUniquePowers()
        {
            return new List<Ability>();
        }

        public override void onStart(Map map)
        {
            foreach (God g in map.world.potentialGods)
            {
                map.overmind.powers.AddRange(g.getUniquePowers());
            }
        }
    }
}
