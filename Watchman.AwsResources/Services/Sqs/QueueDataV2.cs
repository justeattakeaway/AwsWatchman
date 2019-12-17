namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueDataV2
    {
        public QueueDataV2(string name)
        {
            Name = name;
            WorkingQueue = null;
            ErrorQueue = null;
        }

        public string Name { get; }

        public QueueDataV2 WorkingQueue { get; private set; }

        public QueueDataV2 ErrorQueue { get; private set; }

        public void SetWorkingQueue(string name)
        {
            WorkingQueue = new QueueDataV2(name);
        }

        public void SetErrorQueue(string name)
        {
            ErrorQueue = new QueueDataV2(name);
        }
    }
}
