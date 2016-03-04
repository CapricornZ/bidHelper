using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.util.http.ws.cmd {
    public class SetTriggerCmd : Command {

        public SetTriggerCmd() {
            this.category = "SETTRIGGER";
            this.time = DateTime.Now;
        }

        public String trigger { get; set; }
    }
}
