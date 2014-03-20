using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTypes
{
    public class Report
    {
        public enum ReportTypes
        {
            RequirementSpecification,
            TestPlan            
        };

        public ReportTypes ReportType { get; set; }
    }
}
