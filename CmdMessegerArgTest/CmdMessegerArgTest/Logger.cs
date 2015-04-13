using System;
using System.IO;

namespace CmdMessegerArgTest
{
    internal class Logger
    {
        private static int _count;

        public static void Log(String lines)
        {
            // Linjene blir skrevet inn i Log.txt i Folderen hvor exe'en blir kjørt
            using (                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 var file = new StreamWriter("Log.txt", true))
            {
                file.WriteLine(_count + ": " + lines);
                _count++;
            }
        }
    }
}