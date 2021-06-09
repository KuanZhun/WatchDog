using System;
using System.IO;

namespace KzDev
{
    class ExceptionLogWriter
    {
        private static readonly string exceptionLogPath = Path.Combine(Directory.GetCurrentDirectory(), "ExceptionLog");
        public static void WriteLog(Exception ex)
        {
            DateTime time = DateTime.Now;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(time.ToString("yyyy/MM/dd HH:mm:ss.fff"));
            Console.WriteLine(ex);
            Console.ResetColor();
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                using (FileStream fileStream = File.Open(exceptionLogPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fileStream.Position = fileStream.Length;
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.WriteLine(time.ToString("yyyy/MM/dd HH:mm:ss.fff") + "\n" + ex + "\n");
                    }
                }
            });
        }
    }
}