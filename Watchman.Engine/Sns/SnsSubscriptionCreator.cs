using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Watchman.Configuration;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Sns
{
    public class SnsSubscriptionCreator : ISnsSubscriptionCreator
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IAlarmLogger _logger;

        public SnsSubscriptionCreator(IAlarmLogger logger, IAmazonSimpleNotificationService snsClient)
        {
            _logger = logger;
            _snsClient = snsClient;
        }

        public Task EnsureSnsSubscriptions(AlertingGroup alertingGroup, string snsTopicArn)
        {
            return EnsureSnsSubscriptions(alertingGroup.Targets, snsTopicArn);
        }
        public async Task EnsureSnsSubscriptions(IEnumerable<AlertTarget> alertingTargets, string snsTopicArn)
        {
            if (string.IsNullOrWhiteSpace(snsTopicArn))
            {
                throw new ArgumentNullException(nameof(snsTopicArn));
            }

            if (alertingTargets == null || !alertingTargets.Any())
            {
                _logger.Info($"No targets. No Sns Subscriptions will be created for {snsTopicArn}");
                return;
            }

            foreach (var target in alertingTargets)
            {
                if (target is AlertEmail)
                {
                    await EnsureSnsEmailSubscription(target as AlertEmail, snsTopicArn);
                }
                else if (target is AlertUrl)
                {
                    await EnsureSnsUrlSubscription(target as AlertUrl, snsTopicArn);
                }
                else
                {
                    throw new ArgumentException($"The target type '{target.GetType()}' was not found.");
                }
            }
        }

        private async Task EnsureSnsEmailSubscription(AlertEmail alert, string snsTopicArn)
        {
            await EnsureSnsSubscription(snsTopicArn, "email", alert.Email);
        }

        private async Task EnsureSnsUrlSubscription(AlertUrl alert, string snsTopicArn)
        {
            if (string.IsNullOrWhiteSpace(alert.Url))
            {
                return;
                //throw new ArgumentNullException(nameof(alert.Url));
            }

            var protocol = alert.Url.Split(':').First();
            await EnsureSnsSubscription(snsTopicArn, protocol, alert.Url);
        }

        private async Task EnsureSnsSubscription(string snsTopicArn, string protocol, string endpoint)
        {
            if (string.IsNullOrWhiteSpace(snsTopicArn))
            {
                throw new ArgumentNullException(nameof(snsTopicArn));
            }
            if (string.IsNullOrWhiteSpace(protocol))
            {
                throw new ArgumentNullException(nameof(protocol));
            }
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            var subExists = await SnsSubscriptionExists(snsTopicArn, protocol, endpoint);

            if (! subExists)
            {
                await _snsClient.SubscribeAsync(new SubscribeRequest(snsTopicArn, protocol, endpoint));
                _logger.Info($"Created subscription from SNS topic ARN {snsTopicArn} to {protocol} {endpoint}");
            }
            else
            {
                _logger.Detail($"Already have subscription from SNS topic ARN {snsTopicArn} to {protocol} {endpoint}");
            }
        }

        private async Task<bool> SnsSubscriptionExists(string snsTopicArn, string protocol, string endpoint)
        {
            var response = await _snsClient.ListSubscriptionsByTopicAsync(snsTopicArn);

            if (response?.Subscriptions == null)
            {
                return false;
            }

            return response.Subscriptions.Any(x =>
                x.TopicArn == snsTopicArn &&
                x.Protocol == protocol &&
                x.Endpoint == endpoint);
        }
    }
}
