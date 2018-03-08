/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using System;
using Common.Logging;
using Quartz;

namespace BaseEAM.BackgroundService.Infrastructure
{
    public class ServiceCore
    {
        private static readonly ILog s_log = LogManager.GetLogger<ServiceCore>();
        private readonly IScheduler _scheduler;

        public ServiceCore(IScheduler scheduler)
        {
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

            _scheduler = scheduler;
        }

        public bool Start()
        {
            s_log.Info("Service started");

            if (!_scheduler.IsStarted)
            {
                s_log.Info("Starting Scheduler");
                _scheduler.Start();
            }
            return true;
        }

        public bool Stop()
        {
            s_log.Info("Stopping Scheduler...");
            _scheduler.Shutdown(true);

            s_log.Info("Service stopped");
            return true;
        }
    }
}
