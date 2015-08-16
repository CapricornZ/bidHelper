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

        public RestClient(string endpoint, HttpVerb method)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "text/xml";
            this.PostData = "";
        }

        public RestClient(string endpoint, HttpVerb method, string postData)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "text/xml";
            this.PostData = postData;
        }

        public RestClient(string endpoint, HttpVerb method, Object postObj)
        {
            this.EndPoint = endpoint;
            this.Method = method;
            this.ContentType = "application/json;charset=UTF-8";
            this.PostData = Newtonsoft.Json.JsonConvert.SerializeObject(postObj);
        }

        public string MakeRequest()
        {
            return MakeRequest(null);
        }

        public string MakeRequest(string parameters)
        {
            var request = (HttpWebRequest)WebRequest.Create(parameters == null ? EndPoint : EndPoint + parameters);

            request.Method = Method.ToString();
            request.ContentLength = 0;
            request.ContentType = ContentType;

            if (!string.IsNullOrEmpty(PostData) && Method == HttpVerb.POST)
            {
                var encoding = new UTF8Encoding();
                //var bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(PostData);
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