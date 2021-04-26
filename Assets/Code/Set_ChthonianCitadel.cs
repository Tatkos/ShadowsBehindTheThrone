using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Set_ChthonianCitadel : Settlement
    {
        public Set_ChthonianCitadel(Location loc) : base(loc)
        {
            this.isHuman = false;
            name = "Chthonian Citadel";

            defensiveStrengthMax = 20;
            militaryCapAdd += loc.map.param.chtonian_armyStrength;
            militaryRegenAdd = 1;
            Unit_ChthonianInfiltrator infiltrator = new Unit_ChthonianInfiltrator(location, location.soc);
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


            //Create Infiltrator from the Citadel if one doesn't exist.
            if (checkIfLinkedAgentIsAlive() == false)
            {
                Debug.Log("Trying to create infiltrator...");
                Unit_ChthonianInfiltrator infiltrator = new Unit_ChthonianInfiltrator(location, location.soc);

            }

        }

        public override Sprite getSprite()
        {
            return location.map.world.textureStore.loc_flesh; //TODO change sprite
        }


        public bool checkIfLinkedAgentIsAlive()
        {
            //foreach (Unit unit in World.staticMap.units) //Dear Bobby, if you ever see this, you can delete this line
            foreach (Unit unit in location.map.units)
            {
                if (unit.parentLocation == this.location)
                {
                    Debug.Log("LOCATION'S AGENT FOUND!");
                    return true;
                }
            }
            Debug.Log("No agent found for location. :(");
            return false;
        }
    }
}
