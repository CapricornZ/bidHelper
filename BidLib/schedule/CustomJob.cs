using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tobid.scheduler.jobs.action;
using System.Threading;

namespace tobid.scheduler.jobs {

    public class CustomJob : ISchedulerJob{

        private static log4net.ILog logger = log4net.LogManager.GetLogger(typeof(CustomJob));
        private static Object lockObj = new Object();

        private List<ITask> tasks;
        private int nextTask = -1;

        public CustomJob(List<ITask> tasks) {
            if (null != tasks && tasks.Count > 0) {
                this.tasks = tasks;
                this.nextTask = 0;
            } else
                this.nextTask = -1;
        }

        public void Execute() {

            if (Monitor.TryEnter(CustomJob.lockObj, 500)) {

                if (this.nextTask < this.tasks.Count) {

                    if (this.tasks[this.nextTask].execute()) {
                        
                        Console.WriteLine( this.tasks[this.nextTask] + " Execute OK");
                        this.nextTask++;
                    }

                }
                Monitor.Exit(CustomJob.lockObj);
            } else
                logger.Warn("obtain CustomJob.lockObj timeout on Execute(...)");
            
        }
    }
}
