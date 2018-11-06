using System;
using System.Reflection;
using BinaryTools;
using BinaryTools.Core.Extensions;
using EasyHook;

namespace SharpBox.CLI
{
    static class Border
    {
        public const Char TopLeft = '╔';
        public const Char TopRight = '╗';
        public const Char BottomLeft = '╚';
        public const Char BottomRight = '╝';
        public const Char Horizontal = '═';
        public const Char Vertical = '║';
    }

    class Program
    {
        private const string ReleaseChannel = "stable";

        private const string ProgramPath = @"C:\Program Files\7-Zip\7zFM.exe";

        static String ChannelName = null;

        static void Main(string[] args)
        {
            InitGUI();

            Console.WriteLine(Assembly.GetExecutingAssembly().Location);

            try
            {
                Config.Register("A sandbox application written in c#.", "SharpBox.CLI.exe", "SharpBox.Remote.dll");
                RemoteHooking.IpcCreateServer
            }
            catch (Exception ExtInfo)
            {
                Console.WriteLine($"There was an error while connecting to target:\r\n{ExtInfo.ToString()}");
            }
            Console.ReadLine();
        }

        private static void InitGUI()
        {
#if DEBUG
            Console.Title = $"SharpBox v{Assembly.GetExecutingAssembly().GetName().Version}[dev]";
            string VersionString = $"Version {Assembly.GetExecutingAssembly().GetName().Version}[dev]";
#else
            Console.Title = $"SharpBox v{Assembly.GetExecutingAssembly().GetName().Version}[{ReleaseChannel}]";
            string VersionString = $"Version {Assembly.GetExecutingAssembly().GetName().Version}[{ReleaseChannel}]";
#endif
            if (VersionString.Length % 2 != 0) VersionString += " ";
            ConsoleEx.WriteLineCentered($"{Border.TopLeft}{Border.Horizontal.Repeat(VersionString.Length + 2)}{Border.TopRight}");
            ConsoleEx.WriteLineCentered($"{Border.Vertical} {' '.Repeat((VersionString.Length - "SharpBox".Length) / 2)}SharpBox{' '.Repeat((VersionString.Length - "SharpBox".Length) / 2)} {Border.Vertical}");
            ConsoleEx.WriteLineCentered($"{Border.Vertical} {VersionString} {Border.Vertical}");
            ConsoleEx.WriteLineCentered($"{Border.BottomLeft}{Border.Horizontal.Repeat(VersionString.Length + 2)}{Border.BottomRight}");
        }
    }
}
