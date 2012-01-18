using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HyperCloud
{
    public interface IManager
    {
        IEnumerable<IProcessor> Processors { get; }
        //IEnumerable<ICommand> Commands { get; }
    }
}
