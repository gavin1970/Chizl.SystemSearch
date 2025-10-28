using System;
using Chizl.Applications;
using System.Windows.Forms;

namespace Chizl.SearchSystemUI
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            if (!SingleInstance.IsRunning(About.Title))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Starter());
            }
        }
    }
}
