using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Amazon.SQS.Model;
using Timer = System.Timers.Timer;

namespace PaQMan.Workers.Implementations
{
    public class AwsSqsQueueWorker
    {
        internal Action<Message> TimeoutCallback { get; set; }
        internal Message Message { get; }
        internal Task BackgroundTask { get; set; }
        private readonly int _taskTimeout;

        internal AwsSqsQueueWorker(Message message, int taskTimeout = 25000)
        {
            Message = message;
            _taskTimeout = taskTimeout;
        }

        internal async Task ExecuteAsync()
        {
            var taskTimer = new Timer(_taskTimeout);
            taskTimer.Elapsed += OnTimedEvent;
            taskTimer.Enabled = true;
            await ConsumeAsync();
        }

        internal virtual async Task ConsumeAsync()
        {
            await Task.Delay(1000);
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            TimeoutCallback?.Invoke(Message);
        }
    }
}
