using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace tobid.util.orc {

    struct ColorRGB {

        public byte R;
        public byte G;
        public byte B;

        public ColorRGB(Color value) {

            this.R = value.R;
            this.G = value.G;
            this.B = value.B;
        }

        public static void RGB2HSL(ColorRGB rgb, out double h, out double s, out double l) {

            double r = rgb.R / 255.0;
            double g = rgb.G / 255.0;
            double b = rgb.B / 255.0;
            double v;
            double m;
            double vm;
            double r2, g2, b2;

            h = 0; // default to black
            s = 0;
            l = 0;
            v = Math.Max(r, g);
            v = Math.Max(v, b);
            m = Math.Min(r, g);
            m = Math.Min(m, b);
            l = (m + v) / 2.0;
            if (l <= 0.0) {
                return;
            }
            vm = v - m;
            s = vm;
            if (s > 0.0) {
                s /= (l <= 0.5) ? (v + m) : (2.0 - v - m);
            } else {
                return;
            }
            r2 = (v - r) / vm;
            g2 = (v - g) / vm;
            b2 = (v - b) / vm;

            if (r == v) {
                h = (g == m ? 5.0 + b2 : 1.0 - g2);
            } else if (g == v) {
                h = (b == m ? 1.0 + r2 : 3.0 - b2);
            } else {
                h = (r == m ? 3.0 + g2 : 5.0 - r2);
            }

            h /= 6.0;
        }
    }

    public class CaptchaHelper {

        /// <summary>
        /// 是否为蓝色“刷新校验码”
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Boolean isRefresh(Bitmap bitmap) {

            double h, s, l;
            int active = 0;
            int percent = 0;
            int width = 111, height = 26;
            for (int x = 8; x < width; x++)
                for (int y = 0; y < height; y++) {

                    Color point = bitmap.GetPixel(x, y);
                    ColorRGB.RGB2HSL(new ColorRGB(point), out h, out s, out l);
                    //System.Console.WriteLine(String.Format("({0},{1}) HUE:{2:f},Sat:{3:f},Bright:{4:f}", x, y, point.GetHue(), point.GetSaturation(), point.GetBrightness()));
                    h *= 240;
                    s *= 241;
                    l *= 241;
                    //System.Console.WriteLine(String.Format("({0},{1}) Hue:{2:f},Sat:{3:f},L:{4:f}", x, y, h, s, l));
                    if (h > 136 && h < 143 && s > 140 && s < 235) {
                        active++;
                    }
                }
            percent = active * 100 / width / height;
            System.Console.WriteLine(String.Format("ACTIVE {0:f}%", percent));
            return percent > 60;
        }

        /// <summary>
        /// 是否为“正在获取校验码”
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Boolean isLoading(Bitmap bitmap) {

            int count = 0;
            for (int nX = 0; nX < bitmap.Width; nX++)
                for (int nY = 0; nY < bitmap.Height; nY++) {
                    Color color = bitmap.GetPixel(nX, nY);
                    if (color.R == color.G && color.G == color.B && color.B < 200)
                        count++;
                }

            System.Console.WriteLine("COUNT:" + count);
            return count > 500;
        }
    }
}
