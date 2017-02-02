using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Watchman.Configuration;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Generic
{
    public class CloudFormationAlarmCreator : IAlarmCreator
    {
        private const int CloudformationRequestBodyLimit = 51200;
        private readonly IAlarmLogger _logger;
        private readonly IAmazonCloudFormation _cloudformation;

        private readonly IAmazonS3 _s3Client;
        private readonly S3Location _s3Location;

        private readonly List<Alarm> _alarms = new List<Alarm>();

        private readonly TimeSpan _stackStatusCheckInterval;
        private readonly TimeSpan _stackStatusCheckTimeout;

        private readonly List<string> _stackStatusFilter = new List<string>
        {
            // this is just to exclude deleted ones (DELETE COMPLETE), because they don't exist
            "CREATE_IN_PROGRESS",
            "CREATE_FAILED",
            "CREATE_COMPLETE",
            "ROLLBACK_IN_PROGRESS",
            "ROLLBACK_FAILED",
            "ROLLBACK_COMPLETE",
            "DELETE_IN_PROGRESS",
            "DELETE_FAILED",
            "UPDATE_IN_PROGRESS",
            "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS",
            "UPDATE_COMPLETE",
            "UPDATE_ROLLBACK_IN_PROGRESS",
            "UPDATE_ROLLBACK_FAILED",
            "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
            "UPDATE_ROLLBACK_COMPLETE",
            "REVIEW_IN_PROGRESS"
        };

        private readonly List<string> _stackFailureStatuses = new List<string>
        {
            "CREATE_FAILED",
            "ROLLBACK_IN_PROGRESS",
            "ROLLBACK_FAILED",
            "ROLLBACK_COMPLETE",
            "UPDATE_ROLLBACK_IN_PROGRESS",
            "UPDATE_ROLLBACK_FAILED",
            "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
            "UPDATE_ROLLBACK_COMPLETE"
        };

        public CloudFormationAlarmCreator(
            IAlarmLogger logger,
            IAmazonCloudFormation cloudformation,
            IAmazonS3 s3Client,
            S3Location s3Location) : this(logger, cloudformation, s3Client, s3Location, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(5))
        {
        }

        public CloudFormationAlarmCreator(
            IAlarmLogger logger,
            IAmazonCloudFormation cloudformation,
            IAmazonS3 s3Client,
            S3Location s3Location,
            TimeSpan wait, TimeSpan timeout)
        {
            _logger = logger;
            _cloudformation = cloudformation;
            _stackStatusCheckTimeout = timeout;
            _stackStatusCheckInterval = wait;
            _s3Client = s3Client;
            _s3Location = s3Location;
        }

        public void AddAlarm(Alarm alarm)
        {
            if (alarm.AlarmDefinition.Threshold.ThresholdType != ThresholdType.Absolute)
            {
                throw new Exception("Threshold type must be absolute for creation");
            }

            _alarms.Add(alarm);
        }

        public async Task SaveChanges(bool dryRun)
        {
            var groupedBySuffix = _alarms
                .GroupBy(x => x.AlertingGroup.AlarmNameSuffix,
                    x => x,
                    (g, x) => new
                    {
                        Suffix = g,
                        Alarms = x
                    });

            foreach (var group in groupedBySuffix)
            {
                var stackName = "Watchman-" + group.Suffix;
                var template = new CloudWatchCloudFormationTemplate();
                template.AddAlarms(group.Alarms);
                var json = template.WriteJson();

                var allStacks = await AllStacks();

                var isUpdate = allStacks.Contains(stackName);

                await CommitStackChanges(stackName, isUpdate, dryRun, json);
            }
        }

        private async Task<IEnumerable<string>> AllStacks()
        {
            string nextToken = null;
            var results = new List<string>();

            do
            {
                var allStacks = await _cloudformation.ListStacksAsync(new ListStacksRequest
                {
                    StackStatusFilter = _stackStatusFilter,
                    NextToken = nextToken
                });

                nextToken = allStacks.NextToken;

                results.AddRange(allStacks.StackSummaries.Select(x => x.StackName));
            } while (nextToken != null);

            return results;
        }

        private Task Commit(UpdateStackRequest r)
        {
            Func<Task> f = () => _cloudformation.UpdateStackAsync(r);
            return Commit(f, r.StackName, StackStatus.UPDATE_COMPLETE);
        }

        private Task Commit(CreateStackRequest r)
        {
            Func<Task> f = () => _cloudformation.CreateStackAsync(r);
            return Commit(f, r.StackName, StackStatus.CREATE_COMPLETE);
        }

        private async Task Commit(Func<Task> f, string stackName, StackStatus targetStatus)
        {
            try
            {
                await f();
            }
            catch (AmazonCloudFormationException ex)
            {
                // evil
                if (ex.ErrorCode == "ValidationError" && ex.Message == "No updates are to be performed.")
                {
                    _logger.Info("No stack updates required");
                }
                else
                {
                    _logger.Error(ex, "Failed to create or update stack");
                    throw;
                }

                return;
            }

            var didComplete = await WaitForStackToReachStatus(stackName, targetStatus);
            if (!didComplete)
            {
                throw new Exception("Cloudformation stack did reach target status");
            }
        }


        private async Task CommitStackChanges(string stackName, bool isUpdate, bool isDryRun, string body)
        {
            string templateUrl = null;
            string templateBody = null;

            if (isDryRun)
            {
                _logger.Info("Skipping save due to dry run");
                return;
            }

            if (body.Length >= CloudformationRequestBodyLimit)
            {
                templateUrl = await CopyTemplateToS3(stackName, body);
            }
            else
            {
                templateBody = body;
            }

            if (isUpdate)
            {
                _logger.Info($"Stack {stackName} exists, updating");

                await Commit(new UpdateStackRequest
                {
                    StackName = stackName,
                    TemplateURL = templateUrl,
                    TemplateBody = templateBody
                });
            }
            else
            {
                _logger.Info($"Stack {stackName} does not exist, creating");

                await Commit(new CreateStackRequest
                {
                    StackName = stackName,
                    TemplateURL = templateUrl,
                    TemplateBody = templateBody
                });
            }
        }

        private async Task<string> CopyTemplateToS3(string stackName, string body)
        {
            if (_s3Location == null)
            {
                throw new Exception("Cannot create large cloudformation stack without s3 configuration");
            }

            // would be good if we could use some hash to check the remote version before uploading

            var s3Path = $"{_s3Location.Path}/{stackName}.json";

            _logger.Info($"Uploading template to s3://{_s3Location.BucketName}/{s3Path}");

            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                Key = s3Path,
                ContentBody = body,
                BucketName = _s3Location.BucketName
            });

            // can't use s3:// for some reason
            return $"https://s3.amazonaws.com/{_s3Location.BucketName}/{s3Path}";
        }

        private async Task<bool> WaitForStackToReachStatus(string stackName, StackStatus desiredStatus)
        {
            var elapsed = TimeSpan.Zero;

            do
            {
                await Task.Delay(_stackStatusCheckInterval);

                var stack = (await _cloudformation.DescribeStacksAsync(new DescribeStacksRequest
                {
                    StackName = stackName
                })).Stacks.Single();

                if (stack.StackStatus == desiredStatus)
                {
                    _logger.Info($"Stack {stackName} reached target status {stack.StackStatus}");
                    return true;
                }

                if (elapsed >= _stackStatusCheckTimeout)
                {
                    _logger.Error($"Stack {stackName} change timeout reached");
                    return false;
                }

                if (_stackFailureStatuses.Contains(stack.StackStatus.ToString(), StringComparer.InvariantCultureIgnoreCase))
                {
                    _logger.Error($"Stack {stackName} reached status {stack.StackStatus} with reason {stack.StackStatusReason}");
                    return false;
                }

                _logger.Info($"Stack {stackName} at status {stack.StackStatus}, waiting");
            } while (true);
        }
    }
}
