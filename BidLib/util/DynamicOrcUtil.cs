using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace tobid.util.orc {

    public class DynamicOrcUtil : IOrc {

        private int[] index;
        private int offsetX, offsetY;
        private int width, height;
        private int minNearSpots;
        private IDictionary<Bitmap, String> dict;
        private List<Bitmap> subImgs;
        public List<Bitmap> SubImgs { get { return this.subImgs; } }

        static public DynamicOrcUtil getInstance(tobid.rest.OrcConfig orcConfig, IDictionary<Bitmap, String> dict) {
            
            DynamicOrcUtil rtn = new DynamicOrcUtil();
            rtn.index = orcConfig.index;
            rtn.offsetX = orcConfig.offsetX;
            rtn.offsetY = orcConfig.offsetY;
            rtn.width = orcConfig.width;
            rtn.height = orcConfig.height;
            rtn.dict = dict;
            rtn.minNearSpots = orcConfig.minNearSpots;
            return rtn;
        }

        static private Boolean isWhite(Color point) {

            if (point.R + point.G + point.B > 100)
                return true;
            return false;
        }

        static private Point scanStart(Bitmap image) {

            Boolean bFound = false;
            int offsetX;
            for (offsetX = 0; !bFound && offsetX < 20; offsetX++) {

                List<int> rows = new List<int>();
                for (int x = 0; x < 15; x++) {

                    int black = 0;
                    for (int y = 0; y < image.Height; y++)
                        if (!isWhite(image.GetPixel(x + offsetX, y)))
                            black++;
                    rows.Add(black);
                }

                if (rows[0] == 0 && rows[1] != 0 && rows[2] != 0 && rows[3] != 0)
                    bFound = true;

                if(rows[0] == 0 && rows[1] == 0 && rows[2] == 0)
                    bFound = (rows[12] == 0 && rows[13] == 0 && rows[14] == 0);
            }

            offsetX -= 1;
            bFound = false;
            int offsetY = 0;
            for (int y = 0; !bFound && y < image.Height; y++) {

                int black = 0;
                for (int x = 0; x < 15; x++)
                    if (!isWhite(image.GetPixel(x + offsetX, y)))
                        black++;
                if (black != 0) {
                    offsetY = y;
                    bFound = true;
                }
            }
            return new Point(offsetX, offsetY-1<0?0:offsetY-1);
        }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="x">x偏移量</param>
        /// <param name="y">y偏移量</param>
        /// <returns></returns>
        public String IdentifyStringFromPic(Bitmap image, int x = 0, int y = 0) {

            this.subImgs = new List<Bitmap>();
            StringBuilder sb = new StringBuilder();
            ImageTool it = new ImageTool();
            it.setImage(image);
            it = it.changeToGrayImage().changeToBlackWhiteImage();
            if (minNearSpots != 0)
                it = it.removeBadBlock(1, 1, this.minNearSpots);
            it.Image.Save(@"p.bmp");

            Point start = DynamicOrcUtil.scanStart(it.Image);
            for (int i = 0; i < this.index.Length; i++) {

                Rectangle cloneRect = new Rectangle(this.index[i] + start.X, start.Y, this.width, this.height);
                Bitmap subImg = it.Image.Clone(cloneRect, it.Image.PixelFormat);
                this.subImgs.Add(subImg);
                subImg.Save(String.Format(@"p{0}.bmp", i));
                String s = OrcUtil.getSingleChar(subImg, this.dict);
                sb.Append(s);
            }
            return sb.ToString();
        }

        public Boolean IsBlank(Bitmap image, int x = 0, int y = 0)
        {
            ImageTool it = new ImageTool();
            it.setImage(image);
            it = it.changeToGrayImage().changeToBlackWhiteImage();
            if (minNearSpots != 0)
                it = it.removeBadBlock(1, 1, this.minNearSpots);
            int whitePercent = it.getWhitePercent();
            return whitePercent > 80;
        }
    }
}
