using StructureMap;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine.Logging;

namespace Watchman.IoC
{   
    public class IocBootstrapper
    {
        public IContainer ConfigureContainer(StartupParameters parameters)
        {
            var registry = new Registry();

            registry.IncludeRegistry(new ApplicationRegistry(parameters));
            registry.IncludeRegistry<AwsServiceRegistry>();
            registry.IncludeRegistry(new BoundaryRegistry(parameters));
            return new Container(registry);
        }    
    }
}
