# AWSWatchman

> Because unmonitored infrastructure will bite you

[![Build status](https://github.com/justeat/AwsWatchman/workflows/build/badge.svg?branch=main&event=push)](https://github.com/justeat/AwsWatchman/actions?query=workflow%3Abuild+branch%3Amain+event%3Apush)

## What

AWSWatchman creates and maintains AWS CloudWatch alerts.

Dynamic monitoring. This program creates and maintains CloudWatch alerts for infrastructure in AWS. It covers DynamoDB tables, SQS queues and more.

The details of who to alert and what tables to alert on must be stored in configuration files.

> :warning: Be careful. This code, when used correctly, will modify your AWS account by adding CloudWatch alerts to multiple resources. By default it will do a dry run which will tell you what alarms would be added. You must add `--RunMode GenerateAlarms` to enable writes.

## Why

Unmonitored infrastructure is a problem waiting to happen, so all infrastructure in AWS should have appropriate alerts via CloudWatch. It is usually possible to declare all these alarms with the resource upfront in CloudFormation. But this is not always the best way to do it. AWSWatchman is good for cases where the CloudFormation definitions are harder to use. For example:

- Dynamically scaled resources such as DynamoDB tables where the table can be scaled up
- Dynamically created resources such as SQS queues, where the queue can be subscribed to a topic at runtime by code.
- Verifying existing resources by scanning all resources in the system and programmatically identifying ones that do not have alarms, rather than inspecting all the CloudFormation text that created them.

## Run

`Watchman` is written in C# and targets .NEt 6. [You can download the `dotnet` runtime for Windows, Mac or Linux here](https://dotnet.microsoft.com/download)

Run the `Watchman` specifying a config folder for config files (see [Configuration file format](ConfigurationFileFormat.md)), and optionally AWS credentials.

### Examples

#### With AWS credentials on the command line

```ps1
dotnet .\Watchman.dll --RunMode GenerateAlarms --ConfigFolder ".\configuration"  --AwsAccessKey AKABC123 --AwsSecretKey abcd1234
```

#### With AWS credentials from a profile

```ps1
dotnet .\Watchman.dll --RunMode GenerateAlarms --ConfigFolder ".\configuration"  --AwsProfile prod
```

#### With default AWS credentials

```ps1
dotnet .\Watchman.dll --RunMode GenerateAlarms --ConfigFolder ".\configuration"
```

If you are using the new resource types and have a high number of alarms you will need to specify and S3 bucket/path that the CloudFormation template can be deployed to. The AWS credentials will need permissions to put objects into that location.

```ps1
dotnet .\Watchman.dll --RunMode GenerateAlarms --ConfigFolder ".\configuration" --TemplateS3Path "s3://je-deployments-qa21/watchman"
```

## Command-line Parameters

The possible command-line parameters are:

- `RunMode`: One of `TestConfig`, `DryRun`, or `GenerateAlarms`. Optional, default is `DryRun`. Mode behaviours are:
  - `TestConfig`: Configuration files are loaded and validated. Used to test syntax of changes to configuration. AWS credentials are not needed.
  - `DryRun`: All actions short of writing alarms are performed. Used to test what the effects of the configuration will be on the AWS account.
  - `GenerateAlarms`: A full run of the program. You must specify `GenerateAlarms` in order to actually write alarms.
- `AwsAccessKey` and `AwsSecretKey`. Optional. Supply both of these parameters in order to specify AWS credentials on the command line.
- `AwsProfile` Specify a named AWS profile to use for credentials. Optional.
- `AwsRegion` The AWS region to use. Optional, default is `eu-west-1`.
- `ConfigFolder`: The ppath to the configuration files. Required.
- `Verbose`: One of `true` or `false`. Give more detailed output. Optional, default is `false`.
- `WriteCloudFormationTemplatesToDirectory`. If set, alarms deployed via CloudFormation will be written to this folder instead of deployed. Note that this does not affect SQS and DynamoDB alarms which currently use a different deployment method.
- `AwsLogging`. Enable AWS SDK logging. Default is `false`. If `true`, AWS metrics and error responses are logged to the console.

AWS connection credentials will be found in the following order:

- If `AwsAccessKey` and `AwsSecretKey` are specified, these will be used.
- If `AwsProfile` is specified, the named profile will be used.
- Fallback to configuration file, default profile credentials, federated credentials and environment variables.

### Test configuration run

Test that the configuration files can be read and pass validation. AWS credentials are not required for this.

For example:

```ps1
dotnet .\Watchman.dll --RunMode TestConfig --ConfigFolder ".\configuration"
```

### Dry run

Shows what would happen - It does all the reads but none of the writes. AWS credentials are required for this.

For example:

```ps1
dotnet .\Watchman.dll --RunMode DryRun --ConfigFolder ".\configuration" --AwsAccessKey AKABC123 --AwsSecretKey abc123 --Verbose true
```

### Full run

A full read and write run. AWS credentials are required for this.

For example:

```ps1
dotnet .\Watchman.dll --RunMode GenerateAlarms --ConfigFolder ".\configuration" --AwsAccessKey AKABC123 --AwsSecretKey abc123
```

## Permissions needed

The user associated with the keys needs to be in several roles. These are all documented [in the Security Policy](SecurityPolicy.md).

## Alerts on

See [Supported Services](SupportedServices.md) for supported AWS services.

## Run sequence

When run, `Watchman` does things in approximately this order:

- Validate command-line parameters, and stop if they are invalid.
- Read the configuration folder and load all alerting groups.
- Validate the alerting groups in the configuration. Stop if the configuration is invalid. Stop if the run mode is `TestConfig`.
- For each resource type, across all alerting groups:
  - Populate resource names. Expand regular expression patterns into full resource names that match the pattern.
  - For the set of alarm definitions for each service (e.g. DynamoDB, SQS, RDS), apply either the default threshold from those definitions, or from the alerting group, or from the specific resource definition, depending on what is defined.
  - Create alarm models and create CloudFormation from them.
  - Commit the changes. Skipped if the run mode is `DryRun`.
  - Report on "orphans", i.e. resources that are not covered by any alarms in alerting group, excluding "CatchAll" groups.

## Quartermaster

[Quartermaster](Quartermaster.md) is a reporting tool to examine your DynamoDB usage. It will send a weekly _"Dynamo provisioning report"_ email to the reporting targets. This lists the read and write capacity, provisioned and peak of actual usage, across all all the DynamoDB tables and their indexes listed in the alerting group for the last week, and the usage as a percentage of provisioned capacity.

The percentage use can be used to track which tables and indexes are approaching the threshold, and which are over-provisioned. The intention is to allow you to change capacities up or down without first needing an alert.

## Configuration format

- [Configuration file format](ConfigurationFileFormat.md)
- [Configuration file examples](ConfigurationExamples.md)
- [Supported Services](SupportedServices.md)
