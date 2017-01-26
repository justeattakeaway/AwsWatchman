using System.IO;
using System.Reflection;

namespace Watchman.Configuration.Tests.Load
{
    public static class TestFiles
    {
        public static string GetRelativePathTo(string folder)
        {
            var assemblyFilePath = Assembly.GetExecutingAssembly().Location;
            var basePath = Path.GetDirectoryName(assemblyFilePath);
            return Path.Combine(basePath, folder);
        }
    }
}
