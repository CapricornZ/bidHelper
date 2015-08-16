using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;

[assembly: log4net.Config.XmlConfigurator(ConfigFile="log4net.config", Watch = true)]
namespace Helper
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            String endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            String debug = ConfigurationManager.AppSettings["DEBUG"];

            if ("true".Equals(debug.ToLower()))
            {
                WindowHelper.AllocConsole();
                WindowHelper.SetConsoleTitle("千万不要关掉我!");
                IntPtr windowHandle = WindowHelper.FindWindow(null, System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                IntPtr closeMenu = WindowHelper.GetSystemMenu(windowHandle, IntPtr.Zero);
                uint SC_CLOSE = 0xF060;
                WindowHelper.RemoveMenu(closeMenu, SC_CLOSE, 0x0);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(endPoint:endPoint));
        }
    }
}
