using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.util.http;

namespace tobid.rest.f9{

    public class Action {
        public String submit { get; set; }
        public int delta { get; set; }
        public int percent { get; set; }
    }

    public class Trigger {
        public String fire { get; set; }
        public Action[] actions { get; set; }
    }

    public class F9Common {

        public Trigger[] triggers { get; set; }

        public Action randomAction() {

            int second = DateTime.Now.Second;
            bool bFound = false;
            tobid.rest.f9.Action rtn = null;
            tobid.rest.f9.Action[] actions = null;
            for (int i = 0; !bFound && i < this.triggers.Length; i++) {
                if (second == Convert.ToInt16(this.triggers[i].fire)) {
                    logger.DebugFormat("select ACTION in trigger[{0}s]", this.triggers[i].fire);
                    actions = this.triggers[i].actions;
                    bFound = true;
                }
            }

            long tick = DateTime.Now.Ticks;
            Random random = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));
            if (bFound) {

                //按比例
                int rand = random.Next(1, 100);
                bool bStop = false;
                int i = 0;
                for (i = 0; !bStop && i < actions.Length; i++) {
                    bStop = actions[i].percent >= rand;
                }
                rtn = actions[i - 1];

                //平分
                //rtn = actions[random.Next(actions.Length)];
                logger.DebugFormat("select ACTION[random:{2}]:{{delta:{0}, percent:{1}%}}", rtn.delta, rtn.percent, rand);
            }
            return rtn;
        }

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(F9Common));
        public static F9Common getInstance(String endPoint) {

            /*Trigger t = new Trigger();
            t.fire = "48";
            t.actions = new Action[]{
                new Action{submit="53", delta=300},
                new Action{submit="54", delta=400},
                new Action{submit="55", delta=500}
            };
            F9Common f9 = new F9Common {
                triggers = new Trigger[]{t}
            };

            String s = Newtonsoft.Json.JsonConvert.SerializeObject(t);
            System.Console.WriteLine(s);*/

            String epKeepAlive = endPoint + "/rest/service/command/common/F9?force=false";
            RestClient restF9 = new RestClient(endpoint: epKeepAlive, method: HttpVerb.GET);

            F9Common f9Common = null;
            try {
                logger.DebugFormat("获取F9策略...【{0}】", epKeepAlive);
                String jsonResponse = restF9.MakeRequest(epKeepAlive);
                f9Common = Newtonsoft.Json.JsonConvert.DeserializeObject<F9Common>(jsonResponse);
            } catch (Exception ex) {
                logger.ErrorFormat("获取F9策略异常:{0}", ex);
                throw ex;
            }
            return f9Common;
        }
    }
}
