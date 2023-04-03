# Legacy configuration

## DynamoDb

| Field | Type | Value | Description |
|-------|------|-----------|------------|
| Threshold | Decimal | Default is `0.8` (ie 80%) | The used as a percentage of the capacity to work out the alarm thresholds. Will be used as threshold of all tables listed in an AlertingGroup unless overridden for a table. |
| MonitorThrottling | bool | Default is false | Enables monitoring on throttled reads or writes. |
| MonitorCapacity | bool | Default is true | Enables monitoring of usage against provisioned capacity. |
| ThrottlingThreshold| int| Optional | Set the throttling threshold - the number of throttled reads or writes in a minute that causes an alarm. |
| Tables | Array of `Table` | ** | Array of dynamo tables to add the alerts to. See below for more info on `Tables` |
| ExcludeTablesPrefixedWith | Array of strings | | Don't add read or write alerts for any tables with these name prefixes. Exclude overrides `Tables` settings. |
| ExcludeReadsForTablesPrefixedWith | Array of strings |  | Don't add read alerts for any tables with these name prefixes. Exclude overrides `Tables` settings. |
| ExcludeWritesForTablesPrefixedWith | Array of strings | | Don't add write alerts for any tables with these name prefixes. Exclude overrides `Tables` settings. |

## Tables

Tables can either be added as strings or Table objects. If added as simple strings, the default options will used.

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| Name | String | One of `Name` or `Pattern` must be specified | Name of the dynamo table |
| Pattern | String | One of `Name` or `Pattern` must be specified | Regex to match multiple dynamo tables |
| Threshold | Decimal | Default from the `Threshold` value of containing alerting group. | Used as a fraction of the capacity to work out the alarm threshold for this table *** |
| MonitorWrites | Boolean | Default is `true` | If `false`, no alerts are generated for writes to the table or its indexes |
| MonitorThrottling | bool | Default from the `MonitorThrottling` value of containing alerting group. | Enables or disables monitoring on throttled reads or writes. |
| MonitorCapacity | bool | Default from the `MonitorCapacity` value of containing alerting group. | Enables monitoring of usage against provisioned capacity. |
| ThrottlingThreshold| int| Default from the `ThrottlingThreshold` value of containing alerting group. | Set the throttling threshold - the number of throttled reads or writes in a minute that causes an alarm. |

The alerting Threshold is a value from 0.0 to 1.0 that specifies the fraction of the table's read or write capacity being used that triggers the cloudwatch alarm. The threshold for a table uses these fallbacks:

- if a threshold is specified for the table, that is used. If not,
- if a threshold is specified for the containing alerting group, that is used. If not,
- a global default of 0.8 (i.e. 80% utilisation) is used.

All global secondary indexes on the table will also be monitored at the same threshold.

### Sqs configuration sections

### Sqs

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| LengthThreshold | int, count of messages | Optional | Value for alert on queue length |
| OldestMessageThreshold | int, number of seconds | Optional | Value for alert on old messages |
| Errors | error queue data | Optional | Default configuration of error queues |
| Queues | Array of queue |  |  |

### Queue

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| Name | String | One of `Name` or `Pattern` must be specified | Name of the queue |
| Pattern | String | One of `Name` or `Pattern` must be specified | Regex to match multiple queues |
| LengthThreshold | int, count of messages | Optional | Value for alert on queue length |
| OldestMessageThreshold | int, number of seconds | Optional | Value for alert on age of messages in a queue |
| Errors | error queue data | Optional | Values for alerts on error queues |

### Errors

Can be attached to Queue or alerting group or both. Values are defaulted - if a value is not specified in a queue, the default from the alerting group is used. If that is not specified, then a global default is used.

| Field | Type | Value | Description |
|-------|------|-------|-------------|
| Monitored | bool | Optional, default _true_ | Should this error queue be monitored at all. If False, none of the other values in the Errors object are used  |
| Suffix | string | Optional, default is "_error" | Text at the end of queue name to determine if this is an error queue |
| LengthThreshold | int, count of messages | Optional | Value for alert on error queue length |
| OldestMessageThreshold  | int, number of seconds | Optional | Value for alert on age of messages in an error queue |

This looks complex, but the defaults are overrides are aimed at producing readable and sensible markup, while dealing with the way that queues are set up in a variety of ways: in some cases the team needs to know about even 1 message arriving on an error queue, while in others the error queue is ignored, or does not exist.
