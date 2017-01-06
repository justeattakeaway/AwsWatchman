namespace Watchman.Configuration.Load
{
    public class FileSettings
    {
        public FileSettings(string folderLocation)
        {
            FolderLocation = folderLocation;
        }

        public string FolderLocation { get; private set; }
    }
}
