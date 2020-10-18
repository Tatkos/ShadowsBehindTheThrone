﻿using UnityEngine;

namespace Assets.Code
{
    public class Pr_MinorSecurityBoost : Property_Prototype
    {
        
        public Pr_MinorSecurityBoost(Map map,string name) : base(map,name)
        {
            this.decaysOverTime = true;
            this.stackStyle = stackStyleEnum.TO_MAX_CHARGE;
            this.securityIncrease = map.param.society_securityBuffMinor;
        }

        public override void turnTick(Property p,Location location)
        {
        }

        public override Sprite getSprite(World world)
        {
            return world.textureStore.property_forgottenSecret;
        }

        internal override string getDescription()
        {
            return "This location has increased its security level to a moderate degree.";
        }
    }
}
