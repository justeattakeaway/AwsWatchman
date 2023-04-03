namespace QuarterMaster.Models
{
    public class ProvisioningReportRow
    {
        public string TableName { get; set; }
        public string IndexName { get; set; } = string.Empty;
        public long ProvisionedReadCapacityUnits { get; set; }
        public long ProvisionedWriteCapacityUnits { get; set; }
        public long ProvisionedReadPerMinute => ProvisionedReadCapacityUnits*60;
        public long ProvisionedWritePerMinute => ProvisionedWriteCapacityUnits*60;
        public double MaxConsumedReadPerMinute { get; set; }
        public double MaxConsumedWritePerMinute { get; set; }
        public double ReadUsePercentage => Math.Round((MaxConsumedReadPerMinute/ProvisionedReadPerMinute) * 100);
        public double WriteUsePercentage => Math.Round((MaxConsumedWritePerMinute / ProvisionedWritePerMinute) * 100);
    }
}
