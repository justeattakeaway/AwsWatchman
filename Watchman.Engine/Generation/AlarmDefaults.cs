using System.Collections.Generic;

namespace Watchman.Engine.Generation
{
    public class AlarmDefaults<TServiceType> : List<AlarmDefinition>
    {

        public static AlarmDefaults<TServiceType> FromDefaults(IEnumerable<AlarmDefinition> defaults)
        {
            var result = new AlarmDefaults<TServiceType>();
            result.AddRange(defaults);
            return result;
        }
    }
}