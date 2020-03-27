using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PaQMan.Workers.Interfaces;

namespace PaQMan.Brokers.Interfaces
{
    public interface IBroker<in T>
    {
        Task ProcessAsync();
    }
}
