namespace IdleGame.Classes
{
    public class BlankCharacter : Player
    {
        public BlankCharacter()
        {
            Id = 0;
        }

        public override void LevelUp() {}

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

        public override uint GetExp()
        {
            throw new System.NotImplementedException();
        }

        public override void GiveExp(uint exp)
        {
            throw new System.NotImplementedException();
        }
    }
}