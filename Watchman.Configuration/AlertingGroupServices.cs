using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration
{
    public class AlertingGroupServices
    {
        public AwsServiceAlarms Rds { get; set;  }
        public AwsServiceAlarms AutoScaling { get; set; }
        public AwsServiceAlarms Lambda { get; set; }
        public AwsServiceAlarms VpcSubnet { get; set; }
        public AwsServiceAlarms Elb { get; set; }
        public AwsServiceAlarms KinesisStream { get; set; }
        public AwsServiceAlarms StepFunction { get; set; }
        public AwsServiceAlarms DynamoDb { get; set; }

        public IList<AwsServiceAlarms> AllServices => new[]
                { Rds, AutoScaling, Lambda, VpcSubnet, Elb, KinesisStream, StepFunction, DynamoDb }
            .Where(s => s != null)
            .ToArray();

        public Dictionary<string, AwsServiceAlarms> AllServicesByName => new Dictionary<string, AwsServiceAlarms>()
        {
            {"Rds", Rds},
            {"AutoScaling", AutoScaling},
            {"Lambda", Lambda},
            {"VpcSubnet", VpcSubnet},
            {"Elb", Elb},
            {"KinesisStream", KinesisStream},
            {"StepFunction", StepFunction},
            {"DynamoDb", DynamoDb}
        };
    }
}
