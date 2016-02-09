using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tobid.scheduler.jobs.action {
    
    public interface IAction {
        Boolean execute();
    }

    public interface IBidAction : IAction {
        void notify(String message);
    }

    public class IfThenAction : IBidAction {
        private IBidAction first, second;

        public IfThenAction(IBidAction first, IBidAction second) {
            this.first = first;
            this.second = second;
        }

        public void notify(string message) {

            first.notify(message);
            //second.notify(message);
        }

        public bool execute() {

            bool rtn = this.first.execute();
            if (rtn)
                rtn = this.second.execute();
            return true;
        }
    }

    /// <summary>
    /// 顺序执行
    /// </summary>
    public class SequenceAction : IBidAction {

        private List<IBidAction> tasks;

        public SequenceAction(List<IBidAction> tasks) {
            
            this.tasks = tasks;
        }

        public void notify(string message) {

            this.tasks[0].notify(message);
            //foreach(IBidAction action in this.tasks)
                //action.notify(message);
        }

        public bool execute() {

            bool rtn = false;
            foreach (IBidAction action in this.tasks)
                rtn = action.execute();
            return rtn;
        }
    }
}
