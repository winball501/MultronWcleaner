<div align="center">

  <img src="MultronWinCleaner/Assets/mwc_logo.png" alt="Multron Win Cleaner Logo" width="130" />

  # Multron Win Cleaner

  **Modern, lightweight, and community-driven Windows system optimizer.**  
  Completely free, open-source, and free of subscriptions, paywalls, or background telemetry.

  <p align="center">
    <a href="https://github.com/winball501/MultronWcleaner/releases">
      <img src="https://img.shields.io/github/v/release/winball501/MultronWcleaner?style=for-the-badge&color=0e7490&label=Release" alt="Release" />
    </a>
    <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078d4?style=for-the-badge&logo=windows&logoColor=white" alt="Platform" />
    <img src="https://img.shields.io/badge/Stack-C%23%20%7C%20WPF-512bd4?style=for-the-badge&logo=dotnet&logoColor=white" alt="Stack" />
    <a href="https://github.com/winball501/MultronWcleaner-Database">
      <img src="https://img.shields.io/badge/Database-Community%20Driven-059669?style=for-the-badge&logo=github&logoColor=white" alt="Database Repo" />
    </a>
    <img src="https://img.shields.io/github/license/winball501/MultronWcleaner?style=for-the-badge&color=15803d" alt="License" />
    <a href="https://github.com/winball501/MultronWcleaner/actions/workflows/build.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/winball501/MultronWcleaner/build.yml?branch=beta&style=for-the-badge&logo=githubactions&logoColor=white" alt="Build Status"/>
  </a>
  </p>

  [Download Release](https://github.com/winball501/MultronWcleaner/releases/latest) • [Database Repo](https://github.com/winball501/MultronWcleaner-Database) • [Report Issue](https://github.com/winball501/MultronWcleaner/issues)

</div>

---

## Previews

<div align="center">
 <img width="788" height="563" alt="{2A6FD3AA-AA81-4414-87B3-2C2AC09A897D}" src="https://github.com/user-attachments/assets/760ce3c4-c907-48c5-947c-c07d8e791cfd" />
 <img width="788" height="563" alt="{BC7D7B0F-1472-420B-AA6E-C8707D5E47E7}" src="https://github.com/user-attachments/assets/9304bafa-a0e4-477f-bfb2-2fde6c995a42" />
</div>

---

## Overview

**Multron Win Cleaner** is a comprehensive system maintenance and disk recovery suite engineered for Windows. Unlike conventional cleaning tools, it pairs deep low-level system controls (DISM, WinSxS, boot-time operations) with transparent, community-updatable cleaning definitions.

---

## Key Features

### 🧹 Deep System Cleanup
* **250+ Application Rules:** Continuously updated definitions for third-party software, browser caches, and temporary data.
* **Modular Rule Database:** Cleanup targets are governed by a modular [`database.txt`](https://github.com/winball501/MultronWcleaner-Database) repository, allowing instant community contributions without altering core application binaries.
* **WinSxS Optimization:** Integrated component store analysis and image reduction utilizing native `DISM.exe` commands.
* **Deep Log Tracing:** Configurable detection and wiping of `.log`, `.etl`, `.dmp`, `.tmp`, and `.bak` files across target directories.
* **Age-Based File Filters:** Target files strictly older than defined day thresholds based on file creation or last access timestamps.
* **Auto Cleaner:** Configurable background routines to schedule automatic cleanup intervals (hourly, daily, custom).
* **Native cleanmgr Access:** Integrated launch shortcuts to Windows Disk Cleanup presets.

### 🔒 Locked File & Process Management
* **Real-Time Lock Detection:** Surfaces file locks dynamically, displaying the active locking Process Name, Process ID (PID), and absolute file path.
* **Force Unlock & Delete:** Inline retry mechanisms or administrative force-kill routines to dispose of locked traces.
* **Boot Operations Manager:** Schedules pending file removal and modification routines to execute cleanly during the next Windows reboot cycle.

### ⚙️ System & Startup Control
* **Startup Manager:** Inspects and toggles boot entries across Registry keys, Task Scheduler, and Winlogon Userinit targets.
* **Startup Sentinel:** Delivers real-time notifications whenever a newly installed application registers a boot entry.
* **Firewall Rule Hygiene:** Scans for and prunes broken, orphaned, or obsolete Windows Firewall rules.
* **Legacy System Optimization:** One-click resource tuning profiles tailored to accelerate low-spec or older machines.

### 📊 Memory & Performance Tracking
* **RAM Optimizer:** Manual or interval-based memory working set reductions.
* **Real-Time Resource Monitor:** Continuous visual tracking for active CPU and memory utilization.

### 🔍 Storage Utilities
* **Duplicate File Finder:** Fast byte-level comparison scanner to identify and eliminate duplicate copies.
* **Large File Finder:** Scans drive structures to isolate overgrown archives, installers, and oversized media.
* **Pre-Deletion Audit:** Comprehensive summary table displaying each file's size, creation date, modification date, and age in days prior to execution.

---

## System Requirements

* **OS:** Windows 10 / Windows 11 (64-bit recommended)
* **Privileges:** Administrator permissions (required for DISM operations, boot-time actions, and Registry access)
* **Runtimes:** Microsoft .NET Framework / Desktop Runtime

---

## Installation

1. Navigate to the [Releases](https://github.com/winball501/MultronWcleaner/releases) section.
2. Download the latest `MultronWinCleaner.exe` executable or installer archive.
3. Right-click the application and select **Run as administrator**.

---

## Community Database Contributions

The cleaning rules are kept outside the main codebase to allow rapid additions without needing a new version release. You can inspect or extend the rule definitions directly at the [MultronWcleaner-Database](https://github.com/winball501/MultronWcleaner-Database) repository.

Pull Requests adding support for new applications or custom directory paths are always reviewed and merged there.

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for complete details.
