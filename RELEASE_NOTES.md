## Multron Win Cleaner 1.26.1 Update

> [!WARNING]
> **Antivirus false positives:** some antivirus programs may flag Multron Win Cleaner or one of its DLL files as suspicious when you download or run it. In particular, **Bitdefender** and engines that use it (for example Emsisoft, eScan, GData, Arcabit, VIPRE) may detect `MultronWinCleaner.dll` **heuristically as ransomware**, with a name such as `Gen:Heur.Ransom.Imps.3`. This is a false positive: a heuristic detection is based on behavior, not on a known malware signature, and it is triggered by what a system cleaner has to do (closing programs, deleting files, changing Windows settings). Multron Win Cleaner does not encrypt files, does not ask for a ransom and does not send your data anywhere except the optional cloud malware scan. The file has been reported to Bitdefender as a false positive again. The full source code is in this repository, and every release is built automatically by GitHub Actions. If your antivirus blocks the app, you can report the file to its vendor as a false positive or add an exception for the app folder.
>
> * Bitdefender false positive form: <https://www.bitdefender.com/consumer/support/answer/29358/>
> * Microsoft Defender file submission: <https://www.microsoft.com/en-us/wdsi/filesubmission>

> **Test release:** the cloud malware scan is published for testing. It is released so it can be tried thoroughly and made stable, so results and behavior may still change. Please report false detections, missed threats or any problems in [Issues](https://github.com/winball501/MultronWcleaner/issues).

### Malware scan: removal progress
- **Detailed progress while threats are removed:** Remove All Threats, Remove Selected, Delete File, Quarantine and the removal at the end of a clean now open a progress window instead of only showing a wait cursor.
- It shows which file is being handled (for example 2 / 5), a progress bar, and the step that is running right now: looking for programs started from the file, deleting, removing read-only attributes, closing programs that lock the file, taking ownership, renaming, or scheduling deletion at the next restart.
- Counters for **Removed / Quarantined**, **At Restart**, **Failed** and the elapsed time, and a **Details** list with every step and the result of each file in color.
- **Stop** ends the removal after the current file; files that were already removed stay removed.
- **Copy Details** copies the list, and **Open Log** opens the log of the removal.
- When some files can only be deleted at the next restart, the window says so.

### Malware scan: logs
- **Every scan writes a log:** scan type, where it was started from, the scan settings, how many files were found, connection drops and reconnects, every threat and suspicious file (with threat name, SHA-256 and size), files that could not be checked or were skipped, and a summary with the counts and the duration. A stopped or failed scan is logged too.
- Removals, quarantine actions, restores from quarantine and rescans are added to the log of the scan they belong to, step by step.
- **Scan Logs (new window):** the new **Scan Logs** button in the Malware Scan window lists the logs with their date, type and result and shows the selected log next to the list. Logs can be opened in Notepad, copied, deleted one by one or all at once, and the logs folder can be opened.
- Logs are kept in the `Logs\MalwareScan` folder of the app, and only the latest 100 are kept.

### Malware scan: quarantine in its own window
- **Select all and quarantine all:** a checkbox in the header of the results list checks or unchecks every file in the list. The new **Quarantine All / Quarantine Selected** button next to Remove All Threats moves the checked files to quarantine, or every threat in the list when nothing is checked, with the same progress window as removal.
- The quarantine list moved out of the Malware Scan window into its own **Quarantine** window, so the Malware Scan window stays tidy. It can restore or delete files one by one, delete all of them, and open the quarantine folder.
- **Compressed and encrypted quarantine:** quarantined files are now compressed and then encrypted with AES-256 (each file with its own random key). An encrypted file cannot run and is not detected again by antivirus programs, and it takes much less space. The Quarantine window shows each file's original and stored size. Before a file is restored, it is decrypted and checked against the SHA-256 saved when it was quarantined, so a damaged file is never written back. Files that were already in quarantine are compressed and encrypted automatically the next time the app starts.
- **Export:** quarantined files can be exported one by one or all at once to a ZIP protected with the password `infected` (the usual password for malware samples), together with a text file that lists each file's original path, threat, SHA-256 and date, for example to send them to an antivirus vendor. Scan logs can be exported one by one as a text file or all at once as a ZIP.

### Main window status
- **Tool progress on the main screen:** while a malware scan, Duplicate File Finder, Large File Finder, Firewall Rule Cleaner, Shortcut Fixer or Security Check runs from its own window, the status text under the big button on the main screen shows what it is doing (for example the number of files checked and threats found), and the ring shows the progress when the tool reports it. When the tool finishes, its result stays there. A scan or clean started on the main screen always has priority.
- **Smoother spinner:** the spinning ring around the shield in the malware scan and removal windows now turns exactly around the shield instead of wobbling.

### Tougher threat removal (Force Delete)
- **Locked and protected threats are removed with SYSTEM privileges:** for any threat or stubborn file that cannot be deleted or quarantined the normal way, Multron Win Cleaner now escalates step by step — closing the programs that lock it, taking ownership, resetting permissions and, as a last resort, deleting or moving it as `NT AUTHORITY\SYSTEM` using Sysinternals PsExec. This applies to every detection, not to any one program.
- **PsExec is now built into the app:** `PsExec64.exe` is embedded in Multron Win Cleaner and extracted automatically when it is needed, so the feature works on any machine without a separate download or setup. Each removal step is shown in the removal progress window.
- **Remove at next restart:** when a threat still cannot be removed while Windows is running, it is now registered to be deleted (or moved to quarantine) at the next restart — tried several ways (standard Windows API, direct registry, and with SYSTEM privileges) and verified. The file is then removed very early during the next boot, before background programs start, which clears many files that are locked or in use.
- **Clearer reasons when a threat cannot be removed:** the steps now show exactly why a SYSTEM removal failed (for example PsExec could not start, or access was denied). Some security programs actively protect their own files and block deletion, renaming and even scheduling at restart, even for SYSTEM; in that case the app says so and explains that the protecting program's self-protection must be turned off, or the file removed in Windows Safe Mode.
- PsExec is a Microsoft Sysinternals tool; its license is included as [PSEXEC-EULA.md](PSEXEC-EULA.md) and linked from the README.

### Fixes
- A new scan, a rescan or Clear List can no longer start while threats are being removed, which could mix up the results list.
- Several Malware Scan and quarantine texts that were shown in English are now translated into all 12 languages.

For everything else that is new in 1.26, see the [v1.26 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26).
