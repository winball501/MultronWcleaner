## Multron Win Cleaner 1.26.2 Update

> [!WARNING]
> **Antivirus false positives:** some antivirus programs may flag Multron Win Cleaner or one of its DLL files as suspicious when you download or run it. In particular, **Bitdefender** and engines that use it (for example Emsisoft, eScan, GData, Arcabit, VIPRE) may detect `MultronWinCleaner.dll` **heuristically as ransomware**, with a name such as `Gen:Heur.Ransom.Imps.3`. This is a false positive: a heuristic detection is based on behavior, not on a known malware signature, and it is triggered by what a system cleaner has to do (closing programs, deleting files, changing Windows settings). Multron Win Cleaner does not encrypt files, does not ask for a ransom and does not send your data anywhere except the optional cloud malware scan. The file has been reported to Bitdefender as a false positive again. The full source code is in this repository, and every release is built automatically by GitHub Actions. If your antivirus blocks the app, you can report the file to its vendor as a false positive or add an exception for the app folder.
>
> * Bitdefender false positive form: <https://www.bitdefender.com/consumer/support/answer/29358/>
> * Microsoft Defender file submission: <https://www.microsoft.com/en-us/wdsi/filesubmission>

> **Test release:** the cloud malware scan is published for testing. It is released so it can be tried thoroughly and made stable, so results and behavior may still change. Please report false detections, missed threats or any problems in [Issues](https://github.com/winball501/MultronWcleaner/issues).

### Cloud malware scan: license agreement
The cloud malware scan agreement was updated. Read the full agreement here: [MALWARE_SCAN_EULA.md](https://github.com/winball501/MultronWcleaner/blob/beta/MALWARE_SCAN_EULA.md). The app shows it before your next cloud scan and asks you to accept the current version.

### Fixes
- The cloud malware scan agreement text can no longer be out of date in an installed build: it is always the version published in this repository.

For the full list of what is new in 1.26, see the [v1.26.1 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26.1) and the [v1.26 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26).
