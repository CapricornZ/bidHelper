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

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace HelperUpgrade {
    class Program {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args) {

            String endPoint = ConfigurationManager.AppSettings["ENDPOINT"];
            while (true) {

                System.Console.WriteLine("请输入指令，U：更新、下载HELPER，C：检查版本，Q：结束应用");
                string command = System.Console.ReadLine();
                if("U".Equals(command.ToUpper())){
                    Download(endPoint);
                }
                if("C".Equals(command.ToUpper())){
                    Check(endPoint);
                }
                if("Q".Equals(command.ToUpper())){
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
