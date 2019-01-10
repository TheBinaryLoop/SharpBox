using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using EasyHook;
using SharpBox.Remote;

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

        //private const string targetExe = @"C:\Program Files\7-Zip\7zFM.exe";
        //private const string targetExe = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\IDE\devenv.exe";

        static void Main(string[] args)
        {
            string targetExe = null;

            // Will contain the name of the IPC server channel
            string channelName = null;

            if (!Console.IsOutputRedirected) InitGUI();

            Console.WriteLine();

            if (args.Length <= 0 || String.IsNullOrEmpty(args[0]))
            {
                Console.WriteLine("Missing argument: Target Executable");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"'{args[0]}' is not a valid file");
                return;
            }
            targetExe = args[0];

            // Create the IPC server using the SharpBox.SharpBoxInterface class as a singleton
            RemoteHooking.IpcCreateServer<SharpBoxInterface>(ref channelName, WellKnownObjectMode.Singleton);
            
            // Get the full path to the assembly we want to inject into the target process
            string injectionLibrary = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SharpBox.Remote.dll");

            try
            {
                Console.WriteLine("Attempting to create and inject into {0}", targetExe);

                RemoteHooking.CreateAndInject(
                    targetExe,          // executable to run
                    string.Join(" ", args.Skip(1).ToArray()),                 // command line arguments for target
                    0,                  // additional process creation flags to pass to CreateProcess
                    InjectionOptions.DoNotRequireStrongName, // allow injectionLibrary to be unsigned
                    injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                    injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                    out Int32 OutProcessId,      // retrieve the newly created process ID
                    channelName,         // the parameters to pass into injected library
                    targetExe,
                    injectionLibrary,
                    injectionLibrary
                );

                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("There was an error while injecting into target:");
                Console.ResetColor();
                Console.WriteLine(e.ToString());
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("<Press any key to exit>");
            Console.ResetColor();
            Console.ReadKey();
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
            WriteLineCentered($"{Border.TopLeft}{new String(Border.Horizontal, VersionString.Length + 2)}{Border.TopRight}");
            WriteLineCentered($"{Border.Vertical} {new String(' ',(VersionString.Length - "SharpBox".Length) / 2)}SharpBox{new String(' ',(VersionString.Length - "SharpBox".Length) / 2)} {Border.Vertical}");
            WriteLineCentered($"{Border.Vertical} {VersionString} {Border.Vertical}");
            WriteLineCentered($"{Border.BottomLeft}{new String(Border.Horizontal, VersionString.Length + 2)}{Border.BottomRight}");
        }

        public static void WriteLineCentered(string value, ConsoleColor foregroundColor = ConsoleColor.Gray, ConsoleColor backgrounColor = ConsoleColor.Black)
        {
            Console.SetCursorPosition((Console.WindowWidth - value.Length) / 2, Console.CursorTop);
            Console.WriteLine(value);
        }
    }
}
