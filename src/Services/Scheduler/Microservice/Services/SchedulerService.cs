using Liquid;
using Liquid.Activation;
using Liquid.Base;
using Liquid.Domain;
using Liquid.OnAzure;
using Liquid.Repository;
using Microservice.ViewModels;
using NCrontab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class SchedulerService : LightService
    {
        private const int _MAX_UNHANDLED_ACTIVATIONS = 3;

        private static readonly SchedulerMessageBus<ServiceBus> schedulerMessageBus = new("TRANSACTIONAL");

        private static ConcurrentDictionary<string, Models.Schedule> inMemoryScheduleList;
        protected static ConcurrentDictionary<string, Models.Schedule> InMemoryRepo
        {
            get
            {
                if (inMemoryScheduleList is null)
                {
                    inMemoryScheduleList = new();

                    var persistedSchedules = Repository.GetAll<Models.Schedule>();

                    foreach (var schedule in persistedSchedules)
                    {
                        inMemoryScheduleList.TryAdd(schedule.Id, schedule);
                    }

                }
                return inMemoryScheduleList;
            }
        }

        public DomainResponse GetJob(string microservice, string jobName)
        {
            Telemetry.TrackEvent("Get Scheduled Job", $"{microservice}/{jobName}");

            var keyAndValue = InMemoryRepo.Where(kvp => kvp.Value.Microservice == microservice && kvp.Value.Job == jobName).FirstOrDefault().Value;

            if (keyAndValue is null)
                return NoContent();

            return Response(ScheduleVM.FactoryFrom(keyAndValue));
        }

        public DomainResponse GetByMicroservice(string microservice)
        {
            Telemetry.TrackEvent("Get Scheduled Jobs By Microservice", microservice);

            var schedulesVM = InMemoryRepo.Where(kvp => kvp.Value.Microservice == microservice).Select(kvp => ScheduleVM.FactoryFrom(kvp.Value));

            return Response(schedulesVM);
        }

        public DomainResponse GetAllJobs()
        {
            Telemetry.TrackEvent("Get All Scheduled Jobs");

            var schedulesVM = InMemoryRepo.Select(kvp => ScheduleVM.FactoryFrom(kvp.Value));

            return Response(schedulesVM);
        }

        public string ListJobs()
        {
            Telemetry.TrackEvent("List All Scheduled Jobs");

            var schedulesVM = InMemoryRepo.Select(kvp => ScheduleVM.FactoryFrom(kvp.Value));

            StringBuilder builder = new();

            //Currently, only Brazilian timezone is considered 
            var localTimeZone = TimeZoneInfo.GetSystemTimeZones().Any(x => x.Id == "E. South America Standard Time") ?
                                                    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time") :
                                                    TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

            builder.AppendLine("SCHEDULER JOBS".PadLeft(83));
            builder.AppendLine();

            foreach (var statusCode in schedulesVM.Select(s => s.Status.Code).Distinct())
            {
                builder.AppendLine($"STATUS: {statusCode}");
                builder.Append("".PadRight(5));
                builder.AppendLine(new string('-', 145));
                builder.Append("".PadRight(5));
                builder.Append("Microservice".PadRight(20));
                builder.Append("Job".PadRight(55));
                builder.Append("Frequency".PadRight(20));
                builder.Append("Last success (UTC-3)".PadRight(25));
                builder.AppendLine("Next activation (UTC-3)".PadRight(25));
                builder.Append("".PadRight(5));
                builder.AppendLine(new string('-', 145));

                string lastMicroservice = string.Empty;
                bool firstLine = true;
                foreach (var schedule in schedulesVM.Where(s => s.Status.Code == statusCode)
                                                    .OrderBy(vm => $"{vm.Status.Order:00}{vm.Microservice,-20}{100-vm.Frequency.Order:00}{vm.Job}"))
                {
                    if (lastMicroservice != schedule.Microservice)
                    {
                        lastMicroservice = schedule.Microservice;
                        if (!firstLine)
                            builder.AppendLine();
                        builder.Append("".PadRight(5));
                        builder.Append(lastMicroservice.PadRight(20));
                    }
                    else
                    {
                        builder.Append("".PadRight(5));
                        builder.Append("".PadRight(20));
                    }

                    builder.Append(schedule.Job.PadRight(55));
                    builder.Append(schedule.Frequency.Code.PadRight(20));

                    if (schedule.LastAcknowledged > DateTime.MinValue)
                        builder.Append(TimeZoneInfo.ConvertTimeFromUtc(schedule.LastAcknowledged, localTimeZone).ToString("yyyy-MM-dd HH:mm:ss").PadRight(25));
                    else
                        builder.Append("".PadRight(25));

                    builder.Append(TimeZoneInfo.ConvertTimeFromUtc(schedule.NextActivation, localTimeZone).ToString("yyyy-MM-dd HH:mm:ss").PadRight(25));
                    builder.AppendLine();
                    firstLine = false;
                }
                builder.AppendLine();
                builder.AppendLine();
            }

            return builder.ToString();
        }

        public async Task<DomainResponse> RegisterJob(RegisterScheduleVM registerScheduleVM)
        {
            Telemetry.TrackEvent("Add or Update Schedule", $"{registerScheduleVM.Microservice}/{registerScheduleVM.Job}");

            // Checks if it 'already exists'
            var existingSchedule = InMemoryRepo.Where(kvp => kvp.Value.Microservice == registerScheduleVM.Microservice && kvp.Value.Job == registerScheduleVM.Job).FirstOrDefault().Value;

            // If so, preemptly reactivates it
            if (existingSchedule is not null)
            {
                existingSchedule.MapFrom(registerScheduleVM);
                existingSchedule.NextActivation = CalculateNextActivationTime(existingSchedule);
                existingSchedule.Status = LightJobStatus.Running.Code;
                existingSchedule.ActivationsSinceLastAcknowledge = 0;

                InMemoryRepo.TryAddOrUpdate(existingSchedule, true);
                await Repository.UpdateAsync(existingSchedule);


                var vm = ScheduleVM.FactoryFrom(existingSchedule);
                return Response(vm);
            }
            else
            {
                var scheduleToSave = Models.Schedule.FactoryFrom(registerScheduleVM);
                scheduleToSave.Id = $"{scheduleToSave.Microservice}-{scheduleToSave.Job}";

                // calculate first activation based on current time
                scheduleToSave.LastActivation = WorkBench.UtcNow;
                scheduleToSave.NextActivation = CalculateNextActivationTime(scheduleToSave);
                // then set last activation to invalid value
                scheduleToSave.LastActivation = DateTime.MinValue;

                scheduleToSave.Status = LightJobStatus.Running.Code;
                scheduleToSave.ActivationsSinceLastAcknowledge = 0;

                if (InMemoryRepo.TryAdd(scheduleToSave.Id, scheduleToSave))
                {
                    await Repository.UpdateAsync(scheduleToSave); // Actually does an upsert
                }
                else
                {
                    // Recovers from the lack of read lock at the 'already exists check' above
                    InMemoryRepo.TryAddOrUpdate(scheduleToSave, true);
                    await Repository.UpdateAsync(scheduleToSave);
                }

                return Response(ScheduleVM.FactoryFrom(scheduleToSave));
            }
        }

        public DomainResponse ReactivateJob(string microservice, string jobName)
        {
            Telemetry.TrackEvent("Reactivate Job", $"{microservice}/{jobName}");

            var scheduleToUpdate = InMemoryRepo.Where(kvp => kvp.Value.Status != LightJobStatus.Running.Code &&
                                                             kvp.Value.Microservice == microservice &&
                                                             kvp.Value.Job == jobName).FirstOrDefault().Value;

            if (scheduleToUpdate is null)
                return NoContent();

            scheduleToUpdate.Status = LightJobStatus.Running.Code;
            scheduleToUpdate.ActivationsSinceLastAcknowledge = 0;

            InMemoryRepo.TryAddOrUpdate(scheduleToUpdate);

            return Response(ScheduleVM.FactoryFrom(scheduleToUpdate));
        }

        public async Task<ScheduleVM> RemoveJobAsync(string microservice, string jobName)
        {
            Telemetry.TrackEvent("Remove Job", $"{microservice}/{jobName}");

            var toDelete = InMemoryRepo.Where(kvp => kvp.Value.Microservice == microservice &&
                                                     kvp.Value.Job == jobName).FirstOrDefault().Value;

            if (toDelete is null)
                return null;

            var updatedSchedule = await DeleteJobAsync(toDelete);

            return ScheduleVM.FactoryFrom(updatedSchedule);
        }

        public async Task<DomainResponse> DispatchJobs()
        {
            _ = FlushInMemoryRepoToDB();

            var now = WorkBench.UtcNow;
            var schedules = InMemoryRepo.Where(kvp => kvp.Value.Status == LightJobStatus.Running.Code &&
                                                      kvp.Value.NextActivation <= now.AddSeconds(1) &&
                                                      kvp.Value.NextActivation > DateTime.MinValue);

            var dispatchedJobs = new List<ScheduleVM>();

            foreach (var kvp in schedules)
            {
                Models.Schedule updated;
                ScheduleVM vm;
                if (kvp.Value.ActivationsSinceLastAcknowledge >= _MAX_UNHANDLED_ACTIVATIONS * kvp.Value.PartitionCount)
                {
                    var toDeactivate = kvp.Value;
                    Telemetry.TrackEvent("Deactivate Job", $"{toDeactivate.Microservice}/{toDeactivate.Job}");

                    updated = await UpdateJobAsDeactivatedAsync(toDeactivate);

                    vm = ScheduleVM.FactoryFrom(updated);
                    dispatchedJobs.Add(vm);

                    continue;
                }

                DateTime thisActivation = kvp.Value.NextActivation;
                kvp.Value.ActivationsSinceLastAcknowledge += kvp.Value.PartitionCount;
                kvp.Value.LastActivation = now;
                kvp.Value.NextActivation = CalculateNextActivationTime(kvp.Value);

                InMemoryRepo.TryAddOrUpdate(kvp.Value);

                //Defensively, ensures that InMemoryRepo persistence errors are not passed on to subscribers
                if (thisActivation > DateTime.MinValue.AddDays(1))
                {
                    vm = ScheduleVM.FactoryFrom(kvp.Value);
                    dispatchedJobs.Add(vm);

                    var message = FactoryLightMessage<JobDispatchMSG>(JobDispatchCMD.Trigger);
                    message.MapFrom(kvp.Value);
                    for (var i = 0; i < kvp.Value.PartitionCount; i++)
                    {
                        message.Partition = i + 1;
                        message.Activation = thisActivation;

                        WorkBench.ConsoleWriteLine($"{WorkBench.UtcNow} Sending message to {kvp.Value.Microservice}-{kvp.Value.Job} on partition {message.Partition}");
                        await schedulerMessageBus.SendDispatch(message);
                    }
                }
            }
            return Response(dispatchedJobs);
        }

        public async Task<DomainResponse> AbortJob(string microservice, string jobName)
        {
            Telemetry.TrackEvent("Abort Job Execution", $"{microservice}/{jobName}");

            var schedule = InMemoryRepo.Where(kvp => kvp.Value.Microservice == microservice && kvp.Value.Job == jobName).FirstOrDefault().Value;

            if (schedule is null)
                return NoContent();

            var message = FactoryLightMessage<JobDispatchMSG>(JobDispatchCMD.Abort);
            message.MapFrom(schedule);

            await new SchedulerMessageBus<ServiceBus>("TRANSACTIONAL").SendDispatch(message);

            schedule.Status = LightJobStatus.Aborted.Code;

            InMemoryRepo.TryAddOrUpdate(schedule);

            return Response(ScheduleVM.FactoryFrom(schedule));
        }

        public async Task<ScheduleVM> AcknowledgeActivationAsync(string microservice, string jobName)
        {
            Telemetry.TrackEvent("Acknowlege Job Activation", $"{microservice}/{jobName}");

            var schedule = InMemoryRepo.Where(kvp => kvp.Value.Microservice == microservice && kvp.Value.Job == jobName).FirstOrDefault().Value;

            if (schedule is null)
                return null;

            schedule.LastAcknowledged = WorkBench.UtcNow;

            if (schedule.ActivationsSinceLastAcknowledge > 0)
            {
                schedule.ActivationsSinceLastAcknowledge = 0;

                //Flushing by the acknowledge of the first partition of the job guarantees the same 
                //consistence for the job scheduling cycle as if it were entirely db persisted
                await Repository.UpdateAsync(schedule);
                InMemoryRepo.TryAddOrUpdate(schedule, true);
            }
            else
            {
                InMemoryRepo.TryAddOrUpdate(schedule, false);
            }

            return ScheduleVM.FactoryFrom(schedule);
        }

        private static async Task<Models.Schedule> UpdateJobAsDeactivatedAsync(Models.Schedule toDeactivate)
        {
            toDeactivate.Status = LightJobStatus.Deactivated.Code;

            await Repository.UpdateAsync(toDeactivate);
            InMemoryRepo.TryAddOrUpdate(toDeactivate);

            return toDeactivate;
        }

        private static async Task<Models.Schedule> DeleteJobAsync(Models.Schedule toDeactivate)
        {
            var deleted = await Repository.DeleteAsync<Models.Schedule>(toDeactivate.Id);
            InMemoryRepo.TryRemove(toDeactivate);

            return deleted;
        }

        private static async Task FlushInMemoryRepoToDB()
        {
            foreach (var unsaved in InMemoryRepo.GetNotFlushed())
                await Repository.UpdateAsync(unsaved);
        }

        private static DateTime CalculateNextActivationTime(Models.Schedule schedule)
        {
            var f = schedule.Frequency;

            int minutes = 0;
            if (f == LightJobFrequency.EveryMinute.Code)
                minutes = 1;
            else if (f == LightJobFrequency.EveryFiveMinutes.Code)
                minutes = 5;
            else if (f == LightJobFrequency.EveryTenMinutes.Code)
                minutes = 10;
            else if (f == LightJobFrequency.EveryFifteenMinutes.Code)
                minutes = 15;
            else if (f == LightJobFrequency.EveryThirtyMinutes.Code)
                minutes = 30;

            if (minutes != 0)
                // round to closest minute block
                return schedule.LastActivation.AddMinutes(minutes).RoundDown(TimeSpan.FromMinutes(minutes));

            int hours = 0;
            if (f == LightJobFrequency.Hourly.Code)
                hours = 1;
            else if (f == LightJobFrequency.Daily.Code)
                hours = 24;
            else if (f == LightJobFrequency.Weekly.Code)
                hours = 7 * 24;
            else if (f == LightJobFrequency.Monthly.Code)
                hours = 30 * 24;

            if (hours != 0)
                // round to nearest hour
                return schedule.LastActivation.AddHours(hours).RoundDown(TimeSpan.FromHours(1));

            // https://github.com/atifaziz/NCrontab
            string cronExpression;
            if (f == LightJobFrequency.HourlyAt.Code)
                cronExpression = $"{schedule.Minute?.ToString() ?? "*"} * * * *";
            else if (f == LightJobFrequency.DailyAt.Code)
                cronExpression = $"{schedule.Minute?.ToString() ?? "*"} {schedule.Hour?.ToString() ?? "*"} * * *";
            else if (f == LightJobFrequency.WeeklyAt.Code)
                cronExpression = $"{schedule.Minute?.ToString() ?? "*"} {schedule.Hour?.ToString() ?? "*"} * * {((int?)schedule.DayOfWeek)?.ToString() ?? "*"}";
            else if (f == LightJobFrequency.MonthlyAt.Code)
                cronExpression = $"{schedule.Minute?.ToString() ?? "*"} {schedule.Hour?.ToString() ?? "*"} {schedule.DayOfMonth?.ToString() ?? "*"} * {((int?)schedule.DayOfWeek)?.ToString() ?? "*"}";
            else
                throw new LightException($"{f} is not a valid LightJobFrequency code");

            return CrontabSchedule.Parse(
                cronExpression,
                new CrontabSchedule.ParseOptions
                {
                    IncludingSeconds = cronExpression.Split(' ').Length == 6
                }
            ).GetNextOccurrence(schedule.LastActivation);
        }
    }

    internal static class SchedulerExtensions
    {
        public static DateTime RoundDown(this DateTime dt, TimeSpan ts)
        {
            var remainder = dt.Ticks % ts.Ticks;
            if (remainder == 0)
            {
                return dt;
            }
            else
            {
                return dt.AddTicks(-remainder);
            }
        }

        public static bool TryAddOrUpdate(this ConcurrentDictionary<string, Models.Schedule> dictionary, Models.Schedule newSchedule, bool alreadyFlushed = false)
        {
            newSchedule.Flushed = alreadyFlushed;
            if (dictionary.TryGetValue(newSchedule.Id, out var oldSchedule))
            {
                return dictionary.TryUpdate(newSchedule.Id, newSchedule, oldSchedule);
            }
            else
            {
                return dictionary.TryAdd(newSchedule.Id, newSchedule);
            }
        }

        public static bool TryRemove(this ConcurrentDictionary<string, Models.Schedule> dictionary, Models.Schedule toRemove)
        {
            return dictionary.TryRemove(toRemove.Id, out var _);
        }

        public static List<Models.Schedule> GetNotFlushed(this ConcurrentDictionary<string, Models.Schedule> dictionary)
        {
            var notFlushedOnes = dictionary.Where(kvp => !kvp.Value.Flushed).Select(kvp => kvp.Value).ToList();

            foreach (var notFlushed in notFlushedOnes)
            {
                dictionary.TryAddOrUpdate(notFlushed, true);
            }

            return notFlushedOnes;
        }
    }
}