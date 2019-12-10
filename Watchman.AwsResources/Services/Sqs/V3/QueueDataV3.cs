namespace Watchman.AwsResources.Services.Sqs.V3
{
    public class QueueDataV3
    {
        public QueueDataV3(string name)
        {
            Name = name;
            WorkingQueue = null;
            ErrorQueue = null;
        }

        public string Name { get; }

        public QueueDataV3 WorkingQueue { get; private set; }

        public QueueDataV3 ErrorQueue { get; private set; }

        public void SetWorkingQueue(string name)
        {
            WorkingQueue = new QueueDataV3(name);
        }

        public void SetErrorQueue(string name)
        {
            ErrorQueue = new QueueDataV3(name);
        }
    }
}
