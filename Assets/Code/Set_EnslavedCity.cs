using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class Set_EnslavedCity : Settlement
    {
        public Set_EnslavedCity(Location loc) : base(loc)
        {
            this.isHuman = false;
            name = "Enslaved City";

            defensiveStrengthMax = 5;
            militaryCapAdd += loc.map.param.chthonian_slavearmyStrength;
            militaryRegenAdd = 3;
        }

        //public override Sprite getCustomTerrain(Hex hex)
        //{
        //    int c = hex.graphicalIndexer % 2;
        //    if (c == 0)
        //    {
        //        return World.staticMap.world.textureStore.hex_special_void1; //TODO change sprite
        //    }
        //    else
        //    {
        //        return World.staticMap.world.textureStore.hex_special_void2; //TODO change sprite
        //    }
        //}

        public override void turnTick()
        {
            base.turnTick();

            foreach (Hex h in location.territory)
            {
                h.flora = null;
            }

            BuildFortresses();
        }

        public override Sprite getSprite()
        {
            return location.map.world.textureStore.loc_enslaved_city;
        }

        public override void checkUnitSpawning()
        {
            spawnCounter += 1;
            if (spawnCounter > 5)
            {
                spawnCounter = 0;

                if (this.attachedUnit != null) { throw new Exception(); }

                if (location.soc is SG_Chthonians == false) { return; }
                Unit_ChthonianArmy army = new Unit_ChthonianArmy(location, (SG_Chthonians)location.soc);
                location.map.units.Add(army);
                army.maxHp = (int)this.getMilitaryCap();
                this.attachedUnit = army;
                World.log("Created new Chthonian Army");
            }
        }

        public void BuildFortresses()
        {
            foreach (Location loc in location.getNeighbours())
            {
                if (loc.settlement == null && loc.isOcean == false && Eleven.random.NextDouble() < 0.05)
                {
                    loc.settlement = new Set_ChthonianFortress(loc);
                    loc.soc = this.location.soc;
                }

            }
        }

    }
}
