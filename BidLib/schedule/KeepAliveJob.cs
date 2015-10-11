using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;

using tobid.rest;
using tobid.rest.json;
using tobid.rest.position;
using tobid.util.http;
using tobid.util.orc;
using tobid.util;

namespace tobid.scheduler.jobs
{
    public delegate void ReceiveOperation(rest.Operation operation);
    public delegate void ReceiveLogin(rest.Operation operation, rest.Config config);
    
    /// <summary>
    /// KeepAlive : 向服务器发布主机名，获取配置项
    /// </summary>
    public class KeepAliveJob : ISchedulerJob {

        private static log4net.ILog logger = log4net.LogManager.GetLogger("KeepAliveJob");

        private ReceiveLogin receiveLogin;
        private ReceiveOperation[] receiveOperation;
        private IRepository repository;

        /// <summary>
        /// 使用服务器策略或本地策略
        /// true : 本地策略
        /// false: 服务器策略
        /// </summary>
        public Boolean isManual { get; set; }
        public String EndPoint { get; set; }

        public KeepAliveJob(String endPoint, ReceiveLogin receiveLogin, ReceiveOperation[] receiveOperation, IRepository repo){

            this.isManual = false;
            this.EndPoint = endPoint;
            this.receiveLogin = receiveLogin;
            this.receiveOperation = receiveOperation;
            this.repository = repo;
        }

        class Filter
        {
            public static Client remain(String tag, Client client)
            {
                if (client.operation == null)
                    return client;

                for (int i = client.operation.Count - 1; i >= 0; i--)
                {
                    if (!client.operation[i].tag.Equals(tag))
                        client.operation.RemoveAt(i);
                }
                return client;
            }
        }

        public void Execute() {

            logger.Debug("KeepAliveJob.Execute()");
            string hostName = System.Net.Dns.GetHostName();
            //String epKeepAlive = this.EndPoint + "/command/keepAlive.do";
            String epKeepAlive = this.EndPoint + "/rest/service/command/keepAlive";
            RestClient restKeepAlive = new RestClient(endpoint: epKeepAlive, method: HttpVerb.POST);
            String rtn = restKeepAlive.MakeRequest(String.Format("?ip={0}", hostName));
            tobid.rest.Client client = Newtonsoft.Json.JsonConvert.DeserializeObject<tobid.rest.Client>(rtn, new OperationConvert());
            client = Filter.remain(this.repository.category, client);

            if (!this.isManual && client.operation != null && client.operation.Count > 0)
            {
                foreach (tobid.rest.Operation operation in client.operation)
                {
                    if (operation is tobid.rest.LoginOperation){

                        if (LoginJob.setConfig(client.config, operation as LoginOperation))
                            this.receiveLogin(operation, client.config);
                    } else if (operation is tobid.rest.Step1Operation) {

                        if(SubmitPriceStep1Job.setConfig(operation as Step1Operation))
                            this.receiveOperation[0](operation);
                    } else if (operation is tobid.rest.Step2Operation) {

                        if (SubmitPriceStep2Job.setConfig(operation as Step2Operation))
                            this.receiveOperation[1](operation);
                    }
                }
            }
        }
    }
}
