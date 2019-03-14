namespace Watchman.Configuration.Generic
{
    public interface IServiceAlarmConfig<TConfig>
    {
        TConfig Merge(TConfig parentConfig);
    }
}
