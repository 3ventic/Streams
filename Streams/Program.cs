using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Streams
{
    class Program
    {
        private static Bot bot = new Bot();

        static void Main(string[] args)
        {
            // Ensure data directory exists and is writeable
            if (!Directory.Exists("data"))
            {
                Directory.CreateDirectory("data");
            }
            File.WriteAllText("data/.wt", "3v");

            // Read token
            string token = File.ReadAllText("token.txt").Trim(' ', '\r', '\n');

            // Handle ^C
            Console.CancelKeyPress += Console_CancelKeyPress;

            // Run bot
            bot.Run(token).Wait();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e) => bot.Stop().Wait();
    }
}