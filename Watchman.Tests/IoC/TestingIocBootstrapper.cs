using NSubstitute;
using StructureMap;
using Watchman.IoC;

namespace Watchman.Tests.IoC
{
    class TestingIocBootstrapper
    {
        public StartupParameters StartupParameters { get; }
        private readonly Lazy<IContainer> _container;
        public IContainer Container => _container.Value;
        private readonly Registry _registry = new Registry();


        public TestingIocBootstrapper(StartupParameters startupParameters = null)
        {
            StartupParameters = startupParameters ?? new StartupParameters()
            {
                ConfigFolderLocation = @"c:\test-path"
            };

            _container = new Lazy<IContainer>(ConfigureContainer);

            _registry.IncludeRegistry(new ApplicationRegistry(StartupParameters));
            _registry.IncludeRegistry<AwsServiceRegistry>();
            _registry.IncludeRegistry<FakeBoundaryRegistry>();
        }

        private IContainer ConfigureContainer()
        {
            return new Container(_registry);
        }

        public T GetMock<T>() where T : class
        {
            var instance = Get<T>();
            return instance;
        }

        public T Get<T>() => Container.GetInstance<T>();

        public void Override<T>(T instance) where T : class
        {
            try
            {
                GetMock<T>();
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Cannot override non-boundary interface {typeof(T).Name} in end-to-end tests", nameof(instance));
            }

            Container.Configure(x => x.For<T>().Use(instance));
        }
    }
}
