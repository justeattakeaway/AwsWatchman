using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    static class AlertingGroupParameterExtensions
    {
        public static string DefaultAlarmDescription(this AlertingGroupParameters groupParameters, IResource resource)
        {
            var suffix = string.IsNullOrWhiteSpace(groupParameters.Description)
                ? null
                : $" ({groupParameters.Description})";

            var description = $"{AwsConstants.V2DefaultDescription}. Alerting group: {groupParameters.Name}{suffix}";

            if (!string.IsNullOrWhiteSpace(resource.Description))
            {
                description = $"{resource.Description}{Environment.NewLine}{Environment.NewLine}{description}";
            }
            
            return description;
        }
    }
}
