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
   <img width="788" height="563" alt="{B4478B4F-0660-4402-8F2F-405BB9D86FE5}" src="https://github.com/user-attachments/assets/4a481578-d6fc-4f78-9274-3318e007af77" />
   <img width="788" height="563" alt="{402C60EB-0D5B-4BE8-A976-6DC7FA25E028}" src="https://github.com/user-attachments/assets/6e37774e-0859-4fd6-ac06-36eb099ebbb6" />
   <img width="600" height="552" alt="{7547A368-FBB0-48B2-AB90-36582D181F31}" src="https://github.com/user-attachments/assets/52246d47-e11d-495f-8d6a-3b43763d5fc0" />
   

</div>

---

## Overview

**Multron Win Cleaner** is a comprehensive system maintenance, security and disk recovery suite engineered for Windows. Unlike conventional cleaning tools, it pairs deep low-level system controls (DISM, SFC, WinSxS, boot-time operations) with transparent, community-updatable cleaning definitions — and one scan checks junk files, system health, firewall rules, shortcuts, security settings and duplicate files together.

---

## Key Features

### 🧹 Deep System Cleanup
* **Application Rules:** Continuously updated definitions for third-party software, browser caches, and temporary data.
* **Modular Rule Database:** Cleanup targets are governed by a modular [`database.txt`](https://github.com/winball501/MultronWcleaner-Database) repository, allowing instant community contributions without altering core application binaries.
* **Detailed Cleaning Log:** Every location is shown live as `Cleaning:` and then `Cleaned:` with the number of deleted files, freed space, locked files and access-denied files, followed by a full summary.
* **Deep Log Tracing:** Configurable detection and wiping of `.log`, `.etl`, `.dmp`, `.tmp`, and `.bak` files across target directories.
* **Age-Based File Filters:** Target files strictly older than defined day thresholds based on file creation or last access timestamps.
* **Exceptions:** Exclude files and whole folders from scanning and cleaning.
* **Saved Selections:** Your ticked locations and commands are remembered, with a one-click **Reset Selections** button.
* **Native cleanmgr Access:** Windows Disk Cleanup presets run silently in the background during automatic cleaning.

### 🩺 System Health (DISM & SFC)
* **No Command Windows:** DISM and SFC run inside the app with live progress.
* **Result Panels:** Component store analysis, component store health and system file checks are shown as color-coded panels (green when healthy) with clear explanations.
* **Repair on Demand:** Tick **Run during cleaning** on a panel to clean up the component store (WinSxS), repair the Windows image (`/RestoreHealth`) or repair system files (`sfc /scannow`) as part of the clean.

### 🛡️ Security Check
* **Finds Insecure Windows Settings:** Checks more than 50 settings that weaken your PC — UAC or Windows Firewall turned off, no active antivirus, Defender disabled by policy or with whole drives excluded, SmartScreen off, **AutoPlay on for USB drives**, AutoRun, SMB 1.0, Remote Desktop without Network Level Authentication, disabled Windows Update, Guest account on, blocked Task Manager, sign-in screen backdoors, Office running all macros and more.
* **Risk Levels:** Every finding is marked HIGH, MEDIUM or LOW with an explanation of why it matters.
* **One-Click Fix & Undo:** Tick the findings and click **Fix Selected**. Previous values are backed up first, so every fix can be undone.
* Available in Utilities and as an option of the main scan, with a notification when an automatic scan finds high-risk settings.

### 🔎 One Scan, Every Check
* After the folder scan, **Firewall Rules**, **Shortcuts**, **Security Settings** and **Duplicate Files** run as steps of the scan, each with its own result panel above the scan results (green when nothing was found).
* While a step runs, the Clean button turns into **Cancel**, the progress is shown on the main screen, **Show Status** opens the tool, and **✕** skips the step.
* Nothing is changed automatically — remove rules, fix shortcuts, fix settings or review duplicates right from the panels.

### ⏰ Automatic Cleaning
* **Flexible Schedules:** Every X minutes, daily, weekly or custom days, with an allowed time range and optional **weeks of the month**.
* **Run Conditions:** Waits when the CPU is busy or the PC is in use, and never interrupts a scan or clean you started yourself.
* **Startup Cleaning:** Scan or scan and clean when Windows starts.
* **Status at a Glance:** Green status lines on the main window show when automatic or startup cleaning is on and when the next run is.
* **Notifications:** Bottom-right notifications for finished automatic runs, duplicate files and security warnings.

### 🔒 Locked File & Process Management
* **Real-Time Lock Detection:** Surfaces file locks dynamically, displaying the active locking Process Name, Process ID (PID), and absolute file path.
* **Force Unlock & Delete:** Kills only the processes that hold the file (system processes are protected) and retries the deletion.
* **Boot Operations Manager:** Schedules pending file removal and modification routines to execute cleanly during the next Windows reboot cycle.

### ⚙️ System & Startup Control
* **Startup Manager:** Inspects and toggles boot entries across Registry keys, Task Scheduler, and Winlogon Userinit targets.
* **Startup Sentinel:** Delivers real-time notifications whenever a newly installed application registers a boot entry.
* **Firewall Rule Cleaner:** Scans for and removes broken Windows Firewall rules that point to programs that no longer exist.
* **Shortcut Fixer:** Finds Desktop and Start Menu shortcuts to missing programs, repairs the ones whose program moved to a new version folder and moves the others to the Recycle Bin.
* **System Optimization:** Two profiles — **For old systems** pauses selected services, **For new systems** applies performance settings (power plan, Game Mode, priorities, faster menus and more). Settings are re-applied at startup, every action is listed in an Activity log, and **Undo** restores your previous values.

### 📊 Memory & Performance Tracking
* **RAM Optimizer:** Manual or interval-based memory working set reductions.
* **Real-Time Resource Monitor:** Continuous visual tracking for active CPU and memory utilization.

### 🔍 Storage Utilities
* **Duplicate File Finder:** Byte-by-byte verified, A **side-by-side view** shows each copy next to its original, 40 quick-select file types and presets for images, videos, music, documents, archives and installers, and a safe delete that always keeps one copy.
* **Large File Finder:** Scans drive structures to isolate oversized media, archives, installers, disk images and virtual machines, backups, dumps and logs.
* **Pre-Deletion Audit:** Comprehensive summary table displaying each file's size, creation date, modification date, and age in days prior to execution.

---

## System Requirements

* **OS:** Windows 10 / Windows 11 (64-bit recommended)
* **Privileges:** Administrator permissions (required for DISM/SFC operations, security fixes, boot-time actions, and Registry access)
* **Runtimes:** Microsoft .NET 8 Desktop Runtime

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
