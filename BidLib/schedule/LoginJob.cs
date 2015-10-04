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

        private IOrc orcCaptcha;
        public LoginJob(IOrc orcCaptcha){

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
            } else
                logger.Error("obtain LoginJob.lockObj timeout on setConfig(...)");

            return rtn;
        }

        static void ie_DocumentComplete(object pDisp, ref object URL) {
            
            logger.Info("IE document loaded completely");
            DocComplete.Set();
        }

        private static System.Threading.AutoResetEvent DocComplete = new System.Threading.AutoResetEvent(false);

        public void Execute()
        {
            DateTime now = DateTime.Now;
            if (LoginJob.operation == null)
                logger.Debug("LoginJob.OPERATION NOT SET");
            else
                logger.Debug(String.Format("{0} {{Start:{1}, Expire:{2}, Count:{3}}}",
                    LoginJob.config.pname, LoginJob.operation.startTime, LoginJob.operation.expireTime,
                    LoginJob.executeCount));

            if (Monitor.TryEnter(LoginJob.lockObj, 500))
            {
                //if (now >= LoginJob.operation.startTime && now <= LoginJob.operation.expireTime && LoginJob.executeCount == 0)
                //{
                //   LoginJob.executeCount++;
                //    logger.Debug("trigger Fired");
                //}
                if (null == LoginJob.config) {

                    logger.Error("Corresponding config is not set");
                    return;
                }

                SHDocVw.InternetExplorer Browser = tobid.util.IEUtil.findBrowser();
                if (null != Browser)
                {
                    //Browser.DocumentComplete += new SHDocVw.DWebBrowserEvents2_DocumentCompleteEventHandler(ie_DocumentComplete);
                    //Browser.Navigate(LoginJob.operation.url);
                    //DocComplete.WaitOne();
                    //"testBtnConfirm";
                    //"protocolBtnConfirm";
                    mshtml.IHTMLDocument2 doc2 = (mshtml.IHTMLDocument2)Browser.Document;
                    mshtml.IHTMLElement confirm1 = doc2.all.item("testBtnConfirm") as mshtml.IHTMLElement;
                    confirm1.click();
                    System.Threading.Thread.Sleep(1000);

                    mshtml.IHTMLElement confirm2 = doc2.all.item("protocolBtnConfirm") as mshtml.IHTMLElement;
                    confirm2.click();
                    System.Threading.Thread.Sleep(1000);

                    //"bidnumber";
                    //"bidpassword";
                    //"idcard";
                    //"imagenumber";
                    //"imgcode";
                    //"btnlogin";
                    mshtml.IHTMLElement imgCode = doc2.images.item("imgcode") as mshtml.IHTMLElement;
                    mshtml.HTMLBody body = doc2.body as mshtml.HTMLBody;
                    mshtml.IHTMLControlRange rang = body.createControlRange() as mshtml.IHTMLControlRange;
                    mshtml.IHTMLControlElement img = imgCode as mshtml.IHTMLControlElement;
                    rang.add(img);
                    rang.execCommand("Copy", false, null);  //拷贝到内存
                    System.Drawing.Image numImage = System.Windows.Forms.Clipboard.GetImage();
                    System.IO.MemoryStream ms = new System.IO.MemoryStream();
                    numImage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                    String strCaptcha = this.orcCaptcha.IdentifyStringFromPic(new System.Drawing.Bitmap(ms), 5);

                    mshtml.IHTMLElementCollection inputs = (mshtml.IHTMLElementCollection)doc2.all.tags("INPUT");
                    mshtml.HTMLInputElement input1 = (mshtml.HTMLInputElement)inputs.item("bidnumber");
                    input1.value = LoginJob.config.no;
                    mshtml.HTMLInputElement input2 = (mshtml.HTMLInputElement)inputs.item("bidpassword");
                    input2.value = LoginJob.config.passwd;
                    mshtml.HTMLInputElement input3 = (mshtml.HTMLInputElement)inputs.item("idcard");
                    input3.value = LoginJob.config.pid;
                    mshtml.HTMLInputElement input4 = (mshtml.HTMLInputElement)inputs.item("imagenumber");
                    input4.value = strCaptcha;

                    mshtml.IHTMLElement loginBtn = doc2.all.item("btnlogin") as mshtml.IHTMLElement;
                }
                else
                {
                    logger.Error("IE instance not found");
                }
                Monitor.Exit(LoginJob.lockObj);
            }
            else
                logger.Error("obtain LoginJob.lockObj timeout on Execute(...)");
        }
    }
}