## Multron Win Cleaner 1.26.3 Update

> [!WARNING]
> **Antivirus false positives:** some antivirus programs may flag Multron Win Cleaner or one of its DLL files as suspicious when you download or run it. In particular, **Bitdefender** and engines that use it (for example Emsisoft, eScan, GData, Arcabit, VIPRE) may detect `MultronWinCleaner.dll` **heuristically as ransomware**, with a name such as `Gen:Heur.Ransom.Imps.3`. This is a false positive: a heuristic detection is based on behavior, not on a known malware signature, and it is triggered by what a system cleaner has to do (closing programs, deleting files, changing Windows settings). Multron Win Cleaner does not encrypt files, does not ask for a ransom and does not send your data anywhere except the optional cloud malware scan. The file has been reported to Bitdefender as a false positive again. The full source code is in this repository, and every release is built automatically by GitHub Actions. If your antivirus blocks the app, you can report the file to its vendor as a false positive or add an exception for the app folder.
>
> * Bitdefender false positive form: <https://www.bitdefender.com/consumer/support/answer/29358/>
> * Microsoft Defender file submission: <https://www.microsoft.com/en-us/wdsi/filesubmission>

> **Test release:** the cloud malware scan is published for testing. It is released so it can be tried thoroughly and made stable, so results and behavior may still change. Please report false detections, missed threats or any problems in [Issues](https://github.com/winball501/MultronWcleaner/issues).

### New: Microsoft MRT scan
The Malware Scan group on the main screen has a new **Microsoft MRT Scan** option. It scans with the Malicious Software Removal Tool (`MRT.exe`) that Windows Update installs on every PC.
- On by default with a **Quick Scan**. Choose **Full Scan** to check every fixed drive (this can take hours).
- MRT runs hidden, without its own window. Its progress is shown on the main progress ring and in the malware result panel, and the Clean button turns into **Cancel** while it runs.
- When **Multron Malware Scan Cloud (Testing)** is also selected, MRT runs first and the cloud scan follows. The findings of both are listed together in the malware result panel, and every row says which scanner found it: **Microsoft MRT** or **Multron Cloud**.
- MRT first only reports what it finds. **MRT Results** shows the findings with the full report of that scan from `mrt.log`, and **Remove with MRT** scans again and lets MRT remove the malicious software. Tick *Remove found malicious software right away* if MRT should clean during the scan.
- MRT works without internet, so it is also available in offline mode.
- If `MRT.exe` is missing, the option is hidden and the group explains that Windows Update installs it.

### New: Simplified interface
The cleaning list is now easier to read, similar to other well-known cleaners. The simplified interface is the default:
- One list for all users instead of a group per user, split into **Windows** and **Applications** (applications in alphabetical order).
- Plain item names such as *Local Temp* instead of `LocalTemp=C:\Users\...\AppData\Local\Temp`. The paths are in the tooltip, and right-click opens the location or copies the paths.
- Every group has a checkbox that selects or unselects the whole group and shows how many items are selected.
- The cleanmgr, DISM, SFC and Deep Log Files Scan commands and the extra scans have easy names (for example *Check and repair Windows system files* instead of `sfc.exe=sfc /scannow=...`) and are gathered under **Extra Scans & Tools** at the top of the list.

Prefer the detailed view? Choose **Advanced** in Settings > Interface Mode. It has the same look, but shows everything in detail: a group per user, every item on its own row with its full path, the raw commands and every database button. The cleaning list reloads when you switch.

### Changes
- The cloud malware scan option on the main screen is now called **Multron Malware Scan Cloud (Testing)**.
- The app builds without any compiler warnings, and unused code was removed.
- Security Check warnings can be ignored: click **Ignore** on a warning and it is no longer shown or reported. **Show Ignored Warnings** in Utilities > Security Check brings them back.
- In offline mode, the "database has not been updated" warning now appears after one day instead of after the update interval.

### Fixes
- The security check no longer shows Windows Security warnings (Microsoft Defender turned off, Security Center or Windows Security service disabled) when another antivirus is installed. Antivirus programs with "Defender" in their name, such as Bitdefender, were mistaken for Microsoft Defender, and antivirus programs were not found at all while the Security Center service was off. When no antivirus is turned on at all, the check still warns.
- The tray icon kept showing the "database has not been updated" warning after offline mode was turned off and the database was checked for updates. It now goes back to the normal icon as soon as the warning no longer applies.

For the full list of what is new in 1.26, see the [v1.26.2 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26.2), the [v1.26.1 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26.1) and the [v1.26 release](https://github.com/winball501/MultronWcleaner/releases/tag/v1.26).
