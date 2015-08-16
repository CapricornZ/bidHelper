using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using tobid.util.orc;
using tobid.rest;
using tobid.rest.position;

namespace tobid.scheduler.jobs
{
    public class LoginJob : ISchedulerJob
    {
        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(LoginJob));
        private static Object lockObj = new Object();
        private static LoginOperation operation;
        private static Config config;
        private static int executeCount = 1;

        private String endPoint;
        private OrcUtil orcCaptcha;
        public LoginJob(String endPoint, OrcUtil orcCaptcha)
        {
            this.endPoint = endPoint;
            this.orcCaptcha = orcCaptcha;
        }

        public static Boolean setConfig(Config config, LoginOperation operation)
        {
            logger.Info("setConfig {...}");
            Boolean rtn = false;
            if (Monitor.TryEnter(LoginJob.lockObj, 500))
            {
                if (config == null)
                    return false;

                if ((null == LoginJob.operation) || (operation.updateTime > LoginJob.operation.updateTime))
                {
                    LoginJob.executeCount = 0;
                    LoginJob.config = config;
                    LoginJob.operation = operation;

                    logger.DebugFormat("{{ bidNO:{0}, bidPassword:{1}, idCard:{2} }}",
                        config.no, config.passwd, config.pid);
                    logger.DebugFormat("startTime:{0} - expireTime:{1}",
                        LoginJob.operation.startTime,
                        LoginJob.operation.expireTime);

                    rtn = true;
                }

                Monitor.Exit(LoginJob.lockObj);
            }
            else
            {
                logger.Error("obtain LoginJob.lockObj timeout on setConfig(...)");
            }
            return rtn;
        }

        public void Execute()
        {
            DateTime now = DateTime.Now;
            logger.Debug(String.Format("NOW:{0}, {{Start:{1}, Expire:{2}, Count:{3}}}",
                now,
                LoginJob.operation.startTime, LoginJob.operation.expireTime,
                LoginJob.executeCount));

            if (Monitor.TryEnter(LoginJob.lockObj, 500))
            {
                if (now >= LoginJob.operation.startTime && now <= LoginJob.operation.expireTime && LoginJob.executeCount == 0)
                {
                    LoginJob.executeCount++;
                    logger.Debug("trigger Fired");
                }
                Monitor.Exit(LoginJob.lockObj);
            }
            else
            {
                logger.Error("obtain LoginJob.lockObj timeout on Execute(...)");
            }
        }
    }
}