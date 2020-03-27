using System.Linq;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using PaQMan.Brokers.Implementations;
using PaQMan.Factories.Implementations;
using PaQMan.Factories.Interfaces;

namespace PaQManUnitTests
{
    [TestFixture]
    public class PaQManUnitTests
    {
        [Test]
        public void PollAsyncShouldNotThrowException_ForSimpleTestCase()
        {
            var mockSqsClient = Substitute.For<IAmazonSQS>();
            var sqsWorkerFactory = new AwsQueueWorkerFactory();
            var broker = new AwsSqsBroker(sqsWorkerFactory, mockSqsClient, "queue-url");
            var response = new ReceiveMessageResponse();
            var message = new TestQueueMessage
            {
                Name = "Seth"
            };
            var messageBody = JsonConvert.SerializeObject(message);
            response.Messages.Add(new Message { Body = messageBody });
            mockSqsClient.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .Returns(response);

            Assert.DoesNotThrowAsync(() => broker.PollAsync());
        }

        [Test]
        public async Task ShouldCallRenewMessageVisibility_WhenTaskAgeExceedsTimeout()
        {
            var mockSqsClient = Substitute.For<IAmazonSQS>();
            var mockWorkerFactory = Substitute.For<IAwsSqsQueueWorkerFactory>();
            var broker = new AwsSqsBroker(mockWorkerFactory, mockSqsClient, "queue-url");
            var response = new ReceiveMessageResponse();
            var message = new TestQueueMessage
            {
                Name = "Seth"
            };
            var messageBody = JsonConvert.SerializeObject(message);
            response.Messages.Add(new Message { Body = messageBody });
            mockSqsClient.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>())
                .Returns(response);
            var taskTimeout = 2000;
            mockWorkerFactory.BuildAwsSqsQueueWorker(Arg.Any<Message>())
                .Returns(new TestableAwsSqsQueueWorker(response.Messages.First(), taskTimeout));

            await broker.PollAsync();
            await Task.Delay(taskTimeout + 100);

            await mockSqsClient.Received(1).ChangeMessageVisibilityAsync(Arg.Any<ChangeMessageVisibilityRequest>());
        }
    }
}
