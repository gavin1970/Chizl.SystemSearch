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
            var instName = About.TitleWithFileVersion;
            var winTitle = instName;
            // Two things:
            //  1. Using file version also allows multiple versions to be ran, just in case testing of before and after.
            //  2. In case Release version is running in memory, the developer can still change and test with Debug version.
#if DEBUG
            winTitle += " (DEBUG)";
            instName += " DEBUG";
#else
            winTitle += " (RELEASE)";
            instName += " RELEASE";
#endif

            if (!SingleInstance.IsRunning(instName.Replace(" ", "_")))
            {
                GlobalSetup.WindowTitlebarText = winTitle;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Starter());
            }
        }
    }
}
