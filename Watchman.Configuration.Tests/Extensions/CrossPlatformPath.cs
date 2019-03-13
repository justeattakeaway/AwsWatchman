using System.IO;

namespace Watchman.Configuration.Tests.Extensions
{
    public static class StringExtension
    {
      public static string ToCrossPlatformPath(this string path)
      {
        return path.Replace("\\", Path.DirectorySeparatorChar.ToString());
      }
    }
}
