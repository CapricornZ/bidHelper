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
    public class GivePrice
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

    /// <summary>
    /// 竞价坐标
    /// </summary>
    public class Bid
    {
        public GivePrice give { get; set; }
        public SubmitPrice submit { get; set; }
    }
}
