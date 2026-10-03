## Multron Win Cleaner v1.25 beta

### What's new
- **Automatic cleaning works.** The automatic cleaning settings never took effect before. Every X minutes, Daily, Weekly and Custom Days schedules, the allowed time range and the run conditions now work.
- **Choose weeks of the month.** Custom Days has a new optional "Only in selected weeks of the month" setting, for example to clean only in the 1st and 3rd week.
- **Green status lines on the main window** show when automatic cleaning or startup cleaning is on, when the next automatic run is, and why it is waiting (for example high CPU usage or the PC being in use).

### Improvements
- Killing locked files is much faster. Files that Windows does not let you delete are shown as "access denied" right away instead of being sent to the Locked Files list.
- Automatic cleaning never interrupts a scan or clean you started yourself, and turning it on does not start a clean right away.
- The Locked Files selection window closes automatically when you start Kill, so you can follow the progress.

### Fixes
- Fixed a crash while killing locked files.
- The days ticked for Custom Days are kept after restarting the app.
- "Automatically clean when the app starts" now works without also ticking "Automatically scan when the app starts".
- The After cleaning action (Shutdown, Restart, Log Off) runs only after automatic or startup cleaning, not after a clean you start yourself.
- "Device is plugged in and charging" was reversed, and "Battery is above 30%" stopped automatic cleaning on desktops without a battery. Both now work as described.
- The cleaning notification follows its own setting, and notifications no longer appear when they are turned off.
- Unticking a file in the scan results no longer adds a wrong line to Exceptions. Use the new "Always skip this file (add to Exceptions)" right-click option instead. Folders can now be added to Exceptions too.
- Your cleanmgr.exe, Dism.exe, SFC and Deep Log Files Scan choices are remembered after restarting the app.
- New Reset Selections button puts every choice back to its default.

### Duplicate File Finder
- **Only real duplicates.** Every duplicate is now checked byte by byte, so only files with exactly the same content are listed.
- **New side-by-side view** shows each copy next to the original it was copied from, with an arrow between them. It is the default view; the folder view is one click away.
- A file reached through a folder link is no longer listed as a copy of itself, and duplicates across different target folders are now found.
- "Match media across different qualities" matched photos and videos by name only and could select a different file for deletion. It is now off by default, its results are marked SIMILAR and they are never selected automatically.
- Before deleting, each file is compared again with the copy that is kept, and at least one copy is always kept.
- **New Duplicate Files option on the main screen** (on by default). After a scan, duplicate files in your personal folders are found in the background and shown as a warning above the results. Show Files opens Duplicate File Finder. After automatic or startup scans, a notification appears in the bottom-right corner instead. Nothing is deleted automatically.
- The duplicate search is now a step of the scan, like the malware scan. While it runs, the Clean button turns into Cancel, the progress is shown on the main screen, **Show Status** opens Duplicate File Finder to watch it and **✕** stops it.
- **More file types:** 40 quick-select extensions instead of 8 (27 common ones, such as .mp3, .zip and .iso, are selected by default; .exe is not), and new Music, Documents, Archives and Programs presets. The Images and Videos presets include many more formats, such as .heic, camera RAW and .m2ts.

### Large File Finder
- More extensions in every category, and three new categories: **Programs and Installers**, **Disk Images and VMs** (.iso, .vhdx, .vmdk...) and **Backups, Dumps and Logs** (.bak, .dmp, .log...).

### DISM, SFC and cleanmgr
- **No more command windows.** DISM and SFC repairs run inside the app, with their progress and result shown in the cleaning list and the round progress bar.
- **DISM and SFC results at the top of the scan results**: component store size and reclaimable space, component store health and system file integrity. Green means everything is fine. Each has a checkbox to choose whether it runs during cleaning.
- The DISM and SFC results are read correctly on Turkish Windows, and the progress bar no longer freezes while `sfc /verifyonly` runs.
- During automatic and startup cleaning, cleanmgr runs silently in the background.

### New tools
- **Shortcut Fixer** in Utilities and on the main screen finds Desktop and Start Menu shortcuts whose program no longer exists. Shortcuts to programs that moved to a new version folder are repaired, the others are moved to the Recycle Bin.
- Shortcut Fixer has a Reset button, like Firewall Rule Cleaner.
- The System Optimization card shows an Activity list with everything it did and when. Undo clears it.
- While an optimization is on, it is applied again every time the app starts (paused services that Windows started again, settings that were changed back, DNS flush). After applying, the app offers to add itself to Windows startup so this also happens after every restart.
- **System Optimization** now has two profiles. **For old systems** pauses only the services you select and Undo starts only those again. **For new systems** applies performance settings (power plan, Game Mode, game priority, faster menus and more), can turn on automatic memory cleaning permanently, flushes the DNS cache every time the app starts while it is on, and Undo restores your previous values.

### Security Check (new)
- **New Security Check** in Utilities and as a scan option on the main screen. It finds more than 50 Windows settings that make your PC less secure, for example UAC or Windows Firewall turned off, AutoPlay on for USB drives, Defender turned off by a policy or with whole drives excluded, SmartScreen off, SMB 1.0, Remote Desktop without protection, Windows Update disabled, the Guest account on, Task Manager blocked, backdoors on the sign-in screen and Office running all macros.
- Each finding shows a risk level and an explanation. Tick the ones to fix and click **Fix Selected**. The previous values are saved first, so **Undo Fixes** can restore them.
- After automatic or startup scans, a notification appears when high risk settings are found.

### Main screen
- Firewall Rules, Duplicate Files and Shortcuts results are green when nothing was found.
- The option texts change color right away when switching between dark and light mode.

### Scan and Clean
- The Scan button now turns into Cancel as soon as the scan starts, so a running scan can be cancelled. Before, it stayed as Scan and clicking it again started a second scan.
- While a scan or clean runs, the buttons of Duplicate File Finder, Firewall Rule Cleaner and Shortcut Fixer turn into Cancel, so you can stop the scan or clean from there too.
- Cancel also works while the scan results are being loaded. Before, the button turned back into Scan too early, and clicking it started a second scan.
- Firewall Rules, Shortcuts, Security Settings and Duplicate Files are now steps of the scan, like the malware scan. While each step runs, the Clean button turns into Cancel, the progress is shown on the main screen, **Show Status** opens the tool and **✕** skips the step.
- Opening Duplicate File Finder during its search no longer shows the old results and a Clean button.
- After Kill, the button no longer stays as Cancel, and Kill no longer stops at once after a cancelled clean.
