using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.rest
{
    public interface IOrcConfig {
        String category { get; set; }
    }

    public class OrcConfig : IOrcConfig {

        public int[] index { get; set; }
        public int offsetX { get; set; }
        public int offsetY { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int minNearSpots { get; set; }

        public String category { get; set; }
    }

    public class OrcTipConfig : IOrcConfig {

        public OrcConfig configTip { get; set; }
        public OrcConfig configNo { get; set; }

        public String category { get; set; }
    }

    public class GlobalConfig
    {
        public IList<IOrcConfig> orcConfigs { get; set; }
        public String repository { get; set; }
        public String tag { get; set; }

        public OrcConfig price { get { return this.orcConfigs[0] as OrcConfig; } }
        public OrcTipConfig tips0 { get { return this.orcConfigs[1] as OrcTipConfig; } }
        public OrcTipConfig tips1 { get { return this.orcConfigs[2] as OrcTipConfig; } }
        public OrcConfig loading { get { return this.orcConfigs[3] as OrcConfig; } }
        public OrcConfig captcha { get { return this.orcConfigs[4] as OrcConfig; } }
        public OrcConfig login { get { return this.orcConfigs[5] as OrcConfig; } }

        public Boolean dynamic { get; set; }
    }
}
