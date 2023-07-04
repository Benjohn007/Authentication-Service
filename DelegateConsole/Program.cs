
namespace DelegateConsole
{
    public class Program
    {
        delegate void LogDel(string text);

        static void Main(string[] args)
        {
            Log log = new Log();
            // LogDel logD = new LogDel(log.LogTextToFile);

            LogDel LogTextToScreenDel, LogTextToFileDel;

            LogTextToScreenDel = new LogDel(log.LogTextToScreen);
            LogTextToFileDel = new LogDel(log.LogTextToFile);

            LogDel multipleLog = LogTextToScreenDel + LogTextToFileDel;

            Console.WriteLine("Please enter your name");
            var name = Console.ReadLine();

            //multipleLog(name!);
            LogText(log.LogTextToFile, name);
            Console.ReadKey();
        }
        static void LogText(LogDel logdel, string text)
        {
            logdel(text);
        }
    }
    public class Log
    {
        public void LogTextToScreen(string text)
        {
            Console.WriteLine($"{DateTime.Now}: {text}");
        }

        public void LogTextToFile(string text)
        {
            using (StreamWriter sw = new StreamWriter(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.txt"), true))
            {
                sw.WriteLine($"{DateTime.Now}: {text}");
            }
        }
    }
}