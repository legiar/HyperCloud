using System;
using System.Collections.Generic;
using System.Text;

namespace HyperCloud
{
    public interface ICommand
    {
        void Initialize(IBus bus, ILogger logger);
        void Execute<TMessage>(TMessage message);
    }
}
