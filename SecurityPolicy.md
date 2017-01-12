# Security policy for AWS Watchman 


Permission necessary for AWS Watchman to do all the things. 
* Create An IAM user for Watchman
* Allow access with key and secret. Take note of those, you will use them on the commandline or in a profile
* Add the following as inline policies to the user (You may want to replace `"Resource": [ "*" ]` with something more specific to the account).

## CanDo_CloudwatchAlarms

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": [
				"cloudwatch:PutMetricAlarm",
				"cloudwatch:DeleteAlarms",
				"cloudwatch:DescribeAlarms",
				"cloudwatch:GetMetricData",
				"cloudwatch:GetMetricStatistics",
				"cloudwatch:ListMetrics"
			],
			"Resource": [
				"*"
			]
		}
	]
}
```

## CanDo_SNS

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": [
				"sns:ListTopics",
				"sns:CreateTopic",
				"sns:ListSubscriptionsByTopic",
				"sns:Subscribe"
			],
			"Resource": [
				"arn:aws:sns:<region>:<account-id>:*"
			]
		}
	]
}
```

## CanDo_CloudformationAlarmDeployment

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": [
				"cloudformation:ListStacks",
				"cloudformation:CreateStack"
			],
			"Resource": [
				"*"
			]
		},
		{
			"Effect": "Allow",
			"Action": [
				"cloudformation:UpdateStack"
			],
			"Resource": [
				"arn:aws:cloudformation:<region>:<account-id>:stack/Watchman*"
			]
		},
		{
			"Effect": "Allow",
			"Action": [
				"s3:PutObject"
			],
			"Resource": [
				"arn:aws:s3:::<watchman bucket>/*"
			]
		}
	]
}
```

## CanDo_DescribeResources

```json
{
	"Version": "2012-10-17",
	"Statement": [
		{
			"Effect": "Allow",
			"Action": [
				"autoscaling:DescribeAutoScalingGroups",
				"elasticloadbalancing:DescribeLoadBalancers",
				"rds:DescribeDBInstances",
				"lambda:ListFunctions",
				"sqs:GetQueueAttributes",
				"ec2:DescribeSubnets",
				"dynamodb:DescribeTable",
				"dynamodb:ListTables"
			],
			"Resource": [
				"*"
			]
		}
	]
}
```

