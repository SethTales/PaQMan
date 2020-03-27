using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using PaQMan.Brokers.Interfaces;
using PaQMan.Factories.Interfaces;
using PaQMan.Workers.Implementations;

[assembly: InternalsVisibleTo("PaqManUnitTests")]
namespace PaQMan.Brokers.Implementations
{
    public class AwsSqsBroker : IBroker<Message>
    {
        private readonly IAwsSqsQueueWorkerFactory _sqsQueueWorkerFactory;
        private readonly IAmazonSQS _amazonSqsClient;
        private readonly string _queueUrl;
        private readonly int _queueWaitTimeInSeconds;
        private readonly int _messageVisibilityTimeout;
        private readonly int _maxNumberOfMessages;
        private readonly Action<Message> _onTimeoutCallback;
        private readonly int _maxConcurrency;
        private readonly ConcurrentDictionary<int, AwsSqsQueueWorker> _workerMap;

        public AwsSqsBroker(IAwsSqsQueueWorkerFactory sqsQueueWorkerFactory, IAmazonSQS amazonSqsClient, string queueUrl)
        {
            _sqsQueueWorkerFactory = sqsQueueWorkerFactory;
            _amazonSqsClient = amazonSqsClient;
            _queueUrl = queueUrl;
            _queueWaitTimeInSeconds = 20;
            _messageVisibilityTimeout = 30;
            _maxNumberOfMessages = 1;
            _onTimeoutCallback = OnTimeoutCallback;
            _maxConcurrency = 10;
            _workerMap = new ConcurrentDictionary<int, AwsSqsQueueWorker>();
        }

        public async Task ProcessAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await PollAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unknown error occurred: {ex}");
                    }
                }
            }, CancellationToken.None);
        }

        internal async Task PollAsync()
        {
            var receiveMessageRequest = new ReceiveMessageRequest(_queueUrl)
            {
                WaitTimeSeconds = _queueWaitTimeInSeconds,
                VisibilityTimeout = _messageVisibilityTimeout,
                MaxNumberOfMessages = _maxNumberOfMessages
            };

            var messageResponse = await _amazonSqsClient.ReceiveMessageAsync(receiveMessageRequest);

            if (messageResponse.Messages == null || !messageResponse.Messages.Any())
            {
                return;
            }

            foreach (var message in messageResponse.Messages)
            {
                var workDistributed = false;
                while (!workDistributed)
                {
                    workDistributed = DistributeWork(message);
                    if (!workDistributed)
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }

        private bool DistributeWork(Message message)
        {
            foreach (var taskWorkerPair in _workerMap)
            {
                var trackedTimedTask = taskWorkerPair.Value;
                //TODO: error handling
                if (trackedTimedTask.BackgroundTask.IsCompleted || trackedTimedTask.BackgroundTask.IsCompletedSuccessfully || trackedTimedTask.BackgroundTask.IsFaulted)
                {
                    _workerMap.TryRemove(taskWorkerPair.Key, out _);
                    Console.WriteLine($"Removed finishd task {taskWorkerPair.Key}");
                }
            }

            if (_workerMap.Count >= _maxConcurrency)
            {
                return false;
            }

            var queueWorker = _sqsQueueWorkerFactory.BuildAwsSqsQueueWorker(message);
            queueWorker.TimeoutCallback = OnTimeoutCallback;
            var workerTask = new Task(() => queueWorker.ExecuteAsync());
            queueWorker.BackgroundTask = workerTask;
            var taskAdded = _workerMap.TryAdd(workerTask.Id, queueWorker);
            queueWorker.BackgroundTask.Start();
            return taskAdded;
        }

        private void OnTimeoutCallback(Message message)
        {
            var changeVisibilityRequest = new ChangeMessageVisibilityRequest
            {
                QueueUrl = _queueUrl,
                VisibilityTimeout = 30,
                ReceiptHandle = message.ReceiptHandle
            };
            _amazonSqsClient.ChangeMessageVisibilityAsync(changeVisibilityRequest, CancellationToken.None).RunSynchronously();
        }
    }
}
