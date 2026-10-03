using Multron_Win_Cleaner;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MultronWinCleaner.Processes
{
    public class AutoClean
    {
        private readonly MainWindow window;
        private ulong prevIdleTime;
        private ulong prevKernelTime;
        private ulong prevUserTime;
        private DateTime lastRun;
        private DateTime? enabledSince;
        private DateTime? turnedOnAt;
        private bool? wasEnabled;
        private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5);
        private static string LastRunPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "autoclean_lastrun.txt");

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll")]
        static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

        [DllImport("kernel32.dll")]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        public AutoClean(MainWindow window)
        {
            this.window = window;
            lastRun = ReadLastRun();
            GetCpuUsage();
        }

        public async Task RunAsync()
        {
            while (true)
            {
                try
                {
                    await TickAsync();
                }
                catch (Exception ex)
                {
                    window.SetAutoCleanStatus("Auto Clean error: " + ex.Message);
                }
                await Task.Delay(TickInterval);
            }
        }

        private async Task TickAsync()
        {
            int cpu = GetCpuUsage();
            Settings settings = window.settings;
            bool enabled = settings != null && settings.chkAutoClean.IsChecked == true;
            if (enabled && wasEnabled != true)
                turnedOnAt = wasEnabled == false || lastRun == DateTime.MinValue ? DateTime.Now : null;
            wasEnabled = enabled;
            if (!enabled)
            {
                enabledSince = null;
                window.SetAutoCleanStatus(null);
                return;
            }
            enabledSince ??= DateTime.Now;

            if (window.autoclean == 1)
            {
                window.SetAutoCleanStatus("Auto Clean is running...");
                return;
            }

            DateTime now = DateTime.Now;
            int schedule = settings.cmbScheduleType.SelectedIndex;
            int interval = int.TryParse(settings.txtCleaningInterval.Text.Trim(), out int minutes) && minutes > 0 ? minutes : 60;
            TimeSpan start = ParseTime(settings.txtStartTime.Text, new TimeSpan(8, 0, 0));
            TimeSpan end = ParseTime(settings.txtEndTime.Text, new TimeSpan(22, 0, 0));
            bool[] days =
            {
                settings.Day1.IsChecked == true, settings.Day7.IsChecked == true, settings.Day6.IsChecked == true, settings.Day5.IsChecked == true,
                settings.Day4.IsChecked == true, settings.Day3.IsChecked == true, settings.Day2.IsChecked == true
            };

            bool limitWeeks = settings.chkLimitWeeks.IsChecked == true;
            bool[] weeks =
            {
                settings.Week1.IsChecked == true, settings.Week2.IsChecked == true, settings.Week3.IsChecked == true,
                settings.Week4.IsChecked == true, settings.Week5.IsChecked == true
            };
            Func<DateTime, bool> customDay = d => days[(int)d.DayOfWeek] && (!limitWeeks || weeks[(d.Day - 1) / 7]);

            DateTime reference = lastRun > enabledSince.Value ? lastRun : enabledSince.Value;
            DateTime next;
            switch (schedule)
            {
                case 0:
                    next = reference.AddMinutes(interval);
                    break;
                case 2:
                    next = lastRun == DateTime.MinValue ? now : lastRun.Date.AddDays(7) + start;
                    next = NextInRange(Later(next, FirstStartAfterTurnOn(start)), start, end, _ => true);
                    break;
                case 3:
                    next = NextInRange(reference.AddMinutes(interval), start, end, customDay);
                    break;
                default:
                    next = lastRun.Date >= now.Date ? now.Date.AddDays(1) + start : now;
                    next = NextInRange(Later(next, FirstStartAfterTurnOn(start)), start, end, _ => true);
                    break;
            }

            if (schedule == 3 && Array.TrueForAll(days, d => !d))
            {
                window.SetAutoCleanStatus("Auto Clean is on · no days selected");
                return;
            }
            if (schedule == 3 && limitWeeks && Array.TrueForAll(weeks, w => !w))
            {
                window.SetAutoCleanStatus("Auto Clean is on · no weeks selected");
                return;
            }
            if (now < next)
            {
                window.SetAutoCleanStatus("Auto Clean is on · next run " + FormatNext(next, now));
                return;
            }

            string blocked = CheckConditions(settings, cpu);
            if (blocked != null)
            {
                window.SetAutoCleanStatus("Auto Clean is on · waiting: " + blocked);
                return;
            }

            if (!await window.StartAutoCleanAsync())
            {
                window.SetAutoCleanStatus("Auto Clean is on · waiting for the current task to finish");
                return;
            }

            lastRun = now;
            SaveLastRun(now);
            window.SetAutoCleanStatus("Auto Clean is running...");
        }

        private string CheckConditions(Settings settings, int cpu)
        {
            SYSTEM_POWER_STATUS power = default;
            bool hasPower = GetSystemPowerStatus(out power);
            bool pluggedIn = !hasPower || power.ACLineStatus != 0;

            if (settings.SkipBattery.IsChecked == true && hasPower && power.BatteryLifePercent != 255 && power.BatteryLifePercent <= 30 && !pluggedIn)
                return "battery is at " + power.BatteryLifePercent + "%";
            if (settings.OnlyBattery.IsChecked == true && !pluggedIn)
                return "the device is not plugged in";
            if (settings.RunIfInactive.IsChecked == true && GetIdleMinutes() < 15)
                return "the PC is in use";
            if (settings.OnlyLowCPU.IsChecked == true && cpu > 30)
                return "CPU usage is " + cpu + "%";
            return null;
        }

        private DateTime FirstStartAfterTurnOn(TimeSpan start)
        {
            if (turnedOnAt == null)
                return DateTime.MinValue;
            DateTime on = turnedOnAt.Value;
            return on.TimeOfDay < start ? on.Date + start : on.Date.AddDays(1) + start;
        }

        private static DateTime Later(DateTime a, DateTime b) => a > b ? a : b;

        private static DateTime NextInRange(DateTime candidate, TimeSpan start, TimeSpan end, Func<DateTime, bool> dayAllowed)
        {
            for (int i = 0; i < 40; i++)
            {
                DateTime day = candidate.Date;
                if (dayAllowed(day))
                {
                    if (InRange(candidate.TimeOfDay, start, end))
                        return candidate;
                    if (start <= end && candidate.TimeOfDay < start)
                        return day + start;
                    if (start > end && candidate.TimeOfDay < start && candidate.TimeOfDay >= end)
                        return day + start;
                }
                candidate = day.AddDays(1) + (start <= end ? start : TimeSpan.Zero);
            }
            return candidate;
        }

        private static bool InRange(TimeSpan time, TimeSpan start, TimeSpan end)
        {
            if (start == end)
                return true;
            return start < end ? time >= start && time < end : time >= start || time < end;
        }

        private static TimeSpan ParseTime(string text, TimeSpan fallback)
        {
            return TimeSpan.TryParseExact((text ?? "").Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out TimeSpan value) && value < TimeSpan.FromDays(1)
                ? value
                : fallback;
        }

        private static string FormatNext(DateTime next, DateTime now)
        {
            if (next.Date == now.Date)
                return next.ToString("HH:mm");
            if (next.Date == now.Date.AddDays(1))
                return "tomorrow " + next.ToString("HH:mm");
            return next.ToString("ddd HH:mm", CultureInfo.InvariantCulture);
        }

        private static DateTime ReadLastRun()
        {
            try
            {
                if (File.Exists(LastRunPath) && DateTime.TryParse(File.ReadAllText(LastRunPath).Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime value))
                    return value;
            }
            catch (Exception) { }
            return DateTime.MinValue;
        }

        private static void SaveLastRun(DateTime value)
        {
            try
            {
                File.WriteAllText(LastRunPath, value.ToString("o", CultureInfo.InvariantCulture));
            }
            catch (Exception) { }
        }

        private static int GetIdleMinutes()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            if (!GetLastInputInfo(ref info))
                return int.MaxValue;
            uint idleMs = (uint)Environment.TickCount - info.dwTime;
            return (int)(idleMs / 1000 / 60);
        }

        private int GetCpuUsage()
        {
            if (!GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user))
                return 0;

            ulong idleTime = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime;
            ulong kernelTime = ((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime;
            ulong userTime = ((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime;

            ulong idleDiff = idleTime - prevIdleTime;
            ulong total = (kernelTime - prevKernelTime) + (userTime - prevUserTime);

            prevIdleTime = idleTime;
            prevKernelTime = kernelTime;
            prevUserTime = userTime;

            if (total == 0)
                return 0;
            return Math.Clamp((int)(100 - (idleDiff * 100 / total)), 0, 100);
        }
    }
}
