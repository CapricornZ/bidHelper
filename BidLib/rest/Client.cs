using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.rest
{
    /// <summary>
    /// 客户机信息
    /// </summary>
    public class Client {

        public String ip { get; set; }
        public DateTime updateTime { get; set; }
        public Config config { get; set; }
        public Operation[] operation { get; set; }
    }

    /// <summary>
    /// 配置信息
    /// </summary>
    public class Config {

        public String no { get; set; }
        public String passwd { get; set; }
        public String pid { get; set; }
        public String pname { get; set; }
        public DateTime startTime { get; set; }
        public DateTime expireTime { get; set; }
        public DateTime updateTime { get; set; }
    }

    /// <summary>
    /// 操作类
    /// </summary>
    public abstract class Operation {

        public int id { get; set; }
        public String type { get; set; }
        public String content { get; set; }
        public DateTime startTime { get; set; }
        public DateTime expireTime { get; set; }
        public DateTime updateTime { get; set; }
    }

    /// <summary>
    /// 登录
    /// </summary>
    public class LoginOperation : Operation {
        public String url { get; set; }
    }

    /// <summary>
    /// 第一阶段出价
    /// </summary>
    public class Step1Operation : Operation {
        /// <summary>
        /// 第一阶段实际价格
        /// </summary>
        public int price { get; set; }
    }

    /// <summary>
    /// 第二阶段出价
    /// </summary>
    public class Step2Operation : Operation {
        /// <summary>
        /// 原价基础上Delta价格
        /// </summary>
        public int price { get; set; }
    }
}