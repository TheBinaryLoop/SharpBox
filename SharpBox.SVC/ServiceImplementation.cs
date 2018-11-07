using System;
using System.ServiceProcess;
using SharpBox.SVC.Framework;

namespace SharpBox.SVC
{
    /// <summary>
    /// The actual implementation of the windows service goes here...
    /// </summary>
    [WindowsService("SharpBox.SVC",
         DisplayName = "SharpBox.SVC",
         Description = "Helper service for SharpBox.",
         EventLogSource = "SharpBox.SVC",
         StartMode = ServiceStartMode.Automatic)]
    public class ServiceImplementation : IWindowsService
    {

        /// <summary>
        /// This method is called when the service gets a request to start.
        /// </summary>
        /// <param name="args">Any command line arguments</param>
        public void OnStart(string[] args)
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.Green, "OnStart({0})", args);
        }

        /// <summary>
        /// This method is called when the service gets a request to stop.
        /// </summary>
        public void OnStop()
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.Red, "OnStop()");
        }

        /// <summary>
        /// This method is called when a service gets a request to pause,
        /// but not stop completely.
        /// </summary>
        public void OnPause()
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.Magenta, "OnPause()");
        }

        /// <summary>
        /// This method is called when a service gets a request to resume 
        /// after a pause is issued.
        /// </summary>
        public void OnContinue()
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.Blue, "OnContinue()");
        }

        /// <summary>
        /// This method is called when the machine the service is running on
        /// is being shutdown.
        /// </summary>
        public void OnShutdown()
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.DarkRed, "OnShutdown()");
        }

        /// <summary>
        /// This method is called when a custom command is issued to the service.
        /// </summary>
        /// <param name="command">The command identifier to execute.</param >
        public void OnCustomCommand(int command)
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.DarkGreen, "OnCustomCommand({0})", command);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            ConsoleHarness.WriteToConsole(ConsoleColor.DarkMagenta, "Dispose()");
        }

    }
}
