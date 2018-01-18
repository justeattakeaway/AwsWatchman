using System.Collections.Generic;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources
{
    public interface IAlarmDimensionProvider<in TAwsResourceType, in TAlarmConfig> where TAlarmConfig: class
    {
        List<Dimension> GetDimensions(TAwsResourceType resource, TAlarmConfig config, IList<string> dimensionNames);
    }
}
