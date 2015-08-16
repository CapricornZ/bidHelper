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
    
    /// <summary>
    /// KeepAlive : 向服务器发布主机名，获取配置项
    /// </summary>
    public class KeepAliveJob : ISchedulerJob
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger("KeepAliveJob");

        private ReceiveOperation receiveOperation;
        /// <summary>
        /// 使用服务器策略或本地策略
        /// true : 本地策略
        /// false: 服务器策略
        /// </summary>
        public Boolean isManual { get; set; }
        public String EndPoint { get; set; }

        public KeepAliveJob(String endPoint, ReceiveOperation receiveOperation)
        {
            this.isManual = false;
            this.EndPoint = endPoint;
            this.receiveOperation = receiveOperation;
        }

        public void Execute()
        {
            logger.Debug("KeepAliveJob.Execute()");
            string hostName = System.Net.Dns.GetHostName();
            String epKeepAlive = this.EndPoint + "/command/keepAlive.do";
            RestClient restKeepAlive = new RestClient(endpoint: epKeepAlive, method: HttpVerb.POST);
            String rtn = restKeepAlive.MakeRequest(String.Format("?ip={0}", hostName));
            tobid.rest.Client client = Newtonsoft.Json.JsonConvert.DeserializeObject<tobid.rest.Client>(rtn, new OperationConvert());
            
            if (!this.isManual && client.operation != null && client.operation.Length > 0)
            {
                foreach (tobid.rest.Operation operation in client.operation)
                {
                    if (operation is tobid.rest.BidOperation)
                    {
                        if (SubmitPriceJob.setConfig(operation as BidOperation))
                            this.receiveOperation(operation);
                    }
                    else if (operation is tobid.rest.LoginOperation)
                    {
                        LoginJob.setConfig(client.config, operation as LoginOperation);
                    }
                }
            }
        }
    }
}
