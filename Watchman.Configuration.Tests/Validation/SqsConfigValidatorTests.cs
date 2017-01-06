using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Watchman.Configuration.Tests.Validation
{
    [TestFixture]
    public class SqsConfigValidatorTests
    {
        private Sqs _sqs;
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _sqs = new Sqs
            {
                LengthThreshold = 42,
                OldestMessageThreshold = 42,
                Queues = new List<Queue>
                {
                    new Queue
                    {
                        Name = "QueueName",
                        LengthThreshold = 42,
                        OldestMessageThreshold = 42,
                        Errors = new ErrorQueue
                        {
                            LengthThreshold = 42,
                            OldestMessageThreshold = 42
                        }
                    }
                },
                Errors = new ErrorQueue
                {
                    LengthThreshold = 42,
                    OldestMessageThreshold = 42
                }
            };

            _config = ConfigTestData.ValidConfig();
            _config.AlertingGroups.First().Sqs = _sqs;
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_LengthThreshold_Is_Negative()
        {
            // arrange
            _sqs.LengthThreshold = -1;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-1' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_LengthThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.LengthThreshold = 100001;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100001' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_OldestMessageThreshold_Is_Negative()
        {
            // arrange
            _sqs.OldestMessageThreshold = -2;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-2' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_OldestMessageThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.OldestMessageThreshold = 100002;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100002' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_Error_LengthThreshold_Is_Negative()
        {
            // arrange
            _sqs.Errors.LengthThreshold = -3;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-3' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_Error_LengthThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Errors.LengthThreshold = 100003;

            // act

            // assert
            ConfigAssert.NotValid(_config,"Threshold of '100003' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_Error_OldestMessageThreshold_Is_Negative()
        {
            // arrange
            _sqs.Errors.OldestMessageThreshold = -4;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-4' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Sqs_Error_OldestMessageThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Errors.OldestMessageThreshold = 100004;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100004' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_If_Queue_Is_Null()
        {
            // arrange
            _sqs.Queues.Add(null);

            // act

            // assert
            ConfigAssert.NotValid(_config,"AlertingGroup 'someName' has a null queue");
        }

        [Test]
        public void SqsConfig_Fails_If_Queue_Has_No_Name_Or_Pattern()
        {
            // arrange
            _sqs.Queues.First().Name = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "AlertingGroup 'someName' has a queue with no name or pattern");
        }

        [Test]
        public void SqsConfig_Fails_If_Queue_Has_Name_And_Pattern()
        {
            // arrange
            _sqs.Queues.First().Pattern = "QueuePattern";

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a queue 'QueueName' with a name and a pattern");
        }

        [Test]
        public void AlertingGroupSucceedsWhenQueueHasPattern()
        {
            // arrange
            _sqs.Queues = new List<Queue>
            {
                new Queue {Pattern = "someQueue"}
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_LengthThreshold_Is_Negative()
        {
            // arrange
            _sqs.Queues.First().LengthThreshold = -5;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-5' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_LengthThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Queues.First().LengthThreshold = 100005;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100005' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_OldestMessageThreshold_Is_Negative()
        {
            // arrange
            _sqs.Queues.First().OldestMessageThreshold = -6;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-6' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_OldestMessageThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Queues.First().OldestMessageThreshold = 100006;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100006' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_Error_LengthThreshold_Is_Negative()
        {
            // arrange
            _sqs.Queues.First().Errors.LengthThreshold = -7;

            // act

            // assert
            ConfigAssert.NotValid(_config,"Threshold of '-7' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_Error_LengthThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Queues.First().Errors.LengthThreshold = 100007;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100007' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_Error_OldestMessageThreshold_Is_Negative()
        {
            // arrange
            _sqs.Queues.First().Errors.OldestMessageThreshold = -8;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '-8' must be greater than zero");
        }

        [Test]
        public void SqsConfig_Fails_When_Queue_Error_OldestMessageThreshold_Is_TooHigh()
        {
            // arrange
            _sqs.Queues.First().Errors.OldestMessageThreshold = 100008;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '100008' is ridiculously high");
        }

        [Test]
        public void SqsConfig_Full_Passes()
        {
            // arrange

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void SqsConfig_OnlyQueue_Passes()
        {
            // arrange
            _config.AlertingGroups.First().Sqs = new Sqs
            {
                Queues = new List<Queue>
                {
                    new Queue {Pattern = "QueuePattern"}
                }
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }
    }
}
