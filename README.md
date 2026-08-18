# Multron Win Cleaner

Free, open-source Windows system cleaner and optimizer. No subscriptions, no locked features.

## Features

**System Cleanup**
- Scan and remove junk files from 253+ supported applications (the database.txt file lives in a separate repo and can be extended by the community via commits/PRs)
- WinSxS component store analysis and cleanup using DISM
- Custom folder cleanup via configurable `database.txt`, open for community contributions
- Deep log file scanner (`.log`, `.etl`, `.dmp`, `.tmp`, `.bak` and more and you can add custom extensions)
- File age filtering — scan by creation date or last access date with preset or custom day thresholds, disabled by default
- Auto Cleaner — schedule automatic cleaning at custom intervals (hourly, daily, etc.), disabled by default

**File Tools**
- Duplicate File Finder — find and remove duplicate files
- Large File Finder — identify oversized files consuming disk space

**Memory**
- Memory Cleaner — free up RAM manually with one click or automatically at set intervals
- Memory Monitor — live RAM and CPU usage tracking

**System Management**
- Startup Manager — control which programs launch at boot, add new startup entries to Registry, Task Scheduler, WinLogon Userİnit
- Startup Notification — alerts when new startup items are added
- Boot Operations Manager — handle locked files at next reboot, add custom file actions
- Invalid Firewall Rule Cleaner — remove broken or outdated firewall rules
- Optimization for Old Systems — one-click performance optimization

**Locked File Detection**
During cleanup, files locked by other processes are detected in real time. The system identifies the locking process (name and PID), displays the full file path, and lets you retry or force-delete after killing the process. Files where the locking process can't be identified are shown as "Unknown Process" so nothing is hidden.

**Scan Details**
Scan results display each file's creation date, age in days, and last modified date alongside file size — giving full visibility before cleanup.

## Requirements

- Windows 10/11
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Download

- [Multron Win Cleaner](https://github.com/winball501/MultronWcleaner/releases)
- [Database](https://github.com/winball501/MultronWcleaner-Database/releases)


## Screenshots
<img width="1350" height="1050" alt="image" src="https://github.com/user-attachments/assets/e76d5348-93e9-4d68-8484-55c56544c883" />
<img width="1350" height="1050" alt="image" src="https://github.com/user-attachments/assets/88744868-f5aa-4e70-b205-b96aa813ae72" />


## Contributing

Contributions and feedback are welcome. Fork it, improve it, open a PR.
