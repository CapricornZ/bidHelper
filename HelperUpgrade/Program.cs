using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
using System.Configuration;
using vbAccelerator.Components.Shell;
using System.Drawing;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace HelperUpgrade {

    public class DialogHelper {

        public static int Hues { get { return 0; } }
        public static int Saturation { get { return 1; } }
        public static int Brightness { get { return 2; } }

        public class HSBColor {

            public  float Hues { get; set; }
            public  float Saturation { get; set; }
            public  float Brightness { get; set; }

            public HSBColor(float Hues, float Saturation, float Brightness) {
                this.Hues = Hues;
                this.Saturation = Saturation;
                this.Brightness = Brightness;
            }
        }

        public static HSBColor rgb2hsb(Color point) {

            int[] rgb = new int[] { point.R, point.G, point.B};
            Array.Sort(rgb);
            int max = rgb[2];  
            int min = rgb[0];  
  
            float hsbB = max / 255.0f;  
            float hsbS = max == 0 ? 0 : (max - min) / (float) max;  
  
            float hsbH = 0;  
            if (max == point.R && point.G >= point.B) {  
                hsbH = (point.G - point.B) * 60f / (max - min) + 0;  
            } else if (max == point.R && point.G < point.B) {  
                hsbH = (point.G - point.B) * 60f / (max - min) + 360;  
            } else if (max == point.G) {  
                hsbH = (point.B - point.R) * 60f / (max - min) + 120;  
            } else if (max == point.B) {  
                hsbH = (point.R - point.G) * 60f / (max - min) + 240;  
            }

            return new HSBColor(hsbH, hsbS, hsbB);
        }

        static public Boolean isWifiGreen(Bitmap bitmap) {

            int green = 0;
            for(int x=0; x<bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++) {

                    HSBColor hsb = rgb2hsb(bitmap.GetPixel(x, y));
                    //if(hsb.Hues > 142 && hsb.Hues < 185)
                    //if(hsb.Hues >= 130 && hsb.Hues <= 148)//real
                    if(hsb.Hues >= 130 && hsb.Hues <= 187)//real&simu
                        green++;
                }
            int pixel = bitmap.Width * bitmap.Height;
            int percent = green * 100 / pixel;
            return percent >= 80;
        }

        static public Boolean isFinish(Bitmap bitmap) {

            int blue = 0;
            for(int x=0; x<bitmap.Width; x++)
                for (int y = 0; y < bitmap.Height; y++) {

                    HSBColor hsb = rgb2hsb(bitmap.GetPixel(x, y));
                    if (hsb.Hues > 200 && hsb.Hues < 215)
                        blue++;
                }

            int pixel = bitmap.Width * bitmap.Height;
            int percent = blue * 100 / pixel;
            System.Console.WriteLine(String.Format("{0}/{1} : {2}%", blue, pixel, percent));
            return percent>60;
        }
    }
    class Program {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {

            {
                Bitmap bitmap = (Bitmap)Bitmap.FromFile(@"C:\Users\0392xl\Desktop\8040.png");
                for(int x=0; x<bitmap.Width; x++)
                    for (int y = 0; y < bitmap.Height; y++) {
                        
                        Color color = bitmap.GetPixel(x, y);
                        if (color.GetHue() < 15 || color.GetHue() > 345)
                            ;
                        else
                            bitmap.SetPixel(x, y, Color.White);
                    }
                bitmap.Save(@"C:\Users\0392xl\Desktop\8040x.png");
            }

            String endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            while (true) {

                System.Console.WriteLine("请输入指令，\r\n\tU：更新、下载HELPER\r\n\tC：检查版本\r\n\tQ：结束应用");
                string command = System.Console.ReadLine();
                if ("U".Equals(command.ToUpper())) {
                    Download(endPoint);
                }
                if ("C".Equals(command.ToUpper())) {
                    Check(endPoint);
                }
                if ("Q".Equals(command.ToUpper())) {
                    break;
                }
            }
        }

        static void createShortCut(String filePath, String workingPath) {

            using (ShellLink shortcut = new ShellLink()) {
                shortcut.Target = filePath;
                shortcut.WorkingDirectory = workingPath;
                shortcut.Description = "助手";
                shortcut.DisplayMode = ShellLink.LinkDisplayMode.edmNormal;
                shortcut.Save("HELPER.lnk");
            }
        }

        static void Check(String endPoint) {

            String current = System.Environment.CurrentDirectory;
            String path = current + @"\Release\helper.exe";
            String localVer = "", remoteVer = "";
            try {
                Assembly assembly = Assembly.LoadFile(path);
                AssemblyName assemblyName = assembly.GetName();
                Version version = assemblyName.Version;
                localVer = version.ToString();
                System.Console.WriteLine("本地软件版本:" + localVer);
            } catch (Exception ex) {
            }

            try {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(endPoint + "/Release.ver");
                request.Proxy = null;

                Stream myStream = request.GetResponse().GetResponseStream();
                StreamReader sr = new StreamReader(myStream);
                remoteVer = sr.ReadLine();                
                myStream.Close();
                System.Console.WriteLine("远程软件版本:" + remoteVer);
            } catch (Exception ex) {
                logger.Error("检查更新失败");
                logger.Error(ex);
                return;
            }
        }

        static String readConfig(String config) {

            Configuration cfg = ConfigurationManager.OpenExeConfiguration(config);
            String principal = cfg.AppSettings.Settings["principal"].Value.ToString();
            String credential = cfg.AppSettings.Settings["credential"].Value.ToString();
            return principal + "," + credential;
        }

        static void writeConfig(String config, String identity) {
            
            string[] id = identity.Split(new char[] { ',' });
            Configuration cfg = ConfigurationManager.OpenExeConfiguration(config);
            cfg.AppSettings.Settings["principal"].Value = id[0];
            cfg.AppSettings.Settings["credential"].Value = id[1];
            cfg.Save(ConfigurationSaveMode.Modified);
        }

        static String GetAppConfig(Configuration cfg, String strkey) {
            foreach (string key in cfg.AppSettings.Settings.AllKeys)
                if (key.Equals(strkey))
                    return cfg.AppSettings.Settings[strkey].Value.ToString();
            return null;
        }

        static void Download(String endpoint) {

            String current = System.Environment.CurrentDirectory;
            String workDir = current + @"\Release";
            String fileDir = current + @"\Release\helper.exe";
            Boolean exists = File.Exists(fileDir+".config");
            String id = ",";
            if (exists)
                id = readConfig(fileDir);
            String strFileName = "Release.zip";
            FileStream FStream = new FileStream(strFileName, FileMode.Create);

            try {
                String url = endpoint + "/" + strFileName;
                logger.DebugFormat("DOWNLOADING Release.zip ... from {0}", url);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.Proxy = null;

                Stream myStream = request.GetResponse().GetResponseStream();
                byte[] btContent = new byte[512];
                int intSize = 0;
                intSize = myStream.Read(btContent, 0, 512);
                while (intSize > 0) {
                    FStream.Write(btContent, 0, intSize);
                    intSize = myStream.Read(btContent, 0, 512);
                }
                //关闭流
                myStream.Close();
            } catch (Exception ex) {
                logger.Error("DOWNLOADING Release.zip 错误");
                logger.Error(ex);
            }

            FStream.Seek(0, 0);
            ZipInputStream zip = new ZipInputStream(FStream);
            ZipEntry entry = null;
            try {

                while ((entry = zip.GetNextEntry()) != null) {
                    if (entry.IsFile) {

                        String[] array = entry.Name.Split(new char[] { '/' });
                        logger.DebugFormat("Create File {0}", entry.Name);
                        FileStream fs = new FileStream(entry.Name, FileMode.Create);
                        int size = 2048;
                        byte[] data = new byte[2048];
                        while (true) {
                            size = zip.Read(data, 0, data.Length);
                            if (size > 0)
                                fs.Write(data, 0, size);
                            else
                                break;
                        }
                        fs.Close();
                    }
                    if (entry.IsDirectory) {
                        Directory.CreateDirectory(entry.Name);
                        logger.DebugFormat("Create Directory {0}", entry.Name);
                    }
                }
            } catch (Exception ex) {
                
                logger.Error("更新Helper错误，请关闭正在运行的Helper，稍后再试");
                logger.Error(ex);
            }
            FStream.Close();
            System.Console.WriteLine("下载、更新成功");

            createShortCut(fileDir, workDir);
            writeConfig(fileDir, id);
        }
    }
}
