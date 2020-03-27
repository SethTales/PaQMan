using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PaQMan.Workers.Interfaces
{
    public interface IQueueWorker<in T>
    {
        Task ConsumeAsync(T input);
        void Consume(T input);
    }
}
