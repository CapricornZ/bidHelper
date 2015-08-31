using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using tobid.util;

namespace tobid.util.orc
{
    public interface IOrc {

        String IdentifyStringFromPic(Bitmap image, int x = 0, int y = 0);
        List<Bitmap> SubImgs { get; }
    }

    public class OrcUtilEx : IOrc {

        class OffsetX : IComparable<OffsetX>{
            public int offsetX { get; set; }
            public int offsetY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public IDictionary<Bitmap, String> dict { get; set; }

            public OffsetX(int x, int y, int width, int height, IDictionary<Bitmap, String> dict) {
                this.offsetX = x;
                this.offsetY = y;
                this.width = width;
                this.height = height;
                this.dict = dict;
            }

            public int CompareTo(OffsetX obj) {
                return this.offsetX - obj.offsetX;
            }
        }

        private rest.OrcTipConfig config;
        private IDictionary<Bitmap, String> dictTip;
        private IDictionary<Bitmap, String> dictNo;
        private List<Bitmap> subImgs;
        public List<Bitmap> SubImgs { get { return this.subImgs; } }

        static public IOrc getInstance(tobid.rest.OrcTipConfig orcConfig, 
            IDictionary<Bitmap, String> dictTip, IDictionary<Bitmap, String> dictNo) {

            OrcUtilEx rtn = new OrcUtilEx();
            rtn.config = orcConfig;
            rtn.dictNo = dictNo;
            rtn.dictTip = dictTip;
            return rtn;
        }

        public String IdentifyStringFromPic(Bitmap image, int x = 0, int y = 0) {

            this.subImgs = new List<Bitmap>();
            StringBuilder sb = new StringBuilder();
            ImageTool it = new ImageTool();
            it.setImage(image);
            it = it.changeToGrayImage().changeToBlackWhiteImage();
            if (config.configTip.minNearSpots != 0)
                it = it.removeBadBlock(1, 1, config.configTip.minNearSpots);

            List<OffsetX> offset = new List<OffsetX>();
            for (int i = 0; i < config.configTip.offsetX.Length; i++)
                offset.Add(new OffsetX(config.configTip.offsetX[i], config.configTip.offsetY,
                    config.configTip.width, config.configTip.height, 
                    this.dictTip));
            for(int i=0; i<config.configNo.offsetX.Length; i++)
                offset.Add(new OffsetX(config.configNo.offsetX[i], config.configNo.offsetY,
                    config.configNo.width, config.configNo.height, this.dictNo));

            offset.Sort();
            foreach(OffsetX element in offset){

                Rectangle cloneRect = new Rectangle(element.offsetX + x, element.offsetY + y, 
                    element.width, element.height);
                Bitmap subImg = it.Image.Clone(cloneRect, it.Image.PixelFormat);
                this.subImgs.Add(subImg);
                String s = OrcUtil.getSingleChar(subImg, element.dict);
                sb.Append(s);
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 识别图片
    /// </summary>
    public class OrcUtil : IOrc {

        internal static String getSingleChar(Bitmap img, IDictionary<Bitmap, String> dict)
        {
            String result = "";
            int width = img.Width;
            int height = img.Height;
            int min = width * height;
            foreach (Bitmap bi in dict.Keys)
            {
                if (width > bi.Width || height > bi.Height)
                    continue;

                int count = 0;
                for (int x = 0; x < width; ++x)
                    for (int y = 0; y < height; ++y)
                    {
                        Color imgPoint = img.GetPixel(x, y);
                        Color biPoint = bi.GetPixel(x, y);
                        if (isWhite(imgPoint) != isWhite(biPoint))
                        {
                            count++;
                            if (count >= min)
                                goto Label1;
                        }
                    }
            Label1:
                if (count < min)
                {
                    min = count;
                    result = dict[bi];
                }
            }
            return result;
        }

        static private int isWhite(Color point)
        {
            if (point.R + point.G + point.B > 100)
                return 1;
            return 0;
        }

        private int[] offsetX;
        private int offsetY;
        private int width, height;
        private int minNearSpots;
        private IDictionary<Bitmap, String> dict;
        private List<Bitmap> subImgs;
        public List<Bitmap> SubImgs { get { return this.subImgs; } }

        static public OrcUtil getInstance(tobid.rest.OrcConfig orcConfig, IDictionary<Bitmap, String> dict)
        {
            OrcUtil rtn = getInstance(orcConfig.offsetX, orcConfig.offsetY, orcConfig.width, orcConfig.height, dict);
            rtn.minNearSpots = orcConfig.minNearSpots;
            return rtn;
        }

        static public OrcUtil getInstance(int[] offsetX, int offsetY, int width, int height, IDictionary<Bitmap, String> dict)
        {
            OrcUtil rtn = new OrcUtil();
            rtn.offsetX = offsetX;
            rtn.offsetY = offsetY;
            rtn.width = width;
            rtn.height = height;
            rtn.dict = dict;
            return rtn;
        }

        /// <summary>
        /// 创建Orc实例(from Stream)
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        static public OrcUtil getInstance(int[] offsetX, int offsetY, int width, int height, Stream resource)
        {
            OrcUtil rtn = new OrcUtil();
            rtn.offsetX = offsetX;
            rtn.offsetY = offsetY;
            rtn.width = width;
            rtn.height = height;
            rtn.dict = new Dictionary<Bitmap, String>();

            System.Resources.ResXResourceReader resxReader = new System.Resources.ResXResourceReader(resource);
            IDictionaryEnumerator enumerator = resxReader.GetEnumerator();
            while (enumerator.MoveNext())
            {
                DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                rtn.dict.Add((Bitmap)entry.Value, (String)entry.Key);
            }
            return rtn;
        }

        /// <summary>
        /// 创建Orc实例(from Directory)
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="resource"></param>
        /// <returns></returns>
        static public OrcUtil getInstance(int[] offsetX, int offsetY, int width, int height, String dictPath)
        {
            OrcUtil rtn = new OrcUtil();
            rtn.offsetX = offsetX;
            rtn.offsetY = offsetY;
            rtn.width = width;
            rtn.height = height;
            rtn.dict = new Dictionary<Bitmap, String>();

            String[] files = System.IO.Directory.GetFiles(dictPath);
            foreach (String file in files)
            {
                String name = new System.IO.FileInfo(file).Name;
                String[] array = name.Split(new char[] { '.' });
                Bitmap bitmap = new Bitmap(file);
                rtn.dict.Add(bitmap, array[0]);
            }
            return rtn;
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="x">x偏移量</param>
        /// <param name="y">y偏移量</param>
        /// <returns></returns>
        public String IdentifyStringFromPic(Bitmap image, int x = 0, int y = 0)
        {
            this.subImgs = new List<Bitmap>();
            StringBuilder sb = new StringBuilder();
            ImageTool it = new ImageTool();
            it.setImage(image);
            it = it.changeToGrayImage().changeToBlackWhiteImage();
            if (minNearSpots != 0)
                it = it.removeBadBlock(1, 1, this.minNearSpots);
            it.Image.Save(@"p.bmp");
            for (int i = 0; i < this.offsetX.Length; i++)
            {
                Rectangle cloneRect = new Rectangle(this.offsetX[i] + x, this.offsetY + y, this.width, this.height);
                Bitmap subImg = it.Image.Clone(cloneRect, it.Image.PixelFormat);
                this.subImgs.Add(subImg);
                subImg.Save(String.Format(@"p{0}.bmp", i));
                String s = OrcUtil.getSingleChar(subImg, this.dict);
                sb.Append(s);
            }
            return sb.ToString();
        }
    }

    
}
