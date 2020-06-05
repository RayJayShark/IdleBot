using Discord.Commands;
using IdleGame.Services;

namespace IdleGame.Modules
{
    [Name("Planet and Party Commands")]
    public class PlanetModule : ModuleBase<SocketCommandContext>
    {
        private readonly SqlService _sqlService;
        
        public PlanetModule(SqlService sqlService = null)
        {
            _sqlService = sqlService;
        }
    
        
    }
}