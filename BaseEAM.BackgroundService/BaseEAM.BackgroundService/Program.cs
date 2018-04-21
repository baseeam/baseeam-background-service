/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using System;
using System.Collections.Specialized;
using Autofac;
using Autofac.Extras.Quartz;
using Common.Logging;
using Quartz;
using Topshelf;
using Topshelf.Autofac;
using Topshelf.Quartz;
using Topshelf.ServiceConfigurators;
using BaseEAM.BackgroundService.Infrastructure;
using BaseEAM.Services;
using BaseEAM.Data;
using BaseEAM.Core.Infrastructure;

namespace BaseEAM.BackgroundService
{
    internal static class Program
    {
        static readonly ILog s_log = LogManager.GetLogger(typeof(Program));

        static int Main(string[] args)
        {
            s_log.Info("Starting...");

            try
            {
                //get builders and register services
                var backgroundServiceBuilder = BackgroundServiceEngine.Instance.BackgroundServiceBuilder;
                ConfigureContainer(backgroundServiceBuilder);
                BackgroundServiceEngine.Instance.Build();

                IContainer container = BackgroundServiceEngine.Instance.BackgroundServiceContainerManager.Container;

                ScheduleJobServiceConfiguratorExtensions.SchedulerFactory = () => container.Resolve<IScheduler>();

                HostFactory.Run(conf => {
                    conf.SetServiceName("BaseEAMCloudBackgroundService");
                    conf.SetDisplayName("BaseEAM Cloud Background Service");
                    conf.UseLog4Net();
                    conf.UseAutofacContainer(container);

                    conf.Service<ServiceCore>(svc => {
                        svc.ConstructUsingAutofacContainer();
                        svc.WhenStarted(o => o.Start());
                        svc.WhenStopped(o => {
                            o.Stop();
                            container.Dispose();
                        });
                        ConfigureScheduler(svc);
                    });
                });

                s_log.Info("Shutting down...");
                log4net.LogManager.Shutdown();

                //start StartupTask manually
                var auditLogStartupTask = new AuditLogStartupTask();
                auditLogStartupTask.Execute();

                return 0;
            }

            catch (Exception ex)
            {
                s_log.Fatal("Unhandled exception", ex);
                log4net.LogManager.Shutdown();
                return 1;
            }
        }

        static void ConfigureScheduler(ServiceConfigurator<ServiceCore> svc)
        {
            svc.ScheduleQuartzJob(q =>
            {
                //ClearUrlAccessedLog
                q.WithJob(JobBuilder.Create<ClearUrlAccessedLogJob>()
                    .WithIdentity("ClearUrlAccessedLog", "BackgroundJob")
                    .Build);
                q.AddTrigger(() => TriggerBuilder.Create()
                    .WithSchedule(SimpleScheduleBuilder.RepeatHourlyForever(24)).Build());

            }, true);

            svc.ScheduleQuartzJob(q =>
            {
                //SendMessageJob
                q.WithJob(JobBuilder.Create<SendMessageJob>()
                    .WithIdentity("SendMessageJob", "BackgroundJob")
                    .Build);
                q.AddTrigger(() => TriggerBuilder.Create()
                    .WithSchedule(SimpleScheduleBuilder.RepeatSecondlyForever(30)).Build());

            }, true);

            svc.ScheduleQuartzJob(q =>
            {
                //PMJob
                q.WithJob(JobBuilder.Create<PMJob>()
                    .WithIdentity("PMJob", "BackgroundJob")
                    .Build);
                q.AddTrigger(() => TriggerBuilder.Create()
                    .WithSchedule(SimpleScheduleBuilder.RepeatSecondlyForever(30)).Build());

            }, true);
        }

        internal static ContainerBuilder ConfigureContainer(ContainerBuilder cb)
        {
            // configure and register Quartz
            var schedulerConfig = new NameValueCollection {
                {"quartz.threadPool.threadCount", "3"},
                {"quartz.threadPool.threadNamePrefix", "BaseEAMSchedulerWorker"},
                {"quartz.scheduler.threadName", "BaseEAMScheduler"},
                {"quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz"},
                {"quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.MySQLDelegate, Quartz"},
                {"quartz.jobStore.tablePrefix", "QRTZ_"},
                {"quartz.jobStore.dataSource", "BaseEAM"},
                {"quartz.dataSource.BaseEAM.connectionString", "Server=localhost;port=3306;Uid=root;Pwd=Ng112358$;Database=EAM;"},
                {"quartz.dataSource.BaseEAM.provider", "MySql-65"},
                {"quartz.jobStore.useProperties", "true"},
                {"quartz.scheduler.instanceName", "ServerScheduler"},
                {"quartz.scheduler.exporter.type", "Quartz.Simpl.RemotingSchedulerExporter, Quartz"},
                {"quartz.scheduler.exporter.port", "555"},
                {"quartz.scheduler.exporter.bindName", "QuartzScheduler"},
                {"quartz.scheduler.exporter.channelType", "tcp"},
                {"quartz.scheduler.exporter.channelName", "httpQuartz"}
            };

            cb.RegisterModule(new QuartzAutofacFactoryModule
            {
                ConfigurationProvider = c => schedulerConfig
            });
            cb.RegisterModule(new QuartzAutofacJobsModule(typeof(HeartbeatJob).Assembly));
            cb.RegisterModule(new QuartzAutofacJobsModule(typeof(SendMessageJob).Assembly));
            RegisterComponents(cb);
            return cb;
        }

        internal static void RegisterComponents(ContainerBuilder cb)
        {
            // register Service instance
            cb.RegisterType<ServiceCore>().AsSelf();
            // register dependencies
            cb.RegisterType<HeartbeatService>().As<IHeartbeatService>();

            //register BaseEAM's services
            AutoFacConfig.RegisterServices(cb);
        }
    }
}
