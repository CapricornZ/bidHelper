using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using System.IO;
using System.Windows.Forms;
using mshtml;

namespace tobid.util
{
    class DomUtil
    {
        /// <summary>
        /// 返回指定WebBrowser中图片<IMG></IMG>中的图内容
        /// </summary>
        /// <param name="WebCtl">WebBrowser控件</param>
        /// <param name="ImgeTag">IMG元素</param>
        /// <returns>IMG对象</returns>
        public Image GetWebImage(WebBrowser WebCtl, HtmlElement ImgeTag)
        {
            HTMLDocument doc = (HTMLDocument)WebCtl.Document.DomDocument;
            HTMLBody body = (HTMLBody)doc.body;
            IHTMLControlRange rang = (IHTMLControlRange)body.createControlRange();
            IHTMLControlElement Img = (IHTMLControlElement)ImgeTag.DomElement; //图片地址

            Image oldImage = Clipboard.GetImage();
            rang.add(Img);
            rang.execCommand("Copy", false, null);  //拷贝到内存
            Image numImage = Clipboard.GetImage();
            try
            {
                Clipboard.SetImage(oldImage);
            }
            catch
            {
            }

            return numImage;
        }

        public Image GetWebImage(HTMLBody body, HtmlElement ImageTag)
        {
            IHTMLControlRange rang = (IHTMLControlRange)body.createControlRange();
            IHTMLControlElement Img = (IHTMLControlElement)ImageTag.DomElement; //图片地址

            Image oldImage = Clipboard.GetImage();
            rang.add(Img);
            rang.execCommand("Copy", false, null);  //拷贝到内存
            Image numImage = Clipboard.GetImage();
            try
            {
                Clipboard.SetImage(oldImage);
            }
            catch
            {
            }

            return numImage;
        }

        public static byte[] transferImage2Byte(Image image)
        {
            MemoryStream ms = new MemoryStream();
            image.Save(ms, ImageFormat.Jpeg);
            return ms.GetBuffer();
        }
    }
}
