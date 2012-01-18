using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Configuration;

namespace HyperCloud
{
    public interface IProcessor
    {
        void Configure(ConfigurationElement config, IBus bus, ILogger logger);
        void Execute();
    }

    public class BasicProcessor : IProcessor
    {
        private Configuration.ProcessorSectionElement _config;
        private IBus _bus;
        private ILogger _logger;

        public Configuration.ProcessorSectionElement Config
        {
            get { return _config; }
        }

        public IBus Bus
        {
            get { return _bus; }
        }

        public ILogger Logger
        {
            get { return _logger; }
        }

        public void Configure(ConfigurationElement config, IBus bus, ILogger logger)
        {
            _config = config as Configuration.ProcessorSectionElement;
            _bus = bus;
            _logger = logger;
            if (_logger == null) {
                _logger = new Loggers.EmptyLogger();
            }
        }

        virtual public void Execute()
        {
        }
    }
}
