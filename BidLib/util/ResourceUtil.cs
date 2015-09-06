using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

using tobid.rest;
using tobid.rest.json;
using tobid.util.orc;
using tobid.util.http;

namespace tobid.util
{
    public interface IGlobalConfig
    {
        String tag { get; }
        IOrc Price { get; }
        IOrc Loading { get; }
        IOrc[] Tips { get; }
        IOrc Captcha { get; }
        IOrc Login { get; }
    }

    public class Resource : IGlobalConfig
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(Resource));
        private orc.IOrc m_login;
        private orc.IOrc m_captcha;
        private orc.IOrc m_price;
        private orc.IOrc m_loading;
        private orc.IOrc[] m_tips;
        private String m_tag;

        public static Resource getInstance(String endPoint)
        {
            Resource rtn = new Resource();
            String hostName = System.Net.Dns.GetHostName();
            String epKeepAlive = endPoint + "/rest/service/command/global";
            //String epKeepAlive = endPoint + "/command/global";
            RestClient restGlobalConfig = new RestClient(endpoint: epKeepAlive, method: HttpVerb.GET);

            GlobalConfig global = null;
            Stream stream = null;
            String urlResource = null;
            try {
                logger.DebugFormat("获取全局配置...【{0}】", String.Format("{0}?fromHost={1}", epKeepAlive, hostName));
                String jsonResponse = restGlobalConfig.MakeRequest(String.Format("?fromHost={0}", hostName));
                global = Newtonsoft.Json.JsonConvert.DeserializeObject<GlobalConfig>(jsonResponse, new ConfigConvert());
            } catch (Exception ex) {
                logger.ErrorFormat("获取全局配置异常:{0}", ex);
            }

            try {
                urlResource = endPoint + global.repository;
                logger.DebugFormat("获取全局配置资源...【{0}】", urlResource);
                stream = new HttpUtil().getAsBinary(urlResource);
            } catch (Exception ex) {
                logger.ErrorFormat("获取全局配置资源异常:{0}", ex);
            }

            IDictionary<Bitmap, String> dictPrice = new Dictionary<Bitmap, String>();
            IDictionary<Bitmap, String> dictLoading = new Dictionary<Bitmap, String>();
            IDictionary<Bitmap, String> dictTips = new Dictionary<Bitmap, String>();
            IDictionary<Bitmap, String> dictTipsNo = new Dictionary<Bitmap, String>();
            IDictionary<Bitmap, String> dictCaptcha = new Dictionary<Bitmap, String>();
            IDictionary<Bitmap, String> dictLogin = new Dictionary<Bitmap, String>();

            ZipInputStream zip = new ZipInputStream(stream);
            ZipEntry entry = null;
            logger.Debug("解析资源...");
            while ((entry = zip.GetNextEntry()) != null) {

                if (entry.IsFile) {

                    logger.Debug(entry.Name);
                    MemoryStream binaryStream = new MemoryStream();
                    int size = 2048;
                    byte[] data = new byte[2048];
                    while (true) {

                        size = zip.Read(data, 0, data.Length);
                        if (size > 0)
                            binaryStream.Write(data, 0, size);
                        else
                            break;
                    }

                    String[] array = entry.Name.Split(new char[] { '.', '/' });
                    Bitmap bitmap = new Bitmap(binaryStream);
                    if (entry.Name.ToLower().StartsWith("captcha/"))
                        dictCaptcha.Add(bitmap, array[array.Length - 2]);
                    else if (entry.Name.ToLower().StartsWith("price/"))
                        dictPrice.Add(bitmap, array[array.Length - 2]);
                    else if (entry.Name.ToLower().StartsWith("loading/"))
                        dictLoading.Add(bitmap, array[array.Length - 2]);
                    else if (entry.Name.ToLower().StartsWith("login/"))
                        dictLogin.Add(bitmap, array[array.Length - 2]);
                    else if (entry.Name.ToLower().StartsWith("captcha.tip/")) {
                        if (entry.Name.ToLower().StartsWith("captcha.tip/no/"))
                            dictTipsNo.Add(bitmap, array[array.Length - 2]);
                        else
                            dictTips.Add(bitmap, array[array.Length - 2]);
                    }
                }
            }

            rtn.m_tag = global.tag;
            rtn.m_login = OrcUtil.getInstance(global.login, dictLogin);
            if(!global.dynamic)
                rtn.m_captcha = OrcUtil.getInstance(global.captcha, dictCaptcha);
            else
                rtn.m_captcha = DynamicOrcUtil.getInstance(global.captcha, dictCaptcha);
            rtn.m_price = OrcUtil.getInstance(global.price, dictPrice);
            rtn.m_tips = new IOrc[]{
                OrcUtilEx.getInstance(global.tips0, dictTips, dictTipsNo),
                OrcUtilEx.getInstance(global.tips1, dictTips, dictTipsNo)
            };

            rtn.m_loading = OrcUtil.getInstance(global.loading, dictLoading);
            return rtn;
        }

        #region IGlobalConfig接口
        public String tag { get { return this.m_tag; } }
        public IOrc Price { get { return this.m_price; } }
        public IOrc Loading { get { return this.m_loading; } }
        public IOrc[] Tips { get { return this.m_tips; } }
        public IOrc Captcha { get { return this.m_captcha; } }
        public IOrc Login { get { return this.m_login; } }
        #endregion
    }
}
