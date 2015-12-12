using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaptchaExam {

    public class ExamRecord {
        public String level { get; set; }
        public int total { get; set; }
        public int correct { get; set; }
        public float avgCost { get; set; }
    }
}
