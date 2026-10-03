## Multron Win Cleaner v1.24.8 beta

### What's new
- **See what is being cleaned.** Each location shows `Cleaning: name: path` while it is cleaned, and the status bar shows the current item and how many files were deleted so far.
- **Details for every location.** When a location is done, it shows how many files were deleted and how much space was freed, and how many files were locked.
- **Summary at the end.** A summary line shows the total files deleted, space freed, locked files, skipped locations and how long cleaning took.

### Improvements
- Cleaning is much faster when there are many files to delete.
- Scanning is faster, and a folder with one protected subfolder is no longer skipped completely.
- The Locked Files list opens much faster and no longer keeps files from earlier cleanings.
- Killing locked files never closes Windows system processes, and the result shows closed programs and deleted files separately.
- Cancelling a clean now stops right away, even inside a large folder.

### Fixes
- **Dism.exe component cleanup runs again.** The `Dism.exe /Online /Cleanup-Image /StartComponentCleanup` command was scanned but never run during cleaning. It now runs when it is ticked and frees the space the scan found in the WinSxS folder.
- **Dism.exe Restore Health runs when needed.** When the scan finds component store corruption and Restore Health is ticked, it now runs during cleaning. Before, it was always skipped.
- **Dism.exe size on every Windows language.** The WinSxS size showed 0 Byte on non-English Windows. It is now read correctly, and the analysis is no longer stopped after 1 minute.
- The "not accessed for" scan filter now uses the number of days you pick. Before, it always used 7 days.
- Locked files inside folders now appear in the Locked Files list.
- An unticked single file is shown as Ignored instead of "Does not exist".
