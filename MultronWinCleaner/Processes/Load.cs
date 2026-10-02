using Multron_Win_Cleaner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MultronWinCleaner;

namespace MultronWinCleaner.Processes
{
    public class Load
    {
        MainWindow main;
        CancellationTokenSource cts = new CancellationTokenSource();
        string currentid = null;
        string getline = null;

        public Load(MainWindow main)
        {
            this.main = main;
        }

        public async System.Threading.Tasks.Task ScandotsAsync(string text, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await main.Dispatcher.InvokeAsync(() => {
                        main.StatusLoad.Text = text + ".";
                        main.StatusLoad.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7"));
                    });

                    await System.Threading.Tasks.Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.StatusLoad.Text = text + "..";
                    });

                    await System.Threading.Tasks.Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.StatusLoad.Text = text + "...";
                    });

                    await System.Threading.Tasks.Task.Delay(1000, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        private void Profilelist_SelectionChanged(System.Windows.Controls.ComboBox comboBox, LoadGroup group, System.Windows.Controls.ListBox listBox, string groupId)
        {
            try
            {
                if (comboBox.SelectedItem is not string folder) return;

                foreach (System.Windows.Controls.CheckBox oldBox in group.ProfileCheckBoxes)
                {
                    string content = oldBox.Content?.ToString() ?? "";
                    main.database.Remove(main.stringtokenizer(content, "=", 0) + "=" + main.stringtokenizer(content, "=", 1));
                    main.checkboxes2.Remove(oldBox);
                    listBox.Items.Remove(oldBox);
                }
                group.ProfileCheckBoxes.Clear();

                int insertAt = listBox.Items.IndexOf(comboBox);
                if (insertAt < 0) insertAt = listBox.Items.Count;

                foreach (ProfileLine line in group.ProfileLines)
                {
                    string path = group.ProfileRoot + folder + line.SubPath;
                    if (!PathExists(path)) continue;

                    System.Windows.Controls.CheckBox box = CreateEntryCheckBox(new LoadEntry { Name = line.Name, Path = path, Recommended = line.Recommended, IsProfile = true }, groupId);
                    group.ProfileCheckBoxes.Add(box);
                    listBox.Items.Insert(insertAt++, box);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not switch browser profile in " + group.Header + ": " + ex.Message);
            }
        }

        private static bool PathExists(string path)
        {
            string checkPath = path.Contains('*') ? path.Substring(0, path.IndexOf('*')) : path;
            return Directory.Exists(checkPath) || File.Exists(checkPath);
        }

        private static List<string> GetProfileFolders(string root)
        {
            List<string> folders = Directory.GetDirectories(root).Select(f => new DirectoryInfo(f).Name).ToList();
            if (root.TrimEnd('\\').EndsWith("Profiles", StringComparison.OrdinalIgnoreCase)) return folders;

            List<string> profiles = folders.Where(name =>
                name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase) ||
                File.Exists(System.IO.Path.Combine(root, name, "Preferences")) ||
                File.Exists(System.IO.Path.Combine(root, name, "prefs.js"))).ToList();

            return profiles.Count > 0 ? profiles : folders;
        }

        private static int PickDefaultProfile(List<string> folders)
        {
            int index = folders.FindIndex(f => f.Equals("Default", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".default-release", StringComparison.OrdinalIgnoreCase) || f.EndsWith("(release)", StringComparison.OrdinalIgnoreCase));
            if (index < 0) index = folders.FindIndex(f => f.StartsWith("Profile", StringComparison.OrdinalIgnoreCase) || f.Contains("default", StringComparison.OrdinalIgnoreCase));
            return Math.Max(0, index);
        }

        public static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T t)
                    yield return t;

                foreach (T descendant in FindChildren<T>(child))
                    yield return descendant;
            }
        }

        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private Expander FindExpanderFromScrollViewer(ScrollViewer scrollViewer)
        {
            DependencyObject current = scrollViewer;
            while (current != null)
            {
                if (current is Expander expander)
                    return expander;

                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;

            double verticalOffset = scrollViewer.VerticalOffset;
            double viewportHeight = scrollViewer.ViewportHeight;
            double extentHeight = scrollViewer.ExtentHeight;

            if (extentHeight <= 0 || viewportHeight <= 0) return;

            const double threshold = 20.0;
            bool isAtBottom = (extentHeight - (verticalOffset + viewportHeight)) <= threshold;

            if (!isAtBottom) return;

            var listbox = FindListBoxFromScrollViewer(scrollViewer);
            var expander = FindExpanderFromScrollViewer(scrollViewer);

            if (listbox != null && expander != null)
            {
                string groupid = expander.Name;
                await LoadMoreItemsForListBox_ByGroup(listbox, groupid);
            }
        }

        private System.Windows.Controls.ListBox FindListBoxFromScrollViewer(ScrollViewer sv)
        {
            foreach (var lb in main.listboxes)
            {
                if (VisualTreeHelper.GetChildrenCount(lb) > 0)
                {
                    var border = VisualTreeHelper.GetChild(lb, 0) as Border;
                    if (border != null && VisualTreeHelper.GetChildrenCount(border) > 0)
                    {
                        var scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                        if (scrollViewer == sv)
                            return lb;
                    }
                }
            }
            return null;
        }

        private async System.Threading.Tasks.Task LoadMoreItemsForListBox_ByGroup(System.Windows.Controls.ListBox listbox, string groupid)
        {
            var allCheckboxes = main.checkboxes2
                .Where(cb => cb?.Name != null && cb.Name.ToString() == groupid)
                .ToList();

            var existingBoxes = listbox.Items.OfType<System.Windows.Controls.CheckBox>().ToList();
            int startIndex = existingBoxes.Any() ? allCheckboxes.IndexOf(existingBoxes.Last()) + 1 : 0;

            var nextBatch = allCheckboxes.Skip(startIndex).Take(3).ToList();

            await main.Dispatcher.InvokeAsync(() =>
            {
                nextBatch.ForEach(originalBox =>
                {
                    if (originalBox.Parent == null) listbox.Items.Add(originalBox);
                });
            });
        }

        public string CreateRandomId(int length = 8)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            Random rnd = new Random();
            char firstChar = letters[rnd.Next(letters.Length)];
            string rest = new string(Enumerable.Repeat(chars, length - 1)
                .Select(s => s[rnd.Next(s.Length)]).ToArray());

            return firstChar + rest;
        }

        private sealed class LoadEntry
        {
            public string Name;
            public string Path;
            public bool Recommended;
            public string Warning;
            public bool IsProfile;
        }

        private sealed class ProfileLine
        {
            public string Name;
            public string SubPath;
            public bool Recommended;
        }

        private sealed class LoadGroup
        {
            public string Header;
            public List<LoadEntry> Entries = new List<LoadEntry>();
            public string ProfileRoot;
            public List<string> ProfileFolders;
            public int ProfileIndex;
            public List<ProfileLine> ProfileLines = new List<ProfileLine>();
            public List<System.Windows.Controls.CheckBox> ProfileCheckBoxes = new List<System.Windows.Controls.CheckBox>();
        }

        private readonly bool allBrowserProfiles = MainWindow.IsCleanAllBrowserProfilesEnabled();

        private sealed class LoadSection
        {
            public string Title;
            public List<LoadGroup> Groups = new List<LoadGroup>();
        }

        private Dictionary<string, bool> selections = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public static string SelectionsPath => System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "selections.txt");

        public static Dictionary<string, bool> ReadSelections()
        {
            var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (File.Exists(SelectionsPath))
                {
                    foreach (string line in File.ReadAllLines(SelectionsPath))
                    {
                        int separator = line.LastIndexOf('=');
                        if (separator > 0 && bool.TryParse(line.Substring(separator + 1), out bool value))
                        {
                            result[line.Substring(0, separator)] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not read selections.txt: " + ex.Message);
            }
            return result;
        }

        public static void WriteSelections(Dictionary<string, bool> selections)
        {
            string tempPath = SelectionsPath + ".tmp";
            File.WriteAllLines(tempPath, selections.Select(s => s.Key + "=" + (s.Value ? "true" : "false")));
            File.Move(tempPath, SelectionsPath, overwrite: true);
        }

        public async System.Threading.Tasks.Task RunAsync()
        {
            try
            {
                await main.Dispatcher.InvokeAsync(async () =>
                {
                    await main.loadothers();
                    await main.loadothers2();
                });

                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.txt");
                if (System.IO.File.Exists(filePath))
                {
                    List<string> targetUsers = new List<string>();

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        targetUsers.Clear();


                        try
                        {
                            if (main.settings != null && main.settings.rbCleanAllUsers != null && main.settings.rbCleanAllUsers.IsChecked == true && main.settings.rbCleanSelectedUsers.IsChecked == false)
                            {
                                foreach (var item in main.settings.comboBoxUserSelection.Items)
                                {
                                    targetUsers.Add(item.ToString());
                                }
                            }
                            else if (main.settings != null && main.settings.comboBoxUserSelection != null && main.settings.comboBoxUserSelection.SelectedItem != null)
                            {
                                targetUsers.Add(main.settings.comboBoxUserSelection.SelectedItem.ToString());
                            }
                        }
                        catch (Exception)
                        {

                        }

                        if (targetUsers.Count == 0)
                        {
                            targetUsers.Add(Environment.UserName);
                        }
                    });

                    List<string> originalLines = System.IO.File.ReadAllLines(filePath).ToList();
                    List<string> linesToProcess = BuildLinesToProcess(originalLines, targetUsers);
                    selections = ReadSelections();

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        var task = ScandotsAsync("Loading Database", cts.Token);
                        main.buttonStartScan.IsEnabled = false;
                    });

                    List<LoadSection> sections = await System.Threading.Tasks.Task.Run(() => ResolveSections(linesToProcess));

                    int totalGroups = Math.Max(1, sections.Sum(s => s.Groups.Count));
                    int doneGroups = 0;

                    foreach (LoadSection section in sections)
                    {
                        WrapPanel sectionPanel = null;
                        Expander sectionExpander = null;

                        if (section.Title != null)
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                sectionPanel = new WrapPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(5) };
                                sectionExpander = new Expander
                                {
                                    Header = section.Title,
                                    Margin = new Thickness(5, 10, 5, 5),
                                    BorderThickness = new Thickness(1),
                                    Padding = new Thickness(5, 5, 5, 10),
                                    IsExpanded = true,
                                    FontSize = 15,
                                    FontWeight = FontWeights.Bold,
                                    Content = sectionPanel
                                };
                                sectionExpander.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, "Text");
                                sectionExpander.SetResourceReference(System.Windows.Controls.Control.BorderBrushProperty, "Text");
                                main.wrapPanel1.Children.Add(sectionExpander);
                            });
                        }

                        foreach (LoadGroup group in section.Groups)
                        {
                            doneGroups++;
                            double progress = (double)doneGroups / totalGroups * 100;

                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                try
                                {
                                    BuildGroup(group, sectionPanel);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Could not build group " + group.Header + ": " + ex.Message);
                                }
                                main.progressBar1.Value = progress;
                            }, System.Windows.Threading.DispatcherPriority.Background);
                        }

                        if (sectionExpander != null)
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                if (sectionPanel.Children.Count == 0)
                                {
                                    main.wrapPanel1.Children.Remove(sectionExpander);
                                }
                            });
                        }
                    }

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        string lastLine = null;
                        if (!string.IsNullOrWhiteSpace(main.settings.logfilepath) && File.Exists(main.settings.logfilepath))
                            lastLine = File.ReadLines(main.settings.logfilepath).LastOrDefault();

                        if (lastLine != null && main.settings.chkShowLastLog.IsChecked == true) main.label1_Copy.Text = lastLine;
                        else main.label1_Copy.Text = "Ready to scan";

                        main.buttonStartScan.IsEnabled = true;
                        main.progressBar1.Value = 0;
                        main.UpdateArc(main.progressBar1.Value);
                        cts.Cancel();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message + " in Load.cs\n" + ex.StackTrace + "\nLine: " + getline, "database.txt error");
            }
        }

        private List<string> BuildLinesToProcess(List<string> originalLines, List<string> targetUsers)
        {
            List<string> globalStandalone = new List<string>();
            List<List<string>> globalBlocks = new List<List<string>>();
            Dictionary<string, List<string>> userStandalone = new Dictionary<string, List<string>>();
            Dictionary<string, List<List<string>>> userBlocks = new Dictionary<string, List<List<string>>>();

            foreach (var user in targetUsers)
            {
                userStandalone[user] = new List<string>();
                userBlocks[user] = new List<List<string>>();
            }

            bool inGroup = false;
            string blockHeader = "";
            List<string> currentBuffer = new List<string>();

            foreach (var line in originalLines)
            {
                string trim = line.Trim();
                if (string.IsNullOrWhiteSpace(trim)) continue;

                if (trim.StartsWith("{"))
                {
                    inGroup = true;
                    blockHeader = trim;
                    currentBuffer.Clear();
                }
                else if (trim.StartsWith("}"))
                {
                    inGroup = false;

                    List<string> globalBufferLines = new List<string>();
                    List<string> userBufferLines = new List<string>();

                    foreach (var l in currentBuffer)
                    {
                        bool isUserSpecific = l.Contains("{##}") ||
                                              l.Contains("AppData", StringComparison.OrdinalIgnoreCase) ||
                                              l.Contains("LocalSettings", StringComparison.OrdinalIgnoreCase) ||
                                              l.Contains("C:\\Users\\", StringComparison.OrdinalIgnoreCase) ||
                                              l.Contains("#profileget#", StringComparison.OrdinalIgnoreCase) ||
                                              l.Contains("#profile#", StringComparison.OrdinalIgnoreCase);

                        bool isProgramData = l.Contains("ProgramData", StringComparison.OrdinalIgnoreCase);

                        if (isUserSpecific && !isProgramData)
                        {
                            userBufferLines.Add(l);
                        }
                        else
                        {
                            globalBufferLines.Add(l);
                        }
                    }

                    if (globalBufferLines.Count > 0)
                    {
                        var gBlock = new List<string> { blockHeader };
                        gBlock.AddRange(globalBufferLines);
                        gBlock.Add("}");
                        globalBlocks.Add(gBlock);
                    }

                    if (userBufferLines.Count > 0)
                    {
                        foreach (var user in targetUsers)
                        {
                            var uBlock = new List<string> { blockHeader };
                            foreach (var l in userBufferLines)
                            {
                                uBlock.Add(ForUser(l, user));
                            }
                            uBlock.Add("}");
                            userBlocks[user].Add(uBlock);
                        }
                    }
                    currentBuffer.Clear();
                }
                else if (inGroup)
                {
                    currentBuffer.Add(trim);
                }
                else
                {
                    if (trim.Contains("{##}") || trim.Contains("AppData", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var user in targetUsers)
                        {
                            userStandalone[user].Add(ForUser(trim, user));
                        }
                    }
                    else
                    {
                        globalStandalone.Add(trim);
                    }
                }
            }

            List<string> linesToProcess = new List<string>();

            linesToProcess.Add("USER_START=💻 System & Global Tools");
            if (globalStandalone.Count > 0)
            {
                linesToProcess.Add("{=General Tools");
                linesToProcess.AddRange(globalStandalone);
                linesToProcess.Add("}");
            }
            foreach (var block in globalBlocks) linesToProcess.AddRange(block);
            linesToProcess.Add("USER_END");

            foreach (var user in targetUsers)
            {
                bool hasContent = userBlocks[user].Count > 0 || userStandalone[user].Count > 0;
                if (hasContent)
                {
                    string userName = new DirectoryInfo(user).Name;
                    linesToProcess.Add($"USER_START=👤 {userName}");

                    if (userStandalone[user].Count > 0)
                    {
                        linesToProcess.Add("{=User Specific Files");
                        linesToProcess.AddRange(userStandalone[user]);
                        linesToProcess.Add("}");
                    }
                    foreach (var block in userBlocks[user]) linesToProcess.AddRange(block);

                    linesToProcess.Add("USER_END");
                }
            }

            return linesToProcess;
        }

        private static string ForUser(string line, string user)
        {
            if (line.Contains("{##}"))
            {
                return line.Replace("{##}", user);
            }

            string profileRoot = System.IO.Path.Combine("C:\\Users", user).Replace("$", "$$");
            return System.Text.RegularExpressions.Regex.Replace(line, @"C:\\Users\\[^\\]+", profileRoot, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        private List<LoadSection> ResolveSections(List<string> lines)
        {
            var sections = new List<LoadSection>();
            var pending = new List<(LoadGroup Group, LoadEntry Entry, string CheckPath)>();

            LoadSection section = null;
            LoadGroup group = null;
            List<string> profileTargets = null;

            foreach (string line in lines)
            {
                getline = line.Trim();

                if (line.StartsWith("USER_START="))
                {
                    section = new LoadSection { Title = line.Substring(11) };
                    sections.Add(section);
                    continue;
                }

                if (line == "USER_END")
                {
                    section = null;
                    continue;
                }

                if (line.StartsWith("{"))
                {
                    if (section == null)
                    {
                        section = new LoadSection();
                        sections.Add(section);
                    }
                    group = new LoadGroup { Header = main.stringtokenizer(line, "=", 1) ?? "" };
                    section.Groups.Add(group);
                    profileTargets = null;
                    continue;
                }

                if (line.StartsWith("}"))
                {
                    group = null;
                    profileTargets = null;
                    continue;
                }

                if (group == null) continue;

                try
                {
                    if (line.StartsWith("#profile#="))
                    {
                        string root = main.stringtokenizer(line, "=", 1) ?? "";
                        if (Directory.Exists(root))
                        {
                            List<string> folders = GetProfileFolders(root);
                            if (folders.Count > 0)
                            {
                                group.ProfileRoot = root;
                                group.ProfileFolders = folders;
                                group.ProfileIndex = PickDefaultProfile(folders);
                                profileTargets = allBrowserProfiles ? folders : new List<string> { folders[group.ProfileIndex] };
                            }
                        }
                        continue;
                    }

                    string name = main.stringtokenizer(line, "=", 0);
                    string path;
                    string state;
                    string warning = null;

                    if (line.Contains("#profileget#"))
                    {
                        if (profileTargets == null) continue;
                        string subPath = main.stringtokenizer(line, "=", 2);
                        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(subPath) || !bool.TryParse(main.stringtokenizer(line, "=", 3), out bool profileRecommended))
                        {
                            Debug.WriteLine("Skipped invalid database line: " + line);
                            continue;
                        }

                        group.ProfileLines.Add(new ProfileLine { Name = name, SubPath = subPath, Recommended = profileRecommended });
                        foreach (string folder in profileTargets)
                        {
                            string profilePath = group.ProfileRoot + folder + subPath;
                            pending.Add((group, new LoadEntry { Name = name, Path = profilePath, Recommended = profileRecommended, IsProfile = true }, profilePath));
                        }
                        continue;
                    }
                    else
                    {
                        path = main.stringtokenizer(line, "=", 1);
                        state = main.stringtokenizer(line, "=", 2);
                        warning = main.stringtokenizer(line, "=", 3);
                        if (warning == "true" || warning == "false") warning = null;
                    }

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(path) || !bool.TryParse(state, out bool recommended))
                    {
                        Debug.WriteLine("Skipped invalid database line: " + line);
                        continue;
                    }

                    pending.Add((group, new LoadEntry { Name = name, Path = path, Recommended = recommended, Warning = warning }, path));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Skipped database line: " + line + " (" + ex.Message + ")");
                }
            }

            bool[] exists = new bool[pending.Count];
            Parallel.For(0, pending.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount) }, i =>
            {
                exists[i] = PathExists(pending[i].CheckPath);
            });

            for (int i = 0; i < pending.Count; i++)
            {
                if (exists[i]) pending[i].Group.Entries.Add(pending[i].Entry);
            }

            return sections;
        }

        private System.Windows.Controls.CheckBox CreateEntryCheckBox(LoadEntry entry, string groupId)
        {
            bool isChecked = selections.TryGetValue(entry.Path, out bool saved) ? saved : entry.Recommended;
            main.databaseDefaults[entry.Path] = entry.Recommended;
            if (isChecked) main.database.Add(entry.Name + "=" + entry.Path);

            System.Windows.Controls.CheckBox newCheckBox = new System.Windows.Controls.CheckBox
            {
                Name = groupId,
                Content = entry.Warning != null ? entry.Name + "=" + entry.Path + "=warning(" + entry.Warning + ")" : entry.Name + "=" + entry.Path,
                Margin = new Thickness(10),
                IsChecked = isChecked,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = main.brush,
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
            };
            newCheckBox.PreviewMouseRightButtonUp += EntryCheckBox_PreviewMouseRightButtonUp;
            newCheckBox.Checked += main.CheckBox_Checked;
            newCheckBox.Unchecked += main.CheckBox_Unchecked;
            main.checkboxes2.Add(newCheckBox);
            return newCheckBox;
        }

        private void BuildGroup(LoadGroup group, WrapPanel sectionPanel)
        {
            bool showProfileList = !allBrowserProfiles && group.ProfileFolders != null && group.ProfileFolders.Count > 1;
            if (group.Entries.Count == 0 && !showProfileList) return;

            this.currentid = CreateRandomId(8);
            string groupId = currentid;
            var groupBoxContent = new System.Windows.Controls.ListBox
            {
                ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))),
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = main.brush,
                MaxHeight = SystemParameters.WorkArea.Height,
                MaxWidth = SystemParameters.WorkArea.Width
            };

            groupBoxContent.Loaded += (s, e) => {
                var listBox = s as System.Windows.Controls.ListBox;
                if (listBox == null) return;
                var sv = main.FindVisualChild<ScrollViewer>(listBox);
                if (sv != null) sv.ScrollChanged += ScrollViewer_ScrollChanged;
            };

            var newExpander = new Expander
            {
                Name = currentid,
                Header = group.Header,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = main.brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = groupBoxContent
            };

            main.listboxes.Add(groupBoxContent);
            main.expanders.Add(newExpander);

            foreach (LoadEntry entry in group.Entries)
            {
                System.Windows.Controls.CheckBox newCheckBox = CreateEntryCheckBox(entry, groupId);
                if (entry.IsProfile) group.ProfileCheckBoxes.Add(newCheckBox);

                if (groupBoxContent.Items.Count < 100)
                {
                    groupBoxContent.Items.Add(newCheckBox);
                }
            }

            if (sectionPanel != null)
            {
                sectionPanel.Children.Add(newExpander);
            }
            else if (main.wrapPanel1.Children.Count != 100)
            {
                main.wrapPanel1.Children.Add(newExpander);
            }

            if (showProfileList)
            {
                var profilelist = new System.Windows.Controls.ComboBox
                {
                    Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7")),
                    FontSize = 14,
                    Margin = new Thickness(5),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top,
                    ToolTip = "Browser profile to clean"
                };
                foreach (string folder in group.ProfileFolders)
                {
                    profilelist.Items.Add(folder);
                }
                profilelist.SelectedIndex = group.ProfileIndex;
                profilelist.SelectionChanged += (s, e) => Profilelist_SelectionChanged(profilelist, group, groupBoxContent, groupId);
                main.comboboxlist.Add(profilelist);
                groupBoxContent.Items.Add(profilelist);
            }
        }

        private void EntryCheckBox_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.CheckBox box) return;

            if (box.ContextMenu == null)
            {
                var menu = new ContextMenu();
                var openDirectory = new MenuItem { Header = "Open directory" };
                var openLocation = new MenuItem { Header = "Open location" };
                var copyPath = new MenuItem { Header = "Copy path" };
                openDirectory.Click += main.OpenDirectory_MainMenu_Click;
                openLocation.Click += main.OpenFileLocation_MainMenu_Click;
                copyPath.Click += main.CopyPath_MainMenu_Click;
                menu.Items.Add(openDirectory);
                menu.Items.Add(openLocation);
                menu.Items.Add(copyPath);
                box.ContextMenu = menu;
            }

            box.ContextMenu.PlacementTarget = box;
            box.ContextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}
