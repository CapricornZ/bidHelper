using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.util.http.ws.cmd {

    #region 远程设置命令
    /// <summary>
    /// 加载配置
    /// </summary>
    public class ReloadCmd : Command {

        public ReloadCmd() {
            this.category = "RELOAD";
            this.time = DateTime.Now;
        }
    }

    public class TriggerF11Cmd : Command {

        public TriggerF11Cmd() {
            this.category = "TRIGGERF11";
            this.time = DateTime.Now;
        }
    }

    public class UpdatePolicyCmd : Command {

        public UpdatePolicyCmd() {
            this.category = "UPDATEPOLICY";
            this.time = DateTime.Now;
        }
    }

    /// <summary>
    /// 设置定时器参数
    /// </summary>
    public class SetTimerCmd : Command {

        public SetTimerCmd() {
            this.category = "SETTIMER";
            this.time = DateTime.Now;
        }

        public List<String> param { get; set; }
    }
    #endregion
}
