using UnityEngine;


namespace Assets.Code
{
    public class Ab_Chtonian_BuildCitadel: Ability
    {
        public override void cast(Map map, Hex hex)
        {
            base.cast(map, hex);
            if (!castable(map, hex)) { return; }


            SG_Chtonians soc = null;
            foreach (SocialGroup sg in map.socialGroups) //this seems to find the freshly created soc
            {
                if (sg is SG_Chtonians) //TODO <- look up what this does
                {
                    soc = (SG_Chtonians)sg; //TODO <- look up what this does
                }
            }
            if (soc == null)
            {
                map.socialGroups.Add(new SG_Chtonians(map, hex.location)); 
            }
            else
            {
                hex.location.soc = soc;
            }

            hex.location.settlement = new Set_ChtonianCitadel(hex.location);

            map.world.prefabStore.popImgMsg(
                "You call your children to return from hiding and build a great citadel. They will build up an army and send out infiltrators to soften up nearby humans for conquest."
                + "\nYou may instruct your children what to do next.",
                map.world.wordStore.lookup("UNHOLY_FLESH_SEED")); //TODO <- look up what this does
        }

        public override void playSound(AudioStore audioStore)
        {
            audioStore.playActivateFlesh();
        }

        public override bool castable(Map map, Hex hex)
        {
            if (hex.location == null) { return false; }
            if (hex.location.soc != null) { return false; }
            if (hex.location.settlement != null) { return false; }
            if (hex.location.isOcean) { return false; }
            if (hasCitadelOnMap (map) == true) { return false; } 
            return true;
        }

        public bool hasCitadelOnMap(Map map)
        {
            foreach (Location loc in map.locations)
            {
                if (loc.settlement is Set_ChtonianCitadel)
                {
                    Debug.Log("Chtonian citadel found.");
                    return true;
                }
            }

            return false;
        }

        public override int getCost()
        {
            return World.staticMap.param.ability_fleshSeedCost;
        }

        public override string getDesc()
        {
            return "Build a citadel for your progeny."
                 + "\n[Requires an empty land location]";
        }

        public override string getName()
        {
            return "Build Chtonian Citadel";
        }

        public override Sprite getSprite(Map map)
        {
            return map.world.textureStore.icon_ghoul;
        }
    }
}