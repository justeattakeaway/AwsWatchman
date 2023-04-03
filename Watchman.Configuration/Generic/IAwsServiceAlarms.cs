namespace Watchman.Configuration.Generic
{
    public interface IAwsServiceAlarms
    {
        List<IResource> Resources { get; }

        List<string> ExcludeResourcesPrefixedWith { get; }

        Dictionary<string, AlarmValues> Values { get; }
    }
}
