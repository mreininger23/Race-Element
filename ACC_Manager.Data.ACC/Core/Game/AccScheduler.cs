﻿using Quartz;
using Quartz.Impl;
using RaceElement.Data.ACC.Core.Game.Jobs;

namespace ACCManager.Data.ACC.Core.Game
{
    public static class AccScheduler
    {
        public static IScheduler Scheduler;

        public static void RegisterJobs()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            Scheduler = factory.GetScheduler().Result;
            Scheduler.Start();

            ReplaySaver.Schedule();
        }
    }
}
