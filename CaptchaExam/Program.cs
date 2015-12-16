using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;

namespace CaptchaExam {
    static class Program {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main() {

            String useProxy = ConfigurationManager.AppSettings["useProxy"];
            String domain = ConfigurationManager.AppSettings["domain"];
            String user = ConfigurationManager.AppSettings["user"];
            String pass = ConfigurationManager.AppSettings["password"];
            String proxy = ConfigurationManager.AppSettings["proxy"];

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if("true".Equals(useProxy))
                Application.Run(new Form1(domain, user, pass, proxy));
            else
                Application.Run(new Form1());
        }
    }
}
