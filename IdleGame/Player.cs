using System.Collections.Generic;

namespace IdleGame
{
    public class Player
    {
        public ulong Id;
        public string Name;
        public uint CurHp;
        public uint MaxHp;
        public uint Money;
        public uint Level;
        public uint Exp;
        public Dictionary<uint, uint> Inventory = new Dictionary<uint, uint>();  //Key = id, Value = quantity

        public Player(ulong Id, string Name)
        {
            this.Id = Id;
            this.Name = Name;
            CurHp = 10;
            MaxHp = 10;
            Money = 10;
            Level = 1;
            Exp = 0;
        }

        public Player(ulong Id, string Name, uint CurHp, uint MaxHp, uint Money, uint Level, uint Exp)
        {
            this.Id = Id;
            this.Name = Name;
            this.CurHp = CurHp;
            this.MaxHp = MaxHp;
            this.Money = Money;
            this.Level = Level;
            this.Exp = Exp;
        }

        public bool LevelUp()
        {
            if (Exp > 10 * Level)
            {
                Level++;
                Exp = 0;
                return true;    // Leveled Up!
            }

            return false;       // Not enough Exp to level
        }
        
    }
}