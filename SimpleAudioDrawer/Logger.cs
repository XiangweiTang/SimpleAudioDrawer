using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleAudioDrawer
{
    static class Logger
    {
        private static readonly object thisLock = new object();
        public static string LogPath = "tmp.txt";

        public static void WriteLine(string line, bool notInFile = false)
        {
            string content = DateTime.Now.ToStringContext() + "\t" + line;
            Console.WriteLine(content);
            if (!notInFile)
                File.AppendAllLines(LogPath, new List<string> { content });
        }

        public static void WriteLine(Exception e, bool notInFile = false)
        {
            WriteLine(e.ToString(), notInFile);
        }

        public static void WriteLineWithLock(string line, bool notInFile = false)
        {
            lock (thisLock)
            {
                WriteLine(line, notInFile);
            }
        }

        public static void WriteLineWithLock(Exception e, bool notInFile = false)
        {
            lock (thisLock)
            {
                WriteLine(e, notInFile);
            }
        }
    }
}
