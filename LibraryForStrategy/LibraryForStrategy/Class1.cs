using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibraryForStrategy
{
    public class Unit
    {
        public string name = "Soldier";
        public bool active = false;
        public int player = 0;//Ничей, принадлежности 1+
        public int team = 0;//Ничей, принадлежности 1+
        public int HP = 0;
        public int EXP = 0;
        public int level = 0;
        public int defence = 10;
        public int atack = 35;

        public static bool Atack(string name)
        {
            return true;
        }
    }

    public class CellOfLand
    {
        public string name = "Snow.png";
        public int passability = 1;
        public int additionalDefence = 5;
        public int player = -2;


    }
}
