﻿namespace Watchman.Configuration.Generic
{
    public interface IResource
    {
        string Name { get; }
        string Pattern { get; }
        Dictionary<string, AlarmValues> Values { get; set; }
        string Description { get; set; }
    }
}
