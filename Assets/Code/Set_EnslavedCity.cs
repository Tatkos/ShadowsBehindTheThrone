using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Set_ChtonianCitadel : Settlement
    {
        public Set_ChtonianCitadel(Location loc) : base(loc)
        {
            this.isHuman = false;
            name = "Chtonian Citadel";

            defensiveStrengthMax = 12;
            militaryCapAdd += loc.map.param.chtonian_slavearmyStrength;
            militaryRegenAdd = 3;
        }

        public override Sprite getCustomTerrain(Hex hex)
        {
            int c = hex.graphicalIndexer % 2;
            if (c == 0)
            {
                return World.staticMap.world.textureStore.hex_special_flesh; //TODO change sprite
            }
            else
            {
                return World.staticMap.world.textureStore.hex_special_flesh2; //TODO change sprite
            }
        }

        public override void turnTick()
        {
            base.turnTick();

            foreach (Hex h in location.territory)
            {
                h.flora = null;
            }
        }

        public override Sprite getSprite()
        {
            return location.map.world.textureStore.loc_flesh; //TODO change sprite
        }
    }
}
