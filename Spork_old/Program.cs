using System;
using GoCommando;

namespace Spork
{
    class Program
    {
        static void Main()
        {
            var foregroundColor = Console.ForegroundColor;
            var backgroundColor = Console.BackgroundColor;

            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.Black;

                Console.WriteLine(@" ______     ______   ______     ______     __  __    
/\  ___\   /\  == \ /\  __ \   /\  == \   /\ \/ /    
\ \___  \  \ \  _-/ \ \ \/\ \  \ \  __<   \ \  _""-.  
 \/\_____\  \ \_\    \ \_____\  \ \_\ \_\  \ \_\ \_\ 
  \/_____/   \/_/     \/_____/   \/_/ /_/   \/_/\/_/ 
");


                Go.Run();
            }
            finally
            {
                Console.ForegroundColor = foregroundColor;
                Console.BackgroundColor = backgroundColor;
            }
        }
    }
}
