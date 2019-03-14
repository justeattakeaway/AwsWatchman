namespace Watchman.Engine.Generation
{
    static class AlertingGroupParameterExtensions
    {
        public static string DefaultAlarmDescription(this AlertingGroupParameters groupParameters)
        {
            var suffix = string.IsNullOrWhiteSpace(groupParameters.Description)
                ? null
                : $" ({groupParameters.Description})";

            var description = $"{AwsConstants.DefaultDescription}. Alerting group: {groupParameters.Name}{suffix}";

            return description;
        }
    }
}
