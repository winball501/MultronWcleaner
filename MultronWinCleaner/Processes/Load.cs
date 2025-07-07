using Multron_Win_Cleaner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        public Load(MainWindow main)
        {
            this.main = main;

        }
        public async Task ScandotsAsync(string text, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = text + ".";
                        main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                    });

                    await Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = text + "..";
                    });

                    await Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = text + "...";
                    });

                    await Task.Delay(1000, cancellationToken);
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
                ComboBox comboBox = sender as ComboBox;
                if (comboBox == null) return;

                string newText = (comboBox.SelectedItem?.ToString() ?? "").Trim();

                foreach (var oldItem in e.RemovedItems)
                {
                    string oldText = oldItem?.ToString()?.Trim() ?? "";


                    Expander parentExpander = FindParent<Expander>(comboBox);
                    string expanderName = parentExpander.Header?.ToString()?.Trim() ?? "";
                    if (parentExpander == null) return;

                    var checkBoxes = FindChildren<CheckBox>(parentExpander);
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
                MessageBox.Show("Hata: " + ex.Message + "\n" + ex.StackTrace);
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
        private ListBox FindListBoxFromScrollViewer(ScrollViewer sv)
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
        private async Task LoadMoreItemsForListBox_ByGroup(ListBox listbox, string groupid)
        {
            var allCheckboxes = main.checkboxes2
                .Where(cb => cb?.Name != null && cb.Name.ToString() == groupid)
                .ToList();

            var existingBoxes = listbox.Items
                .OfType<CheckBox>()
                .ToList();


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
                    var newBox = new CheckBox
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
        public async Task RunAsync()
        {
            try
            {

                string filePath = Environment.CurrentDirectory + "\\" + "database.txt";
                if (System.IO.File.Exists(filePath))
                {
                    int totalLines = System.IO.File.ReadLines(filePath).Count();
                    int currentLine = 0;


                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;


                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            var task = ScandotsAsync("Loading Database", cts.Token);
                            main.buttonStartScan.IsEnabled = false;


                        });
                        int groupboxmode = 0;
                        int created = 0;
                        int profileget = 0;
                        string groupboxcontent = "";
                        GroupBox newGroupBox = null;
                        ListBox groupBoxContent = null;
                        Expander newExpander = null;
                        ComboBox profilelist = null;
                        ScrollViewer scrollViewer = null;
                        string profile = "";
                        while ((line = reader.ReadLine()) != null)
                        {
                            currentLine++;
                            double progress = (double)currentLine / totalLines * 100;
                            int linecontains = 0;
                            string name = main.stringtokenizer(line, "=", 0);
                            string path = main.stringtokenizer(line, "=", 1);

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
                                    main.Dispatcher.Invoke(() =>
                                    {
                                        groupBoxContent.Items.Add(profilelist);
                                    });


                                }
                                profile = "";
                                profilelist = null;
                                groupBoxContent = null;
                                linecontains = 1;
                                groupboxmode = 0;
                                created = 0;
                                profile = "";

                            }
                            else
                            {
                                if (line.Contains("#profileget#"))
                                {
                                    recommended = bool.Parse(main.stringtokenizer(line, "=", 3));

                                }
                                else
                                {
                                    if (!line.StartsWith("#profile#"))
                                    {
                                        recommended = bool.Parse(main.stringtokenizer(line, "=", 2));
                                    }

                                }

                            }

                            string haswarning = main.stringtokenizer(line, "=", 3);
                            if (haswarning == "true" || haswarning == "false")
                            {
                                haswarning = null;
                            }
                            if (line.Contains("{##}"))
                            {
                                await main.Dispatcher.InvokeAsync(async () =>
                                {
                                    path = path.Replace("{##}", main.settings.comboBoxUserSelection.SelectedItem.ToString());
                                });

                            }
                            if (line.Contains("#profile#="))
                            {
                                path = main.stringtokenizer(line, "=", 1);
                                if (line.Contains("{##}"))
                                {
                                    await main.Dispatcher.InvokeAsync(async () =>
                                    {
                                        path = path.Replace("{##}", main.settings.comboBoxUserSelection.SelectedItem.ToString());
                                    });

                                }



                                if (Directory.Exists(path))
                                {

                                    await main.Dispatcher.InvokeAsync(async () =>
                                    {
                                        profilelist = new ComboBox
                                        {
                                            Foreground = System.Windows.Media.Brushes.Blue,
                                            FontSize = 16,
                                            Margin = new Thickness(5),
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
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
                                          
                                         

                                            

                                            if (folderName.EndsWith("(release)"))
                                            {
                                                selectedindex = i;

                                               
                                            }
                                            if (folderName.StartsWith("Profile") || folderName.Contains("Default") || folderName.EndsWith(".default-release"))
                                            {

                                                selectedindex = i;

                                              

                                            }

                                            i++;
                                            profilelist.Items.Add(folderName);
                                        });


                                    }
                                    main.comboboxlist.Add(profilelist);
                                   
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        profilelist.SelectedIndex = selectedindex;
                                    });
                                    if (profilelist != null && profilelist.Items.Count > 0)
                                    {
                                        await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            profile = path + profilelist.Items[selectedindex];

                                        });

                                    }
                                    profileget = 1;
                                }




                            }
                            else if (line.Contains("#profileget#") && profileget == 1)
                            {

                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    string name = main.stringtokenizer(line, "=", 0);
                                    string dir = main.stringtokenizer(line, "=", 2);

                                    profile = profile.Replace("#profileget#", dir);

                                    path = profile + dir;

                                });
                            }

                            if (!line.Contains("#profile#"))
                            {
                                if (Directory.Exists(path) || System.IO.File.Exists(path))
                                {
                                    Debug.WriteLine(line);
                                    if (recommended)
                                    {
                                        main.database.Add(name + "=" + path);
                                    }





                                    await main.progressBar1.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.progressBar1.Value = progress;

                                    });


                                    if (linecontains == 0)
                                    {
                                        if (groupboxmode == 1)
                                        {

                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                if (created == 0)
                                                {
                                                    this.currentid = CreateRandomId(8);
                                                    groupBoxContent = new ListBox
                                                    {

                                                        ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))),
                                                        VerticalAlignment = VerticalAlignment.Stretch,
                                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                                        Margin = new Thickness(5),
                                                        Background = System.Windows.Media.Brushes.Transparent,
                                                        BorderThickness = new Thickness(0),
                                                        Foreground = main.brush,

                                                        MaxHeight = SystemParameters.WorkArea.Height,
                                                        MaxWidth = SystemParameters.WorkArea.Width

                                                    };



                                                    groupBoxContent.Loaded += (s, e) =>
                                                    {
                                                        var listBox = s as ListBox;
                                                        if (listBox == null) return;

                                                        var scrollViewer = main.FindVisualChild<ScrollViewer>(listBox);
                                                        if (scrollViewer != null)
                                                        {
                                                            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                                                        }
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
                                                        FontSize = 14,
                                                        FontWeight = FontWeights.Regular,
                                                        HorizontalAlignment = HorizontalAlignment.Left,
                                                        VerticalAlignment = VerticalAlignment.Top,
                                                        Content = scrollViewer

                                                    };

                                                    created = 1;
                                                }



                                                CheckBox newCheckBox = new CheckBox
                                                {
                                                    Name = currentid,
                                                    Content = name + "=" + path + "=warning(" + "no warning" + ")",
                                                    Margin = new Thickness(10),
                                                    IsChecked = recommended,
                                                    BorderThickness = new Thickness(0),
                                                    BorderBrush = new SolidColorBrush(Colors.Transparent),


                                                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                    Foreground = main.brush,

                                                    FontSize = 12,
                                                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                                                    FontWeight = FontWeights.Regular,
                                                    FontStyle = FontStyles.Normal,
                                                };
                                                main.checkboxes2.Add(newCheckBox);
                                                if (haswarning != null)
                                                {
                                                    newCheckBox.Content = name + "=" + path + "=warning(" + haswarning + ")";
                                                }
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
                                                        if (main.wrapPanel1.Children.Count != 100)
                                                        {
                                                            main.wrapPanel1.Children.Add(newExpander);
                                                        }

                                                    }
                                                    catch (Exception)
                                                    {

                                                    }
                                                }





                                            });




                                        }
                                        else
                                        {
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                CheckBox newCheckBox = new CheckBox
                                                {
                                                    Name = currentid,
                                                    Content = name + "=" + path,
                                                    Margin = new Thickness(5),
                                                    IsChecked = recommended,
                                                    BorderThickness = new Thickness(0),
                                                    BorderBrush = new SolidColorBrush(Colors.White),


                                                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                    Foreground = main.brush,
                                                    VerticalAlignment = VerticalAlignment.Center,
                                                    FontSize = 12,
                                                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                                                    FontWeight = FontWeights.Bold,
                                                    FontStyle = FontStyles.Normal,
                                                    Padding = new Thickness(10),
                                                    
                                                };
                                                main.checkboxes2.Add(newCheckBox);

                                                main.wrapPanel1.Children.Add(newCheckBox);
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

                            main.label1_Copy.Text = "Ready to scan";
                            main.buttonStartScan.IsEnabled = true;
                            main.progressBar1.Value = 0;
                            main.UpdateArc(main.progressBar1.Value);
                            cts.Cancel();
                        });

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.StackTrace);
            }



        }
    }
}
