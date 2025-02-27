using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Assets.Code
{

    public class God_Chthonian : God
    {
        public List<Ability> powers = new List<Ability>();

        public God_Chthonian()
        {
            powers.Add(new Ab_Chthonian_BuildCitadel());
        }

        public override string getDescFlavour()
        {
            return "Born of your malice, your progeny are waking from their slumber, mustering through the gates from the void." +
                "\n\nYour children shall inherit the earth.";
        }

        public override string getDescMechanics()
        {
            return "This Name works by creating a an allied quasi-nation that will offer flexible help infiltrating or conquering human nations." +
                " If the Chthonians conquer a human settlement, they enslave the humans and recruit them into their armies at a penalty." +
                "\n\nShould you be killed, your children will try to defend and hold out until you return.";
        }

        public override string getName()
        {
            return "Chthonian Progenitor";
        }

        public override Sprite getGodBackground(World world)
        {
            return world.textureStore.painting_RhanTegoth;
        }
        public override string getCredits()
        {
            return "The Rhan-Tegoth, Borja Pindando";
        }

        public override List<Ability> getUniquePowers()
        {
            return powers;
        }
    }

}
