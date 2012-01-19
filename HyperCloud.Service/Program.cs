using System;
using System.IO;
using Topshelf;
using log4net;
using log4net.Config;
using Topshelf.Model;
using Topshelf.Messages;
using Topshelf.FileSystem;

namespace HyperCloud.Controller
{
    public class Program
    {
        public static readonly ILog _log = LogManager.GetLogger("HyperCloud");

        readonly IServiceChannel _serviceChannel;

        [LoaderOptimization(LoaderOptimization.MultiDomainHost)]
        static void Main(string[] args)
        {
            BootstrapLogger();
            HostFactory.Run(x =>
            {
                x.BeforeStartingServices(() => Console.WriteLine("[HyperCloud] Preparing to start host services"));
                x.AfterStartingServices(() => Console.WriteLine("[HyperCloud] All services have been started"));

                x.SetServiceName("HyperCloud");
                x.SetDisplayName("HyperCloud");
                x.SetDescription("HyperCloud Service Controller");

                x.RunAsLocalSystem();

                x.EnableDashboard();

                x.Service<Program>(y =>
                {
                    y.SetServiceName("HyperCloud");
                    y.ConstructUsing((name, coordinator) => new Program(coordinator));
                    y.WhenStarted(host => host.Start());
                    y.WhenStopped(host => host.Stop());
                });
                x.AfterStoppingServices(() => Console.WriteLine("[HyperCloud] All services have been stopped"));
            });

            // shutdown log4net just before we exit!
            LogManager.Shutdown();
        }

        static void BootstrapLogger()
        {
            string configurationFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

            var configurationFile = new FileInfo(configurationFilePath);

            // if we can't find the log4net configuration file, perform a basic configuration which at
            // least logs to trace/debug, which means we can attach a debugger
            // to the process!
            if (configurationFile.Exists)
                XmlConfigurator.ConfigureAndWatch(configurationFile);
            else
                BasicConfigurator.Configure();

            _log.DebugFormat("Logging configuration loaded: {0}", configurationFilePath);
        }

        public Program(IServiceChannel serviceChannel)
        {
            _serviceChannel = serviceChannel;
        }

        public void Start()
        {
            var message = new CreateShelfService("HyperCloud.DirectoryMonitor", ShelfType.Internal, typeof(DirectoryMonitorBootstrapper));
            _serviceChannel.Send(message);
        }

        public void Stop()
        {
        }
    }
}
