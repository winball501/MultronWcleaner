## Multron Win Cleaner 1.26 Update

Changes since test build a3.

### Languages
- The whole app is translated into 12 languages: English, Turkish, German, Spanish, French, Italian, Brazilian Portuguese, Russian, Japanese, Korean, Simplified Chinese and Traditional Chinese.
- The language can be changed in Settings; the app restarts to apply it.

### Malware scan

> **Test release:** the cloud malware scan is published for testing. It is released so it can be tried thoroughly and made stable, so results and behavior may still change. Please report false detections, missed threats or any problems in [Issues](https://github.com/winball501/MultronWcleaner/issues).

- **Cloud engine:** files are checked by the viruskov.com OPEN-EDR engine over an encrypted TLS connection. The Malware Scan window now also shows that the scan servers run on a VDS hosted by [Türkbil](https://turkbil.net.tr/).
- **License agreement:** shown once before the first cloud scan. It explains which files are sent; with the "Only executables & scripts" filter on (the default), personal files are never uploaded.
- **Quarantine:** detected files can be moved to a quarantine folder, restored to their original place or deleted, one by one or all at once.
- **Asks before removing threats:** when a clean finds malicious files from the last malware scan, it lists them and asks before deleting them. Automatic and startup cleans never delete them; they stay in the Malware Scan window for you to review.
- **Removed items stay in the list:** a file you quarantine or delete is no longer dropped from the results; its row changes to "Deleted by user" or "Quarantined by user" with the time, so you keep a history. Double-click a row, or use "Show Details", to see the file, threat, SHA-256 and the steps taken.
- **Remove All Threats:** one button deletes every remaining threat at once, after showing the list and asking. Files in a Windows or program folder are marked with a warning in that list.
- **Force-closes the threat:** deleting or quarantining a file now force-closes any program started from that exact file (and the programs it started), while never touching Windows system processes or a program with merely the same name.
- **Nothing is skipped:** threats in the Windows folder or Program Files are no longer skipped during a clean — once you confirm, they are removed like any other, with ownership taken when needed.
- **Encrypted connection only:** the developer server address accepts unencrypted ws:// only for a server on the same PC (localhost); other servers need wss://.
- **Tray icon:** a shield in the system tray shows when a scan is running and tells you when it finishes.
- **Possibly clean:** a new result for files that are almost identical to a verified clean file.
- **Rescan:** right-click a result and choose Rescan to check the file again without the server's saved verdict, for example after new rules or for a "possibly clean" file. A rescan can be stopped with the Stop button.
- **Faster uploads:** files are compressed before upload when that makes them at least 10% smaller.
- **Folder paths, without your user name:** the license agreement now explains that file names and their folder paths are sent to help find where malware hides. Your user folders are replaced by placeholders such as `%USERPROFILE%\Downloads`, so no Windows user name is sent.
- **Survives connection drops:** if the connection to the scan server is lost, for example while the server restarts, the scan waits and reconnects by itself instead of failing. It tries one connection at a time with growing pauses (2 to 30 seconds), tries each file up to 3 times, and stops with an error only after 4 minutes without the server or when the server refuses the connection.

### Right-click menu (new, optional)
- Settings > Right-Click Menu adds **Scan with Multron Malware Scan** to the File Explorer right-click menu for files, folders and drives.
- To scan several items at once, select them and use **Send To > Multron Malware Scan**. They are scanned together with a single UAC prompt.
- If the app is already running, also in the background, the items are handed to it and scanned there.
- On Windows 11 these entries are under "Show more options". The optional **Use the classic right-click menu in Windows 11** setting shows them in the first menu, like in Windows 10. It changes the menu for every program and needs File Explorer to restart.
- The scan respects the "Only executables & scripts" filter, as promised in the license agreement.

### Notifications after a background scan
- After an automatic or startup scan, notifications now also report insecure Windows settings (any risk level, not only high), broken firewall rules, duplicate files and malware scan results. Each has a button that opens the details.
- Notifications about problems use a red warning sign instead of the blue check mark.
- Settings > Startup & Notify Settings: each kind of notification can be turned on or off (all on by default), and you can choose the corner (bottom right by default) and whether notifications are lined up or stacked on top of each other.
- Several notifications no longer cover each other.
- "Notify me after a background scan / cleanup finishes" are now on by default.

### Theme
- The app opens in dark mode when Windows uses dark mode, otherwise in light mode, and switches along with Windows while it runs.
- Changing the theme by hand keeps your choice. Settings > Theme > **Match the Windows theme** turns automatic mode back on.

### Look & feel
- **Transparency effects (new, optional):** windows are slightly see-through with a colored tint. Settings > Theme lets you turn them off, pick the color (blue, green, orange, purple, red, pink, teal or gray) and the strength (light, medium or strong). On by default with blue at medium strength.
- **Search on the main screen:** a small search box next to the buttons filters the cleaning list by app name or file path (also matches paths like %TEMP%). Clearing it shows everything again.
- **New message windows:** all messages, warnings and questions now use the app's own dialog instead of the plain Windows message box. It matches the theme, dims the window it belongs to and shows an icon that fits the message (error, warning, question or information). Esc closes it and Ctrl+C copies its text.
- **Scroll bars** look the same in every window: thin and rounded, like on the main screen.
- **Windows always fit the screen:** a window never opens larger than the screen (without the taskbar) and is moved back if it would stick out, also on smaller or scaled screens and on the monitor it opens on.
- **Same search box everywhere:** Duplicate File Finder and Startup Manager now use the rounded search box of the main screen, with a hint text, a ✕ button and Esc to clear it. Startup Manager also searches the program path.
- **Colored message icons:** the icons in message windows (information, question, warning, error) are drawn in color and no longer look black in dark mode. The warning icons in Startup Manager and in the new startup item window were changed the same way.
- **Section icons:** System & Global Tools on the main screen shows a globe instead of a laptop, and user sections show a person icon in the same thin style. Both follow the text color in light and dark mode.

### Only one copy runs
- **No second copy:** starting Multron Win Cleaner while it already runs, also hidden in the tray, no longer opens a second copy. The running app shows its main window and brings it to the front instead.
- **Right-click scans go to the running app:** *Scan with Multron Malware Scan* hands the files to the running app, which opens the Malware Scan window in front and starts scanning right away.
- **Never hidden without a way back:** a copy started with Windows closes itself and removes itself from Windows startup when both tray icons (main and Utilities) are turned off, because it could not be reached. Turning on "Start Multron Win Cleaner when I sign in to Windows" (or a startup scan or clean) now also turns on the tray icon when neither tray icon is on.

### Settings
- **New title bar buttons:** reset to defaults (asks first), minimize, maximize/restore and close. The Settings window can now also be resized.
- **Settings are saved automatically** as soon as you change them. The Save Settings buttons in Settings and in the Malware Scan window were removed.
- **Windows startup is now a checkbox:** "Start Multron Win Cleaner when I sign in to Windows" replaces the Add to Startup / Remove from Startup buttons and always shows whether the app is in startup. Turning on the startup scan or cleanup offers to turn it on for you.
- **Automatic updates:** the app and the cleaning database can check for updates in the background. The database check interval, automatic loading of a downloaded database, the update status and the Update Database button on the main screen can each be set in Settings.
- **App Updates (new, at the top of Settings):** shows the installed version. **Check for Updates** looks for a newer version, **Update to …** installs it after asking, and **Release Notes** opens its page.

### Updates and installer
- **No more Updater.exe:** the app updates itself. It downloads the update, checks its size, swaps its own files and starts again. If something goes wrong while files are swapped, the old files are put back. Your settings, exclusions and selections are never overwritten. The old Updater.exe is removed automatically.
- **New installer (mwc_setup.exe):** installs for the current user in `%LOCALAPPDATA%\Programs\Multron Win Cleaner` without asking for administrator rights, and updates an existing installation in place. The setup uses your Windows language, with no language question. Uninstalling also removes leftover update files.
- **Portable version (mwc.zip):** the same files without installing.
- **Smaller app folder:** the extra language folders (de, fr, pl, …) and the `runtimes` folder are gone. The app's own 12 languages are not affected.

### Fixes
- The app could freeze and close at startup (two separate causes, both fixed).
- The start time of automatic cleaning was not saved.
- An app update package could write files outside the update folder. Such a package is now refused before anything is unpacked.
- Memory Cleaner no longer freezes for a few seconds while opening.
- Scroll bars in the Settings window could not be dragged while the window was maximized.
- Several buttons in Duplicate File Finder, Large File Finder and Memory Cleaner showed "?" instead of their icons.
- Long option texts on the main screen and the language list in Settings no longer run off the window.
- Fixed three XAML errors that Visual Studio's designer reported in the Settings and Malware Scan windows.
- Duplicate File Finder could treat its own hint text as a search.