## Multron Win Cleaner v1.24.7 beta

### What's new
- **Faster startup.** The cleaning list loads much faster and the window no longer freezes while it loads.
- **Stays fast as the list grows.** Adding more cleaning locations no longer makes the app noticeably slower.
- **Automatic saving.** Your choices are saved automatically as soon as you tick or untick a box. The Save Selections button is hidden while this is on. You can turn it off in Settings > Selections to save manually.
- **Instant saving.** Save Selections now saves your choices instantly.
- **Your choices are kept after updates.** When a new cleaning list is downloaded, your saved choices stay as they are.
- **All browser profiles are cleaned.** If you use more than one profile in Chrome, Edge, Firefox or another browser, every profile is now included. You can turn this off in Settings > Browsers to pick one profile per browser instead.
- **New Write Selections to Database button.** If you want, you can write your choices into the cleaning list itself. This is optional, and choices written this way are replaced the next time a new cleaning list is downloaded.

### Improvements
- The cleaning list is downloaded only when a newer one is available, so startup is quicker.
- If a download is interrupted, your current cleaning list stays untouched and the download is tried again on the next start.
- The app only updates itself to newer versions.
- Settings and Utilities are now labeled blue buttons at the top of the window, so they are easy to find. Discord and GitHub also look clearly clickable.
- Select All and Save Selections look cleaner on hover and stay readable in the light theme.

### Fixes
- Fixed the Utilities tray icon not appearing after it was turned off and on again.
- The app icon now shows on the taskbar instead of a blank window icon.
- Fixed a crash when scrolling to the bottom of the cleaning list.
- Fixed wrong user folders being used when no user was selected in Settings.
- A damaged line in the cleaning list no longer stops the whole list from loading.
- Fixed browser profile options appearing for browsers with no profiles.
- The browser profile list now shows only real profiles, and switching profiles correctly updates what gets cleaned.
- Fixed choices in long categories not being saved for items further down the list.

### Note
- Your choices are now stored in `selections.txt` next to the app. Choices you saved with an older version are still in the cleaning list and keep working.
