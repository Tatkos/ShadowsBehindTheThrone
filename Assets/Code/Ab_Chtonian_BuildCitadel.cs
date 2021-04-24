using UnityEngine;


namespace Assets.Code
{
    public class Ab_Chtonian_BuildCitadel: Ability
    {
        public override void cast(Map map, Hex hex)
        {
            base.cast(map, hex);
            if (!castable(map, hex)) { return; }


            SG_UnholyFlesh soc = null;
            foreach (SocialGroup sg in map.socialGroups) //this seems to find the freshly created soc
            {
                if (sg is SG_UnholyFlesh) //TODO <- look up what this does
                {
                    soc = (SG_UnholyFlesh)sg; //TODO <- look up what this does
                }
            }
            if (soc == null)
            {
                map.socialGroups.Add(new SG_UnholyFlesh(map, hex.location)); //TODO <- look up what this does
            }
            else
            {
                hex.location.soc = soc;
            }

            hex.location.settlement = new Set_UnholyFlesh_Seed(hex.location); //TODO <- look up what this does

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
            return true;
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