using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using PaQMan.Workers.Implementations;

namespace PaQManUnitTests
{
    internal class TestableAwsSqsQueueWorker : AwsSqsQueueWorker
    {
        private readonly int _taskTimeout;
        internal TestableAwsSqsQueueWorker(Message message, int taskTimeout) : base(message, taskTimeout)
        {
            _taskTimeout = taskTimeout;
        }

        internal override async Task ConsumeAsync()
        {
            await Task.Delay(_taskTimeout + 500);
        }
    }
}
