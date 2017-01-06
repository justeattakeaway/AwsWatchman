# Configuration file examples

```json
{
  "Name": "Team1",
  "AlarmNameSuffix": "Team1",
  "Targets": [
    { "Url": "https://events.pagerduty.com/integration/123456789asdfaxbkj/enqueue" }
  ],
  "ReportTargets" : [
    { "Email": "test@example.com" }
  ],
    "DynamoDb": {
      "Tables": [
        { "Name": "table1" },
        { "Name": "table2" }
      ]
    },
  "Sqs": {
    "Queues": [
      { "Name": "queue1" },
      { "Name": "queue2" }
   ]
  }
}
```

```json
{
  "Name": "COG2",
  "AlarmNameSuffix": "cog2",
  "Threshold": 0.50,
  "Targets": [
    { "Url": "https://events.pagerduty.com/integration/123456789asdfaxbkj/enqueue" },
    { "Email": "cog2@justeat.pagerduty.com" }
  ],
  "DynamoDb": {  
    "Tables": [
      { "Name": "table3"},
      { "Name": "table5", "Threshold": 0.75 },
      { "Pattern": "^je-some-prefix-", "MonitorWrites" : false,  "Threshold": 0.50 }
    ]
  }
}
```

```json
{
  "Name": "SysOps",
  "AlarmNameSuffix": "SysOps",
  "Targets": [
    { "Email": "SysOps@justeat.pagerduty.com" }
  ],
  "IsCatchAll": true,
  "DynamoDb": {   
    "Threshold": 0.75,
    "Tables": [
      { "Pattern": ".*" }
    ],
    "ExcludeTablesPrefixedWith": [
      "je-search"
    ]
  }
}
```

```json
{
  "Name": "SqsOnlyDemo",
  "Targets": [
    { "Email": "SqsOnlyTest@example.com" },
    { "Url": "http://farley.com" }
  ],
  "AlarmNameSuffix": "SqsOnlyTest",
  "Sqs": {
    "Queues": [
      {
        "Pattern": "anyqueue",
        "LengthThreshold": 100,
        "OldestMessageThreshold": 600,
        "Errors": {
          "LengthThreshold": 10,
          "Monitored": true
        }
      }
    ]
  }
}

```

### Regex

Patterns are regular expressions, so we can use the RegEx syntax [to exclude](http://fineonly.com/solutions/regex-exclude-a-string) certain resources from one general rule and catch them with a second specific rule, in order to e.g. apply a different "OldestMessageThreshold" to [a delay queue](http://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-delay-queues.html).

```json
{
    "Name": "QueueFilters",
    "AlarmNameSuffix": "qf",
    "Targets": [
		{ "Url": "http://farley.com" }
    ],
    "Sqs": {
        "LengthThreshold": 5,
        "Errors": {
            "LengthThreshold": 1
        },
        "Queues": [
            {
                "Pattern": "-myapp-(?!(delayqueue|ignoredqueue))",
                "OldestMessageThreshold": 60
            },
            {
                "Pattern": "-myapp-delayqueue-",
                "OldestMessageThreshold": 360
            }
        ]
    }
}
```