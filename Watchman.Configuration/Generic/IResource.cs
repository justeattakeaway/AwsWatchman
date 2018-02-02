using System.Collections.Generic;

namespace Watchman.Configuration.Generic
{
    public interface IResource
    {
        string Name { get; }
        string Pattern { get; }
        Dictionary<string, AlarmValues> Values { get; set; }
    }
}
