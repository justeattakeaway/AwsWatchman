using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.AwsResources
{
    public interface ICurrentTimeProvider
    {
        DateTime Now { get; }
    }
}
