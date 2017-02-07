# Configuration of other service types

## Alerting group

One or more alerting groups should be defined in the config folder that you supply to watchman.

```
{
  "Name": "TEST",
  "AlarmNameSuffix": "TestingGroup",
  "Targets": [
    { "Email": "email@just-eat.com" }
  ],
  "Services": {
  	"ServiceIdentifier": { /* service */ }
  }
}

```

## Service definition

Configuration for the additional services is added to the alerting group under the "Services" property. (note that configuration for Dynamo and SQS requires a different configuration format - see existing documentation).


They all require an object in this format:

```
{
	// [optional] defines default thresholds and evaluation periods for the alerting group
	"Values": {
  		"AlarmName": 31
  	},

  	// Resources for this service type
	"Resources": [
		// regular expression match of resource name
		{
       		"Pattern": "^.*$", 

       		// [optional] override the thresholds defined elsewhere
        	"Values": {
        		"AlarmName": 11
        	}
     	},

     	// exact match of resource name
     	{
       		"Name": "Resource"        	
     	},

     	// or
     	"ResourceName"
     ]	
}
```

# Resource types

The following services are supported

- `Rds`
- `AutoScaling`
- `Lambda`
- `Elb`
- `VpcSubnet` (this is a custom service using JUST EAT custom metrics)

## Alarm names and default thresholds

For each resource each of the default alarms will be applied. See [alarm definitions](Watchman.Engine/Alarms/Defaults.cs) for more details of periods etc.

### Rds

- `FreeStorageSpaceLow`: 30 (%)
- `CPUUtilizationHigh`: 60 (%)
- `DatabaseConnectionsHigh`: 200 (count)

### AutoScaling

- `CPUCreditBalanceLow`: 0.2 (credit balance)
- `CPUUtilizationHigh`: 90 (%)
- `GroupInServiceInstancesLow`: 50 (% of desired)

### Lambda

- `ErrorsHigh`: 3 (count)
- `DurationHigh`: 50 (% of defined Timeout)
- `ThrottlesHigh`: 5 (count)

### VpcSubnets

- `IpAddressesRemainingLow` 30 (% of subnet allocation)

### Elb

Note that using the defaults here for all alarms is probably not that useful.

- `Elb5xxErrorsHigh`: 50 (count)
- `Http5xxErrorsHigh`: 500 (count)
- `SurgeQueueLengthHigh`: 200 (count)
- `SpilloverCountHigh`: 10 (count)
- `LatencyHigh`: 500 (ms)
- `UnHealthyHostCountHigh`: 1 (count)

## Full example

```
{
	"Name": "TEST",
  	"AlarmNameSuffix": "TestingGroup",
  	"Targets": [
    	{ "Email": "email@just-eat.com" }
  	],
  	"Services": {
    	"Rds": { 
    		"Resources": [{
       			"Pattern": "allorest", 
        		"Thresholds": {
        			"FreeStorageSpaceLow": 20
        		}
    		},{
    			"Name": "something"
    		},{
    			"Name": "another"
    		}],
    		"Values": {
      			"FreeStorageSpaceLow": 31
      		}
	    }
	    "Lambda": {
      		"Resources": [
      			{"Pattern": "api-"}
      		],
      		"Values": {
      			"ErrorsHigh": 10
      		}
	    }
    }
}
```
