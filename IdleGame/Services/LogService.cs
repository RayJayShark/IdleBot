using System;
using System.Threading.Tasks;

namespace IdleGame.Services
{
    public static class LogService
    {

        public static void DatabaseLog(string log)
        {
            Console.WriteLine(GetTimestamp() + " Database    " + log);
        }

        public static void GameLog(string log)
        {
            Console.WriteLine(GetTimestamp() + " Game\t     " + log);
        }
    
        private static string GetTimestamp()
        {
            return $"{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}";
        }
    }
}