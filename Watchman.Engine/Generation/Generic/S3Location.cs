namespace Watchman.Engine.Generation.Generic
{
    public class S3Location
    {
        public string BucketName { get; }
        public string Path { get; }

        public S3Location(string bucketName, string path)
        {
            BucketName = bucketName;
            Path = path;
        }
    }
}
