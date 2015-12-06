using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;
using System.Drawing;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch = true)]
namespace Helper
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {
            String endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            String debug = ConfigurationManager.AppSettings["DEBUG"];
            String timePos = ConfigurationManager.AppSettings["TimePosition"];

            if (args.Length == 2) {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["principal"].Value = args[0];
                config.AppSettings.Settings["credential"].Value = args[1];
                ConfigurationManager.RefreshSection("appSettings");
                config.Save();
            }

            IntPtr windowHandle = IntPtr.Zero;
            if ("true".Equals(debug.ToLower()))
            {
                WindowHelper.AllocConsole();
                windowHandle = WindowHelper.FindWindow(null, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                IntPtr closeMenu = WindowHelper.GetSystemMenu(windowHandle, IntPtr.Zero);
                uint SC_CLOSE = 0xF060;
                WindowHelper.RemoveMenu(closeMenu, SC_CLOSE, 0x0);
                WindowHelper.SetConsoleTitle("嫑要关掉我!");
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(endPoint: endPoint, timePos: timePos, consoleHWND: windowHandle));
        }
    }
}
