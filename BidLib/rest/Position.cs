using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.rest.position
{
    public class Position
    {
        public Position(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public int x { get; set; }
        public int y { get; set; }
    }

    /// <summary>
    /// 出价的坐标
    /// </summary>
    public class GivePriceStep1 {

        public Position[] inputBox { get; set; }
        public Position button { get; set; }
    }

    /// <summary>
    /// 出价的坐标
    /// </summary>
    public class GivePriceStep2
    {
        public Position price { get; set; }
        public Position inputBox { get; set; }
        public Position button { get; set; }
    }

    /// <summary>
    /// 出价验证码坐标
    /// </summary>
    public class SubmitPrice
    {
        public Position[] captcha { get; set; }
        public Position inputBox { get; set; }
        public Position[] buttons { get; set; }
    }

    public class Login {
        public GivePriceStep1 give { get; set; }
        public SubmitPrice submit { get; set; }
    }

    /// <summary>
    /// 竞价坐标
    /// </summary>
    public class BidStep1 {

        public GivePriceStep1 give { get; set; }
        public SubmitPrice submit { get; set; }
    }

    /// <summary>
    /// 竞价坐标
    /// </summary>
    public class BidStep2
    {
        public GivePriceStep2 give { get; set; }
        public SubmitPrice submit { get; set; }
        public Position Origin { get; set; }
    }
}
