using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;

namespace tobid.util {

    public class Util {

        private static IDictionary<String, String> osVerRepo = new Dictionary<String, String>();
        static Util(){
            osVerRepo.Add("10.0", "win10");
            osVerRepo.Add("6.3", "win8.1");
            osVerRepo.Add("6.2", "win8");
            osVerRepo.Add("6.1", "win7");
            osVerRepo.Add("6.0", "winVista");
            osVerRepo.Add("5.2", "win2003");
            osVerRepo.Add("5.1", "winXP");
            osVerRepo.Add("5.0", "win2000");
        }

        public static String osVersion() {

            //Console.WriteLine("Operation System Information");
            //Console.WriteLine("----------------------------");
            //Console.WriteLine("Name = {0}", OSInfo.Name);
            //Console.WriteLine("Edition = {0}", OSInfo.Edition);
            //Console.WriteLine("Service Pack = {0}", OSInfo.ServicePack);
            //Console.WriteLine("Version = {0}", OSInfo.VersionString);
            //Console.WriteLine("Bits = {0}", OSInfo.Bits);

            String ver = Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.Minor;
            System.Console.WriteLine("OS Version:" + ver);
            String readableVer = osVerRepo[ver];
            return readableVer;
        }

        public static String ieVersion() {

            System.Console.WriteLine("IE Version:" + new WebBrowser().Version.Major);
            return "ie" + Convert.ToString(new WebBrowser().Version.Major);
        }
    }
}
