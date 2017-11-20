using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FlowAnalysisWeb.Models
{
    [NotMapped]
    public class ResultViewModels: AnalysisResults
    {
        public new string Result { get; set; }
    }

    public class StatisticsViewModels
    {
        public int Users { get; set; }
        public int White { get; set; }
        public int Lock { get; set; }
    }
}