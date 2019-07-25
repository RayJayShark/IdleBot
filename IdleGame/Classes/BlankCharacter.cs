namespace IdleGame.Classes
{
    public class BlankCharacter : Player
    {
        public BlankCharacter()
        {
            Id = 0;
        }

        public override bool LevelUp()
        {
            return false;
        }

        public override uint GetCurrentHp()
        {
            throw new System.NotImplementedException();
        }

        public override void GiveHp(uint health)
        {
            throw new System.NotImplementedException();
        }

        public override bool TakeDamage(uint damage)
        {
            throw new System.NotImplementedException();
        }
    }
}