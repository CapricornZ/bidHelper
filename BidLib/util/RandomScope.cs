using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.util {
    public class RandomScope {

        private rest.V3Common.Submit[] submits;
        private int delta;

        public RandomScope(rest.V3Common.Submit[] submits) {
            this.submits = submits;
        }

        public RandomScope(rest.V3Common v3, int delta) {

            this.delta = delta;
            bool bFound = false;
            int i=0;
            for (i = 0; !bFound && i < v3.triggers.Length; i++) {
                if (v3.triggers[i].delta == delta)
                    bFound = true;
            }
            if (bFound)
                this.submits = v3.triggers[i - 1].submits;
            else
                this.submits = v3.triggers[0].submits;
        }

        private rest.V3Common.Submit random() {

            long tick = DateTime.Now.Ticks;
            Random random = new Random((int)(tick & 0xffffffff)|(int)(tick >> 32));
            int rand = random.Next(1, 100);
            bool bStop = false;
            int i = 0;
            for (i = 0; !bStop && i < this.submits.Length; i++) {
                bStop = this.submits[i].percent >= rand;
            }
            return this.submits[i-1];
        }

        public int getDelta() { return this.delta; }
        public String randomTime() {
            rest.V3Common v3 = tobid.rest.V3Common.commonConf;
            rest.V3Common.Trigger trigger = v3.triggers[0];
            RandomScope random = new RandomScope(trigger.submits);
            rest.V3Common.Submit submit = random.random();
            return submit.submitTime;
        }
    }
}
