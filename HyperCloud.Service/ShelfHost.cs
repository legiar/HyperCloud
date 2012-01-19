using System;

namespace HyperCloud.Controller
{
    using Topshelf.FileSystem;
    using Topshelf.Messages;
    using Topshelf.Model;

    public class ShelfHost
    {
        public const string DefaultServiceName = "HyperCloud";

        readonly IServiceChannel _serviceChannel;

        public ShelfHost(IServiceChannel serviceChannel)
        {
            _serviceChannel = serviceChannel;
        }

        public void Start()
        {
            CreateDirectoryMonitor();
        }

        void CreateDirectoryMonitor()
        {
            var message = new CreateShelfService("HyperCloud.DirectoryMonitor", ShelfType.Internal,
                typeof(DirectoryMonitorBootstrapper));
            _serviceChannel.Send(message);
        }

        public void Stop()
        {
        }
    }
}