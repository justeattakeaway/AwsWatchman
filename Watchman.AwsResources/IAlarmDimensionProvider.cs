using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources
{
    public interface IAlarmDimensionProvider<in TAwsResourceType>
    {
        List<Dimension> GetDimensions(TAwsResourceType resource, IList<string> dimensionNames);
    }
}
