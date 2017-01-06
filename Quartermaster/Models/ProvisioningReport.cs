using System.Collections.Generic;
using Watchman.Configuration;

namespace QuarterMaster.Models
{
    public class ProvisioningReport
    {
        public string Name { get; set; }
        public IList<ReportTarget> Targets { get; set; } = new List<ReportTarget>();

        public List<ProvisioningReportRow> Rows { get; set; } = new List<ProvisioningReportRow>();
    }
}
