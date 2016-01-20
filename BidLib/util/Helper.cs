using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace tobid.util {

    public class Util {

        public static String osVersion() {

            IDictionary<String, String> verRepo = new Dictionary<String, String>();
            verRepo.Add("10.0", "win10");
            verRepo.Add("6.3", "win8.1");
            verRepo.Add("6.2", "win8");
            verRepo.Add("6.1", "win7");
            verRepo.Add("6.0", "winVista");
            verRepo.Add("5.2", "win2003");
            verRepo.Add("5.1", "winXP");
            verRepo.Add("5.0", "win2000");

            String ver = Environment.OSVersion.Version.Major + "." + Environment.OSVersion.Version.MajorRevision;
            String readableVer = verRepo[ver];
            return readableVer;
        }

        public static String ieVersion() {

            return "ie" + Convert.ToString(new WebBrowser().Version.Major);
        }
    }
}
