using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace tobid.util.orc{

    public class ImageTool {
        private Bitmap image;
        private int width;
        private int height;

        static private Color WHITE = Color.FromArgb(255, 255, 255);
        static private Color BLACK = Color.FromArgb(0, 0, 0);

        public Bitmap Image {
            get { return this.image; }
        }

        public void setImage(Bitmap image) {
            this.image = image;
            this.width = image.Width;
            this.height = image.Height;
        }

        /// <summary>
        /// 灰度处理
        /// </summary>
        /// <returns></returns>
        public ImageTool changeToGrayImage() {
            int gray;
            Color point;
            for (int i = 0; i < this.height; i++)
                for (int j = 0; j < this.width; j++) {
                    point = image.GetPixel(j, i);
                    gray = (point.R + point.G + point.B) / 3;
                    image.SetPixel(j, i, Color.FromArgb(gray, gray, gray));
                }
            return this;
        }

        /// <summary>
        /// 二值化
        /// </summary>
        /// <returns></returns>
        public ImageTool changeToBlackWhiteImage() {
            int avgGrayValue = this.getAvgValue();
            for (int i = 0; i < this.height; i++)
                for (int j = 0; j < this.width; j++) {
                    Color point = this.image.GetPixel(j, i);
                    image.SetPixel(j, i, point.R < avgGrayValue ? BLACK : WHITE);
                }
            return this;
        }

        /// <summary>
        /// 中值过滤
        /// </summary>
        /// <param name="whiteAreaMinPercent"></param>
        /// <param name="removeLighter"></param>
        /// <returns></returns>
        public ImageTool middleValueFilter(int whiteAreaMinPercent, Boolean removeLighter) {
            int modify = 0;
            int avg = this.getAvgValue();
            while (this.getWhitePercent() < whiteAreaMinPercent) {
                for (int i = 0; i < this.height; i++)
                    for (int j = 0; j < this.width; j++) {
                        Color point = this.image.GetPixel(j, i);
                        if (removeLighter) {
                            if (((point.R + point.G + point.B) / 3) > avg - modify)
                                this.image.SetPixel(j, i, WHITE);
                        } else {
                            if (((point.R + point.G + point.B) / 3) < avg + modify)
                                this.image.SetPixel(j, i, WHITE);
                        }
                    }
                modify++;
            }
            return this;
        }

        /// <summary>
        /// 去除噪点和单点组成的干扰线 
        /// 注意: 去除噪点之前应该对图像黑白化
        /// </summary>
        /// <param name="blockWidth"></param>
        /// <param name="blockHeight"></param>
        /// <param name="neighborhoodMinCount"></param>
        /// <returns></returns>
        public ImageTool removeBadBlock(int blockWidth, int blockHeight, int neighborhoodMinCount) {
            int val;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) {
                    int counter = 0;//初始化邻居数为0
                    int topLeftXIndex = x - 1;
                    int topLeftYIndex = y - 1;
                    //x1 y1是以x,y左上角点为顶点的矩形,该矩形包围在传入的矩形的外围,计算传入的矩形的有效邻居数目
                    if (isBlackBlock(x, y, blockWidth, blockHeight)) {//只有当块是全黑色才计算
                        for (int x1 = topLeftXIndex; x1 <= topLeftXIndex + blockWidth + 1; x1++)
                            for (int y1 = topLeftYIndex; y1 <= topLeftYIndex + blockHeight + 1; y1++) {
                                //判断这个点是否存在
                                if (x1 < width && x1 >= 0 && y1 < height && y1 >= 0) {
                                    //判断这个点是否是传入矩形的外围点
                                    if (x1 == topLeftXIndex || x1 == topLeftXIndex + blockWidth + 1
                                            || y1 == topLeftYIndex || y1 == topLeftYIndex + blockHeight + 1) {
                                        //这里假定图像已经被黑白化,取Red值认为不是0就是255
                                        val = image.GetPixel(x1, y1).R;
                                        //如果这个邻居是黑色,就把中心点的有效邻居数目加一
                                        if (val == 0)
                                            counter++;
                                    }
                                }
                            }

                        if (counter < neighborhoodMinCount)
                            image.SetPixel(x, y, WHITE);
                    }
                }

            return this;
        }

        private bool isBlackBlock(int startX, int startY, int blockWidth, int blockHeight) {
            int counter = 0;//统计黑色像素点的个数
            int total = 0;//统计有效像素点的个数
            int val;
            for (int x1 = startX; x1 <= startX + blockWidth - 1; x1++)
                for (int y1 = startY; y1 <= startY + blockHeight - 1; y1++) {
                    //判断这个点是否存在
                    if (x1 < width && x1 >= 0 && y1 < height && y1 >= 0) {
                        total++;//有效像素点的个数
                        //这里假定图像已经被黑白化,取Red值认为不是0就是255 
                        val = this.image.GetPixel(x1, y1).R;
                        //如果这个点是黑色,就把黑色像素点的数目加一
                        if (val == 0)
                            counter++;
                    }
                }

            return counter == total && total != 0;
        }

        public int getWhitePercent() {
            int white = 0;
            for (int i = 0; i < this.height; i++)
                for (int j = 0; j < this.width; j++) {
                    Color point = this.image.GetPixel(j, i);
                    if (((point.R + point.G + point.B) / 3) == 255)
                        white++;
                }
            return (int)Math.Ceiling(((float)white * 100 / (width * height)));
        }

        private int getAvgValue() {
            Color point;
            int total = 0;
            for (int i = 0; i < this.height; i++)
                for (int j = 0; j < this.width; j++) {

                    point = image.GetPixel(j, i);
                    total += (point.R + point.G + point.B) / 3;
                }
            return total / (this.width * this.height);
        }
    }
}
