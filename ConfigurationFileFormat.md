# Configuration file format

Configuration is read from a folder. Typically we use one folder per environment, containing one file per team.
Each file contains an "alerting group".

 The alerting group has some top-level data such as `Name`, some data about `Targets` that alerts and reports are sent to, and then markup about AWS resources: the `Services` element. This specifies which resources the team claims, and configuration on how to monitor them. Configuration files are validated when loaded, and each file must contain at least one of these AWS resource sections.

## AlertingGroup


| Field | Type | Value | Description |
|-------|------|-----------|------------|
| Name | String | Required | AlertGroup name (usually the team's Name) |
| AlarmNameSuffix | String | Required | Used when generating the dynamo alarms, so can distinguish multiple alarms on the same table (usually the COG's name) |
| Targets | Array of `Target` | Required | Must contain at least one target.<br> See below more info on `Targets`. |
| ReportTargets | Array of `ReportTarget` | | List of email addresses to mail the provisioning report to, for use with Quartermaster |
| IsCatchAll | Boolean | Default is `false` |If `true`, this group is not inspected when generating the lists of unmonitored resources, and it can include resources monitored by this group.|
|Services| services| Required | Describes the services to monitor. |

__NOTES:__

** Each AlertingGroup must specify services to monitor in some form.

#### Targets

Targets receive the alerts - they are added to the SNS subscriptions for each cloudwatch alarm when setting these up. Each target in the array should provide only one key, i.e. a url or an email, not both.

| Key | Description |
|-----|-------------|
| Url | A url to send alerts to. |
| Email | An email to send alerts to. |

To get a Url for PagerDuty integration, use the "In pagerDuty" section of the [PagerDuty / AWS CloudWatch Integration Guide](https://www.pagerduty.com/docs/guides/aws-cloudwatch-integration-guide/).

### Services sections

See [Supported Services](SupportedServices.md) 