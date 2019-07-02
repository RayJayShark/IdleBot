namespace IdleGame
{
    public class Player
    {
        public ulong Id;
        public string Name;
        public int CurHp;
        public int MaxHp;
        public int Money;
        public int Level;
        public int Exp;

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

        public Player(ulong Id, string Name, int CurHp, int MaxHp, int Money, int Level, int Exp)
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
                return true;        // Leveled Up!
            }

            return false;            // Not enough Exp to level
        }
        
        
    }
}