using System.Collections.Generic;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources
{
    public interface IAlarmDimensionProvider<in T>
    {
        List<Dimension> GetDimensions(T resource, IList<string> dimensionNames);
    }
}
