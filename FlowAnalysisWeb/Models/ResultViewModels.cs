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
}