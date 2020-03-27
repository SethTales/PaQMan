using System;
using System.Collections.Generic;
using System.Text;
using Amazon.SQS.Model;
using PaQMan.Workers.Implementations;

namespace PaQMan.Factories.Interfaces
{
    public interface IAwsSqsQueueWorkerFactory
    {
        AwsSqsQueueWorker BuildAwsSqsQueueWorker(Message message, int messageTimeout = 25000);
    }
}
