using System.Collections.Generic;

namespace Watchman.Configuration
{
    public class DynamoDb
    {
        public DynamoDb()
        {
            Tables = new List<Table>();
            ExcludeTablesPrefixedWith = new List<string>();
            ExcludeReadsForTablesPrefixedWith = new List<string>();
            ExcludeWritesForTablesPrefixedWith = new List<string>();
        }

        public double? Threshold { get; set; }

        public bool? MonitorCapacity { get; set; }

        public int? ThrottlingThreshold { get; set; }

        public bool? MonitorThrottling { get; set; }

        public List<Table> Tables { get; set; }

        public List<string> ExcludeTablesPrefixedWith { get; set; }
        public List<string> ExcludeReadsForTablesPrefixedWith { get; set; }
        public List<string> ExcludeWritesForTablesPrefixedWith { get; set; }
    }
}
