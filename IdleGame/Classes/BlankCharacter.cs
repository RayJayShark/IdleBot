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
    }
}