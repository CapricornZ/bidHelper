using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.rest {

    public interface ITrigger{
        String category { get; set; }
    }

    /// <summary>
    /// 自定义Trigger配置
    /// </summary>
    public class Trigger : ITrigger{

        public String category { get; set; }

        public int deltaPrice { get; set; }
        public String priceTime { get; set; }
        public String captchaTime { get; set; }
        public String submitTime { get; set; }
        public int submitReachPrice { get; set; }
    }

    /// <summary>
    /// 自定义V2Trigger配置
    /// </summary>
    public class TriggerV2 : ITrigger{
        
        public string category { get; set; }
        public Trigger[] triggers { get; set; }
    }

    /// <summary>
    /// 自定义V3Trigger配置
    /// </summary>
    public class TriggerV3 : ITrigger {

        public string category { get; set; }
        public int deltaPrice { get; set; }
        public String priceTime { get; set; }
        public String submitTime { get; set; }
        public V3Common common { get; set; }
    }

    public class V3Common {
        public static V3Common commonConf { get; set; }

        public class Submit {
            public String submitTime { get; set; }
            public int percent { get; set; }
        }

        public class Trigger {

            public int delta { get; set; }
            public Submit[] submits { get; set; }
        }

        public String checkTime { get; set; }
        public Trigger[] triggers { get; set; }
    }

    public class Policy {
        public String category { get; set; }
        public ITrigger trigger { get; set; }
    }
}
