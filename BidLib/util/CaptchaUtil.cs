using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace tobid.util.orc{

    /// <summary>
    /// 根据提示获取有效验证码
    /// </summary>
    public class CaptchaUtil {

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(CaptchaUtil));
        private IOrc[] orcTips;
        public CaptchaUtil(IOrc[] tips) {
            this.orcTips = tips;
        }

        private List<Bitmap> subImgs;
        public List<Bitmap> SubImgs { get { return this.subImgs; } }

        public String getActive(String captcha, Bitmap bitmapTips) {

            String strActive = "";
            String strTips = this.orcTips[0].IdentifyStringFromPic(bitmapTips);
            logger.Debug(strTips);
            if ("请输入第25".Equals(strTips)) {
                strActive = captcha.Substring(1, 4);
                this.subImgs = this.orcTips[0].SubImgs;
            }
            else{
                strTips = this.orcTips[1].IdentifyStringFromPic(bitmapTips); 
                logger.Debug(strTips);
                if("请输入前4".Equals(strTips))
                    strActive = captcha.Substring(0, 4);
                if("请输入后4".Equals(strTips))
                    strActive = captcha.Substring(2, 4);
                this.subImgs = this.orcTips[1].SubImgs;
            }
            return strActive;
            /*int index = 0;
            String tips = this.orcTips[index].IdentifyStringFromPic(bitmapTips, 0, 0);
            String numbers = "";
            if (tips.StartsWith("请输入"))
                numbers = this.orcNo.IdentifyStringFromPic(bitmapTips);
            else {
                tips = this.orcTips[++index].IdentifyStringFromPic(bitmapTips);
                numbers = this.orcNo.IdentifyStringFromPic(bitmapTips, x: 20);
            }

            this.subImgs = new List<Bitmap>();
            for (int i = 0; i < this.orcTips[index].SubImgs.Count; i++)
                this.subImgs.Add(this.orcTips[index].SubImgs[i]);
            for (int i = 0; i < this.orcNo.SubImgs.Count; i++)
                this.subImgs.Add(this.orcNo.SubImgs[i]);

            char[] arrayno = numbers.ToCharArray();
            String start = String.Format("{0}", arrayno[0]);
            String end = String.Format("{0}", arrayno[1]);

            if ('第'.Equals(tips[3]))
                return captcha.Substring(Int16.Parse(start) - 1, Int16.Parse(end) - Int16.Parse(start) + 1);
            else if ('前'.Equals(tips[3]))
                return captcha.Substring(0, Int16.Parse(start));
            else if ('后'.Equals(tips[3]))
                return captcha.Substring(captcha.Length - Int16.Parse(start), Int16.Parse(start));
            else
                return captcha;
            return "";*/
        }
    }
}
