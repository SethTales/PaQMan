using System;
using System.Collections.Generic;
using System.Text;
using Amazon.SQS.Model;
using PaQMan.Factories.Interfaces;
using PaQMan.Workers.Implementations;

namespace PaQMan.Factories.Implementations
{
    public class AwsQueueWorkerFactory : IAwsSqsQueueWorkerFactory
    {
        public AwsSqsQueueWorker BuildAwsSqsQueueWorker(Message message, int messageTimeout = 25000)
        {
            return new AwsSqsQueueWorker(message, messageTimeout);
        }
    }
}
