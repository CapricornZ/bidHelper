using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Net;

namespace tobid.util.http
{
    public class HttpUtil
    {
        private String basicAuth;
        public HttpUtil()
        {
            String user = System.Configuration.ConfigurationManager.AppSettings["principal"];
            String pass = System.Configuration.ConfigurationManager.AppSettings["credential"];
            this.basicAuth = user + ":" + pass;
        }

        /// <summary>
        /// 从Address地址获取数据
        /// </summary>
        /// <param name="address">url地址</param>
        /// <returns></returns>
        public Stream getAsBinary(String address, Proxy proxySetting = null)
        {
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(new Uri(address));
            if (null == proxySetting)
                httpReq.Proxy = null;//程序启动后第一次Request非常慢的解决法
            else {

                WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
                proxy.Credentials = new System.Net.NetworkCredential(proxySetting.user, proxySetting.pass, proxySetting.domain);
                httpReq.Proxy = new System.Net.WebProxy(new Uri(proxySetting.proxy), proxy.BypassProxyOnLocal, proxy.BypassList, proxy.Credentials);
            }

            httpReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(this.basicAuth))); 
            httpReq.Method = "GET";
            httpReq.Timeout = 1000 * 30;
            
            WebResponse webRespon = httpReq.GetResponse();
            Stream s = webRespon.GetResponseStream();
            return s;
        }

        public String getAsPlain(String address, Proxy proxySetting = null) {

            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(new Uri(address));
            if (null == proxySetting)
                httpReq.Proxy = null;//程序启动后第一次Request非常慢的解决法
            else {

                WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
                proxy.Credentials = new System.Net.NetworkCredential(proxySetting.user, proxySetting.pass, proxySetting.domain);
                httpReq.Proxy = new System.Net.WebProxy(new Uri(proxySetting.proxy), proxy.BypassProxyOnLocal, proxy.BypassList, proxy.Credentials);
            }

            httpReq.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(this.basicAuth)));
            httpReq.Method = "GET";
            httpReq.Timeout = 1000 * 30;

            WebResponse webRespon = httpReq.GetResponse();
            Stream s = webRespon.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            return sr.ReadToEnd();
        }

        /// <summary>
        /// 上传附件到address地址
        /// </summary>
        /// <param name="address">url地址</param>
        /// <param name="content">文件内容</param>
        /// <returns></returns>
        public String postByteAsFile(String address, byte[] content)
        {
            String returnValue = "++++++";

            //时间戳
            string strBoundary = "----------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + strBoundary + "--\r\n");

            //请求头部信息
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(strBoundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append("file");
            sb.Append("\"; filename=\"");
            sb.Append("captcha.jpg");
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("application/octet-stream");
            sb.Append("\r\n");
            sb.Append("\r\n");
            string strPostHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(strPostHeader);

            // 根据uri创建HttpWebRequest对象   
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(new Uri(address));
            httpReq.Method = "POST";

            //对发送的数据不使用缓存   
            httpReq.AllowWriteStreamBuffering = false;

            //设置获得响应的超时时间（300秒）   
            httpReq.Timeout = 300000;
            httpReq.ContentType = "multipart/form-data; boundary=" + strBoundary;
            long length = content.Length + postHeaderBytes.Length + boundaryBytes.Length;
            long fileLength = content.Length;
            httpReq.ContentLength = length;
            try
            {
                Stream postStream = httpReq.GetRequestStream();

                //发送请求头部消息
                postStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
                postStream.Write(content, 0, content.Length);

                //添加尾部的时间戳   
                postStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                postStream.Close();

                //获取服务器端的响应   
                WebResponse webRespon = httpReq.GetResponse();
                Stream s = webRespon.GetResponseStream();
                StreamReader sr = new StreamReader(s);

                //读取服务器端返回的消息   
                String sReturnString = sr.ReadLine();
                s.Close();
                sr.Close();
                returnValue = sReturnString;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                returnValue = "++++++";
            }
            finally
            {
            }

            return returnValue;
        }

        // <summary>
        /// 将本地文件上传到指定的服务器(HttpWebRequest方法)
        /// </summary>
        /// <param name="address">文件上传到的服务器</param>
        /// <param name="fileNamePath">要上传的本地文件（全路径）</param>
        /// <param name="saveName">文件上传后的名称</param>
        /// <returns>成功返回1，失败返回0</returns>   
        public String postFile(string address, string fileNamePath, string saveName)
        {
            String returnValue = "++++++";

            // 要上传的文件
            FileStream fs = new FileStream(fileNamePath, FileMode.Open, FileAccess.Read);
            BinaryReader r = new BinaryReader(fs);

            //时间戳
            string strBoundary = "----------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundaryBytes = Encoding.ASCII.GetBytes("\r\n--" + strBoundary + "--\r\n");

            //请求头部信息
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(strBoundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append("file");
            sb.Append("\"; filename=\"");
            sb.Append(saveName);
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("application/octet-stream");
            sb.Append("\r\n");
            sb.Append("\r\n");
            string strPostHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(strPostHeader);

            // 根据uri创建HttpWebRequest对象   
            HttpWebRequest httpReq = (HttpWebRequest)WebRequest.Create(new Uri(address));
            httpReq.Method = "POST";

            //对发送的数据不使用缓存   
            httpReq.AllowWriteStreamBuffering = false;

            //设置获得响应的超时时间（300秒）   
            httpReq.Timeout = 300000;
            httpReq.ContentType = "multipart/form-data; boundary=" + strBoundary;
            long length = fs.Length + postHeaderBytes.Length + boundaryBytes.Length;
            long fileLength = fs.Length;
            httpReq.ContentLength = length;
            try
            {
                //每次上传4k   
                int bufferLength = 4096;
                byte[] buffer = new byte[bufferLength];

                //已上传的字节数   
                long offset = 0;

                //开始上传时间   
                DateTime startTime = DateTime.Now;
                int size = r.Read(buffer, 0, bufferLength);
                Stream postStream = httpReq.GetRequestStream();

                //发送请求头部消息   
                postStream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
                while (size > 0)
                {
                    postStream.Write(buffer, 0, size);
                    offset += size;
                    size = r.Read(buffer, 0, bufferLength);
                }
                //添加尾部的时间戳   
                postStream.Write(boundaryBytes, 0, boundaryBytes.Length);
                postStream.Close();

                //获取服务器端的响应   
                WebResponse webRespon = httpReq.GetResponse();
                Stream s = webRespon.GetResponseStream();
                StreamReader sr = new StreamReader(s);

                //读取服务器端返回的消息   
                String sReturnString = sr.ReadLine();
                s.Close();
                sr.Close();
                returnValue = sReturnString;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                returnValue = "++++++";
            }
            finally
            {
                fs.Close();
                r.Close();
            }

            return returnValue;
        }
    }
}
