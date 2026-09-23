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
        int comboid = 0;
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

        private void Profilelist_SelectionChanged(object sender, SelectionChangedEventArgs e, int comboid)
        {
            try
            {
                System.Windows.Controls.ComboBox comboBox = sender as System.Windows.Controls.ComboBox;
                if (comboBox == null) return;

                string newText = (comboBox.SelectedItem?.ToString() ?? "").Trim();

                foreach (var oldItem in e.RemovedItems)
                {
                    string oldText = oldItem?.ToString()?.Trim() ?? "";

                    Expander parentExpander = FindParent<Expander>(comboBox);
                    if (parentExpander == null) return;

                    string expanderName = parentExpander.Header?.ToString()?.Trim() ?? "";

                    var checkBoxes = FindChildren<System.Windows.Controls.CheckBox>(parentExpander);
                    foreach (var box in checkBoxes)
                    {
                        string contentText = box.Content?.ToString() ?? "";

                        if (contentText.Contains(oldText))
                        {
                            string updatedContent = contentText.Replace(oldText, newText);
                            box.Content = updatedContent;

                            for (int i = 0; i < main.database.Count; i++)
                            {
                                if (main.database[i].Contains(expanderName))
                                {
                                    string key = main.stringtokenizer(main.database[i], "=", 0);
                                    string newValue = main.stringtokenizer(updatedContent, "=", 1);
                                    string newEntry = key + "=" + newValue;

                                    main.database[i] = newEntry;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Hata: " + ex.Message + "\n" + ex.StackTrace);
            }
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
            int startIndex = 0;

            if (existingBoxes.Any())
            {
                var lastBox = existingBoxes.Last();
                startIndex = allCheckboxes.FindLastIndex(cb => cb.Name == lastBox.Name && Equals(cb.Content, lastBox.Content)) + 1;
                if (startIndex < 0) startIndex = 0;
            }

            var nextBatch = allCheckboxes.Skip(startIndex).Take(3).ToList();

            await main.Dispatcher.InvokeAsync(() =>
            {
                nextBatch.ForEach(originalBox =>
                {
                    var newBox = new System.Windows.Controls.CheckBox
                    {
                        Name = originalBox.Name,
                        Content = originalBox.Content,
                        IsChecked = originalBox.IsChecked,
                        Margin = originalBox.Margin,
                        BorderThickness = new Thickness(0),
                        BorderBrush = new SolidColorBrush(Colors.Transparent),
                        Background = new SolidColorBrush(Colors.White),
                        Foreground = main.brush,
                        FontSize = 12,
                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                        FontWeight = FontWeights.Regular,
                        FontStyle = FontStyles.Normal,
                    };

                    newBox.Checked += main.CheckBox_Checked;
                    newBox.Unchecked += main.CheckBox_Unchecked;

                    listbox.Items.Add(newBox);
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
                        if (main.settings != null && main.settings.rbCleanAllUsers != null && main.settings.rbCleanAllUsers.IsChecked == true && main.settings.rbCleanSelectedUsers.IsChecked == false)
                        {
                            foreach (var item in main.settings.comboBoxUserSelection.Items)
                            {
                                targetUsers.Add(item.ToString());
                            }
                        }
                        else if (main.settings != null && main.settings.comboBoxUserSelection.SelectedItem != null)
                        {
                            targetUsers.Add(main.settings.comboBoxUserSelection.SelectedItem.ToString());
                        }
                        else
                        {
                            targetUsers.Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        }
                    });

                    if (targetUsers.Count == 0) return;

                    List<string> originalLines = System.IO.File.ReadAllLines(filePath).ToList();

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
                                        string processedLine = l;
                                        if (processedLine.Contains("{##}"))
                                        {
                                            processedLine = processedLine.Replace("{##}", user);
                                        }
                                        else
                                        {
                                            processedLine = System.Text.RegularExpressions.Regex.Replace(
                                                processedLine,
                                                @"C:\\Users\\[^\\]+",
                                                user,
                                                System.Text.RegularExpressions.RegexOptions.IgnoreCase
                                            );
                                        }
                                        uBlock.Add(processedLine);
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
                                    string processed = trim.Contains("{##}") ? trim.Replace("{##}", user) : System.Text.RegularExpressions.Regex.Replace(trim, @"C:\\Users\\[^\\]+", user, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                    userStandalone[user].Add(processed);
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

                    int totalLines = linesToProcess.Count;
                    int currentLine = 0;

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        var task = ScandotsAsync("Loading Database", cts.Token);
                        main.buttonStartScan.IsEnabled = false;
                    });

                    int groupboxmode = 0;
                    int created = 0;
                    int profileget = 0;
                    string groupboxcontent = "";
                    System.Windows.Controls.ListBox groupBoxContent = null;
                    Expander newExpander = null;
                    System.Windows.Controls.ComboBox profilelist = null;
                    string profile = "";

                    Expander currentUserExpander = null;
                    WrapPanel currentUserPanel = null;

                    foreach (string line in linesToProcess)
                    {
                        currentLine++;
                        double progress = (double)currentLine / totalLines * 100;

                        if (line.StartsWith("USER_START="))
                        {
                            string uName = line.Substring(11);
                            await main.Dispatcher.InvokeAsync(() => {
                                currentUserPanel = new WrapPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(5) };
                                currentUserExpander = new Expander
                                {
                                    Header = uName,
                                    Margin = new Thickness(5, 10, 5, 5),
                                    Foreground = main.brush,
                                    BorderBrush = main.brush,
                                    BorderThickness = new Thickness(1),
                                    Padding = new Thickness(5, 5, 5, 10),
                                    IsExpanded = true,
                                    FontSize = 15,
                                    FontWeight = FontWeights.Bold,
                                    Content = currentUserPanel
                                };
                                main.wrapPanel1.Children.Add(currentUserExpander);
                            });
                            await main.progressBar1.Dispatcher.InvokeAsync(() => { main.progressBar1.Value = progress; });
                            continue;
                        }

                        if (line == "USER_END")
                        {
                            await main.Dispatcher.InvokeAsync(() => {
                                if (currentUserPanel != null && currentUserPanel.Children.Count == 0 && currentUserExpander != null)
                                {
                                    main.wrapPanel1.Children.Remove(currentUserExpander);
                                }
                            });

                            currentUserExpander = null;
                            currentUserPanel = null;
                            await main.progressBar1.Dispatcher.InvokeAsync(() => { main.progressBar1.Value = progress; });
                            continue;
                        }

                        int linecontains = 0;
                        string name = main.stringtokenizer(line, "=", 0);
                        string path = main.stringtokenizer(line, "=", 1);
                        if (path == null) path = "";

                        getline = line.Trim();
                        bool recommended = false;

                        if (line.StartsWith("{"))
                        {
                            groupboxmode = 1;
                            linecontains = 1;
                            groupboxcontent = main.stringtokenizer(line, "=", 1);
                        }
                        else if (line.StartsWith("}"))
                        {
                            if (profilelist != null && groupBoxContent != null)
                            {
                                await main.Dispatcher.InvokeAsync(() => { groupBoxContent.Items.Add(profilelist); });
                            }
                            profile = "";
                            profilelist = null;
                            groupBoxContent = null;
                            linecontains = 1;
                            groupboxmode = 0;
                            created = 0;
                            profileget = 0;
                        }
                        else
                        {
                            if (line.Contains("#profileget#"))
                                recommended = bool.Parse(main.stringtokenizer(line, "=", 3));
                            else if (!line.StartsWith("#profile#"))
                                recommended = bool.Parse(main.stringtokenizer(line, "=", 2));
                        }

                        string haswarning = main.stringtokenizer(line, "=", 3);
                        if (haswarning == "true" || haswarning == "false") haswarning = null;

                        if (line.StartsWith("#profile#="))
                        {
                            path = main.stringtokenizer(line, "=", 1);
                            if (Directory.Exists(path))
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    profilelist = new System.Windows.Controls.ComboBox
                                    {
                                        Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7")),
                                        FontSize = 14,
                                        Margin = new Thickness(5),
                                        HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                                        VerticalAlignment = VerticalAlignment.Top
                                    };
                                    comboid++;
                                    profilelist.SelectionChanged += (s, e) => Profilelist_SelectionChanged(s, e, comboid);
                                });

                                string[] profileFolders = Directory.GetDirectories(path);
                                int i = 0;
                                int selectedindex = 0;
                                foreach (string profileFolder in profileFolders)
                                {
                                    string folderName = new DirectoryInfo(profileFolder).Name;
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (folderName.EndsWith("(release)") || folderName.StartsWith("Profile") || folderName.Contains("Default") || folderName.EndsWith(".default-release"))
                                            selectedindex = i;

                                        i++;
                                        profilelist.Items.Add(folderName);
                                    });
                                }
                                main.comboboxlist.Add(profilelist);

                                await main.Dispatcher.InvokeAsync(() => { profilelist.SelectedIndex = selectedindex; });
                                if (profilelist != null && profilelist.Items.Count > 0)
                                {
                                    await main.Dispatcher.InvokeAsync(() => { profile = path + profilelist.Items[selectedindex]; });
                                }
                                else
                                {
                                    await main.Dispatcher.InvokeAsync(() => { profile = ""; });
                                }
                                profileget = 1;
                            }
                        }
                        else if (line.Contains("#profileget#") && profileget == 1)
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                string dir = main.stringtokenizer(line, "=", 2);
                                path = profile + dir;
                            });
                        }

                        if (!line.StartsWith("#profile#=") && !line.StartsWith("{") && !line.StartsWith("}"))
                        {
                            string checkPath = path;
                            if (checkPath.Contains("*"))
                            {
                                checkPath = checkPath.Substring(0, checkPath.IndexOf('*'));
                            }

                            bool isValidItem = Directory.Exists(checkPath) || System.IO.File.Exists(checkPath);

                            if (isValidItem)
                            {
                                Debug.WriteLine(line);
                                if (recommended) main.database.Add(name + "=" + path);

                                await main.progressBar1.Dispatcher.InvokeAsync(() => { main.progressBar1.Value = progress; });

                                if (linecontains == 0)
                                {
                                    if (groupboxmode == 1)
                                    {
                                        await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            if (created == 0)
                                            {
                                                this.currentid = CreateRandomId(8);
                                                groupBoxContent = new System.Windows.Controls.ListBox
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

                                                newExpander = new Expander
                                                {
                                                    Name = currentid,
                                                    Header = groupboxcontent,
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

                                                created = 1;
                                            }

                                            System.Windows.Controls.CheckBox newCheckBox = new System.Windows.Controls.CheckBox
                                            {
                                                Name = currentid,
                                                Content = name + "=" + path,
                                                Margin = new Thickness(10),
                                                IsChecked = recommended,
                                                BorderThickness = new Thickness(0),
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                                                Foreground = main.brush,
                                                FontSize = 12,
                                                FontFamily = new System.Windows.Media.FontFamily("Segoe UI")
                                            };
                                            main.checkboxes2.Add(newCheckBox);

                                            if (haswarning != null) newCheckBox.Content = name + "=" + path + "=warning(" + haswarning + ")";

                                            newCheckBox.ContextMenu = new ContextMenu();
                                            newCheckBox.ContextMenu.Items.Add(new MenuItem { Header = "Open directory" });
                                            newCheckBox.ContextMenu.Items.Add(new MenuItem { Header = "Open location" });
                                            newCheckBox.ContextMenu.Items.Add(new MenuItem { Header = "Copy path" });
                                            ((MenuItem)newCheckBox.ContextMenu.Items[0]).Click += main.OpenDirectory_MainMenu_Click;
                                            ((MenuItem)newCheckBox.ContextMenu.Items[1]).Click += main.OpenFileLocation_MainMenu_Click;
                                            ((MenuItem)newCheckBox.ContextMenu.Items[2]).Click += main.CopyPath_MainMenu_Click;
                                            newCheckBox.PreviewMouseRightButtonUp += (s, ev) => {
                                                newCheckBox.ContextMenu.PlacementTarget = newCheckBox;
                                                newCheckBox.ContextMenu.IsOpen = true;
                                                ev.Handled = true;
                                            };
                                            newCheckBox.Checked += main.CheckBox_Checked;
                                            newCheckBox.Unchecked += main.CheckBox_Unchecked;
                                            main.listboxes.Add(groupBoxContent);
                                            main.expanders.Add(newExpander);

                                            if (groupBoxContent.Items.Count != 100)
                                            {
                                                groupBoxContent.Items.Add(newCheckBox);
                                                newExpander.Content = groupBoxContent;
                                                try
                                                {
                                                    if (currentUserPanel != null)
                                                    {
                                                        if (!currentUserPanel.Children.Contains(newExpander)) currentUserPanel.Children.Add(newExpander);
                                                    }
                                                    else
                                                    {
                                                        if (main.wrapPanel1.Children.Count != 100 && !main.wrapPanel1.Children.Contains(newExpander)) main.wrapPanel1.Children.Add(newExpander);
                                                    }
                                                }
                                                catch (Exception) { }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            System.Windows.Controls.CheckBox newCheckBox = new System.Windows.Controls.CheckBox
                                            {
                                                Name = currentid,
                                                Content = name + "=" + path,
                                                Margin = new Thickness(5),
                                                IsChecked = recommended,
                                                BorderThickness = new Thickness(0),
                                                Foreground = main.brush,
                                                VerticalAlignment = VerticalAlignment.Center,
                                                FontSize = 12,
                                                FontWeight = FontWeights.Bold,
                                                Padding = new Thickness(10)
                                            };
                                            main.checkboxes2.Add(newCheckBox);

                                            if (currentUserPanel != null) currentUserPanel.Children.Add(newCheckBox);
                                            else main.wrapPanel1.Children.Add(newCheckBox);

                                            newCheckBox.Checked += main.CheckBox_Checked;
                                            newCheckBox.Unchecked += main.CheckBox_Unchecked;
                                        });
                                    }
                                }
                            }
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
    }
}