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

    /// <summary>
    /// 顺序执行
    /// </summary>
    public class SequenceAction : IBidAction {

        private List<IBidAction> tasks;

        public SequenceAction(List<IBidAction> tasks) {
            
            this.tasks = tasks;
        }

        public void notify(string message) {

            foreach(IBidAction action in this.tasks)
                action.notify(message);
        }

        public bool execute() {

            bool rtn = false;
            foreach (IBidAction action in this.tasks)
                rtn = action.execute();
            return rtn;
        }
    }
}
