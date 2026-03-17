using Multron_Win_Cleaner;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using static MultronWinCleaner.Processes.Scan;

namespace MultronWinCleaner.Processes
{
    public class AutoClean
    {
        MainWindow window;
        Settings settings;
        private ulong prevIdleTime = 0;
        private ulong prevKernelTime = 0;
        private ulong prevUserTime = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

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
        public AutoClean(MainWindow window, Settings settings)
        {
              this.window = window;
              this.settings = settings;
        }
        public string stringtokinizer(string text, string token, int range)
        {
            string[] words = text.Split(token);
            string output = "";
            int finded = 0;
            foreach (string word in words)
            {
                if(word == token)
                {
                    if (finded < range)
                    {
                        range++;
                        output = "";
                    } else
                    {
                        break;
                    }
                } else
                {
                    output += word;

                }
            }
            return output;
        }
        private int GetIdleMinutes()
        {
            LASTINPUTINFO info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);

            GetLastInputInfo(ref info);

            uint idleMs = (uint)Environment.TickCount - info.dwTime;
            return (int)(idleMs / 1000 / 60);  
        }
        private int GetBatteryPercent()
        {
            GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

            if (status.BatteryLifePercent == 255) return -1;  
            return status.BatteryLifePercent;
        }

        private bool IsOnAC()
        {
            GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);
            return status.ACLineStatus == 1; 
        }
        private int GetCpuUsage()
        {
            GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

            ulong idleTime = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime;
            ulong kernelTime = ((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime;
            ulong userTime = ((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime;

            ulong idleDiff = idleTime - prevIdleTime;
            ulong kernelDiff = kernelTime - prevKernelTime;
            ulong userDiff = userTime - prevUserTime;

            prevIdleTime = idleTime;
            prevKernelTime = kernelTime;
            prevUserTime = userTime;

            ulong total = kernelDiff + userDiff;
            if (total == 0) return 0;
             
            int cpu = (int)(100 - (idleDiff * 100 / total));
            return Math.Clamp(cpu, 0, 100);
        }
        public bool time_equalazer()
        {
            string shh = DateTime.Now.ToString("HH:mm");
            string hour = stringtokinizer(shh, ":", 0);
            string min = stringtokinizer(shh, ":", 1);
            string setstartm = stringtokinizer(settings.txtStartTime.Text, ":", 1);
            string setendm = stringtokinizer(settings.txtEndTime.Text, ":", 1);
            string setstarth = stringtokinizer(settings.txtStartTime.Text, ":", 0);
            string setendh = stringtokinizer(settings.txtEndTime.Text, ":", 0);
            if(int.Parse(setstartm) < int.Parse(min) || int.Parse(setstarth) < int.Parse(hour))
            {
                return true;
            }
            return false;
        }
        public bool checkconfigurations()
        {
            if (settings.OnlyLowCPU.IsChecked == true)
            {
                if (GetCpuUsage() > 30)
                {
                    return true;
                }
            }
            if (settings.OnlyBattery.IsChecked == true)
            {
                if (IsOnAC())
                {
                    return true;
                }
            }
            if (settings.RunIfInactive.IsChecked == true)
            {
                if (GetIdleMinutes() < 15)
                {
                    return true;
                }
            }
            if (settings.SkipBattery.IsChecked == true)
            {
                if (GetBatteryPercent() < 30)
                {
                    return true;
                }
            }
            return false;
        }
        public async Task run()
        {
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            while (true)
            {
         
             
                switch (settings.cmbScheduleType.SelectedIndex)
                {
                    case 0:
                        if (minutes == int.Parse(settings.txtCleaningInterval.Text))
                        {
                           if(window.onclean == 0 && window.autoclean == 0)
                            {
                               if(!checkconfigurations())
                                await window.startscan();
                            }
                        } else
                        {
                            if (seconds == 60)
                            {
                                minutes++;
                                seconds = 0;
                            }
                            else
                            {
                                seconds++;
                            }
                        }
                       
                        break;
                    case 1:
                        if(hours == 24)
                        {
                            if(time_equalazer())
                                if (!checkconfigurations())
                                    await window.startscan();
                            hours = 0;
                            minutes = 0;
                            seconds = 0;

                        } else if(minutes == 60)
                        {
                            hours++;
                            minutes = 0;
                            seconds = 0;
                         
                        } else if(seconds == 60)
                        {
                            minutes++;
                            seconds = 0;
                        } else
                        {
                            seconds++;
                        }
                        break;

                    case 2:
                        if (hours == 168)
                        {
                            if (time_equalazer())
                                if (!checkconfigurations())
                                    await window.startscan();
                            hours = 0;
                            minutes = 0;
                            seconds = 0;

                        }
                        else if (minutes == 60)
                        {
                            hours++;
                            minutes = 0;
                            seconds = 0;

                        }
                        else if (seconds == 60)
                        {
                            minutes++;
                            seconds = 0;
                        }
                        else
                        {
                            seconds++;
                        }
                        break;
                    case 3:
                        if (minutes == int.Parse(settings.txtCleaningInterval.Text))
                        {
                            switch (DateTime.Now.DayOfWeek)
                            {
                                case DayOfWeek.Monday:
                                     
                                    if (settings.Day7.IsChecked == true)
                                    {
                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }
                                    }
                                    break;
                                case DayOfWeek.Tuesday:
                                    if (settings.Day6.IsChecked == true)
                                    {

                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }
                                    }
                                    break;
                                case DayOfWeek.Wednesday:
                                    if (settings.Day5.IsChecked == true)
                                    {
                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }

                                    }
                                    break;

                                case DayOfWeek.Thursday:
                                    if (settings.Day4.IsChecked == true)
                                    {
                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }
                                    }
                                    break;
                                case DayOfWeek.Friday:
                                    if (settings.Day3.IsChecked == true)
                                    {
                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }

                                    }
                                    break;

                                case DayOfWeek.Saturday:
                                    if (settings.Day2.IsChecked == true)
                                    {
                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }
                                    }
                                    break;
                                case DayOfWeek.Sunday:

                                    if (settings.Day1.IsChecked == true)
                                    {

                                        if (time_equalazer() && window.onclean == 0 && window.autoclean == 0)
                                        {
                                            if (!checkconfigurations())
                                                await window.startscan();
                                        }
                                    }
                                    break;
                            }
                            minutes = 0;
                        }
                        else
                        {
                            if (seconds == 60)
                            {
                                minutes++;
                                seconds = 0;
                            }
                            else
                            {
                                seconds++;
                            }
                        }

                        break;
                } 
                await Task.Delay(1000);
            }
        
          
        }

    }
}
