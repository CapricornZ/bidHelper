using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.IO;

namespace tobid.util.http
{
    public enum HttpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public class RestClient
    {
        public string EndPoint { get; set; }
        public HttpVerb Method { get; set; }
        public string ContentType { get; set; }
        public string PostData { get; set; }
        private string basicAuth;

        public RestClient(string endpoint, HttpVerb method)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "text/xml";
            this.PostData = "";

            String user = System.Configuration.ConfigurationManager.AppSettings["principal"];
            String pass = System.Configuration.ConfigurationManager.AppSettings["credential"];
            this.basicAuth = user + ":" + pass;
        }

        /*public RestClient(string endpoint, HttpVerb method, string postData)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "text/xml";
            this.PostData = postData;
        }*/

        public RestClient(string endpoint, HttpVerb method, Object postObj)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "application/json;charset=UTF-8";
            this.PostData = Newtonsoft.Json.JsonConvert.SerializeObject(postObj);
        }

        public RestClient(string endpoint, HttpVerb method, String basicAuth){

            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "text/xml";
            this.PostData = "";

            this.basicAuth = basicAuth;
        }

        public string MakeRequest(Boolean isBasicAuth=true)
        {
            return MakeRequest(null, isBasicAuth:isBasicAuth);
        }

        public string MakeRequest(string parameters, Boolean isBasicAuth=true)
        {
            WebRequest request = WebRequest.Create(parameters == null ? EndPoint : EndPoint + parameters);
            request.Proxy = null;//程序启动后第一次Request非常慢解决法
            //WebProxy proxy = new WebProxy(new Uri("10.16.95.12:80"));
            //proxy.Credentials = new NetworkCredential("hq\0392xl", "Pass2012");
            //request.Proxy = proxy;

            request.Method = Method.ToString();
            if(isBasicAuth)
                request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new ASCIIEncoding().GetBytes(this.basicAuth))); 
            request.ContentLength = 0;
            request.ContentType = ContentType;

            if (!string.IsNullOrEmpty(PostData) && Method == HttpVerb.POST)
            {
                var encoding = new UTF8Encoding();
                var bytes = Encoding.GetEncoding("utf-8").GetBytes(PostData);
                request.ContentLength = bytes.Length;

                using (var writeStream = request.GetRequestStream())
                {
                    writeStream.Write(bytes, 0, bytes.Length);
                }
            }

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                var responseValue = string.Empty;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    var message = String.Format("Request failed. Received HTTP {0}", response.StatusCode);
                    throw new ApplicationException(message);
                }

                // grab the response
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                        using (var reader = new StreamReader(responseStream))
                        {
                            responseValue = reader.ReadToEnd();
                        }
                }

                return responseValue;
            }
        }

    } // class
}