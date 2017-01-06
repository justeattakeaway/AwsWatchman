# QuarterMaster

Quartermaster is a tool for reporting on dynamo provisioning and consumption.  It will examine the supplied Watchman configuration, and generate a report on each of the configured alerting groups.  The Report will include the read & write provisions and the highest peaks of consumption for the read and write on all of the configured tables and their indexes.  It will store all of the generated reports in the reports/ folder in CSV format, and optionally mail the report to any configured ReportTarget.

## Run

Run the exe - specifying aws credentials and a config folder for config files (see config schema, below).

e.g.
```
quartermaster.exe --AwsAccessKey AK123BC456 --AwsSecretKey abcd1234 --ConfigFolder ".\Configs"
```

The command line parameters for specifying AWS credentials, `AwsAccessKey`, `AwsSecretKey`, `AwsRegion`, and `AwsProfile` work as in Watchman. 

The user associated with the credentials needs the following roles:

- dynamodb:DescribeTable
- cloudwatch:GetMetricData
- cloudwatch:GetMetricStatistics

## Config Schema

Configuration for Watchman is read from [DynamoWatchman.Config](https://github.je-labs.com/cogpart/DynamoWatchman.Config) project.

## Report 

The report is generated as a CSV file, and includes the following data per row

| Field | Description |
|-----------|------|-----------|------------|
| TableName | Dynamo Table Name|
| IndexName | Secondary Index Name (Blank if the line refers to the table's provisioning) |
| ProvisionedReadCapacityUnits | Raw "ProvisionedRead" figure |
| ProvisionedWriteCapacityUnits | Raw "ProvisionedWrite" figure |
| ProvisionedReadPerMinute | Provisioned Reads per Minute (ProvisionedReadCapacityUnits * 60) |
| ProvisionedWritePerMinute | Provisioned Writes per Minute (ProvisionedWriteCapacityUnits * 60)|
| MaxConsumedReadPerMinute | Peak Consumed Reads per minute. |
| MaxConsumedWritePerMinute | Peak Consumed Writes per minute. |
| ReadUsePercentage | MaxConsumedReadPerMinute as a percentage of ProvisionedReadPerMinute |
| WriteUsePercentage | MaxConsumedWritePerMinute as a percentage of ProvisionedWritePerMinute |

## Using the Report

The numbers you should care about are your ReadUse/WriteUse Percentages (the last two columns); They represent the peak capacity consumption vs. the current provisioning.  Any number of 75% is probably bad, and you should increase the relevant table capacity to grant more headroom against spikes in consumption.  Dynamo Watchman should be setting your alarms to 80% of capacity by default, so getting near to that limit probably means you'll see alarms during peak.
