using Multron_Win_Cleaner;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
namespace MultronWinCleaner.Processes
{
    public class Updater
    {
        MainWindow main;
        CancellationTokenSource cts = new CancellationTokenSource();
        public Updater(MainWindow main)
        {
            this.main = main;
        }
        public async Task dotsAsync(string text, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (main.cancelstatus.IsCancellationRequested)
                        break;
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
        public async Task downloadlatestdatabase()
        {
            dotsAsync("Checking for latest database", cts.Token);
            try
            {
                string repo = "MultronWcleaner-Database";
                string owner = "winball501";
                var client = new GitHubClient(new ProductHeaderValue("MultronWcleaner-Database"));
                var releases = await client.Repository.Release.GetAll(owner, repo);
                var latest = releases[0];
                string getversion = "1.0";
                if (File.Exists(Environment.CurrentDirectory + "\\databaseversion.txt"))
                {
                    getversion = await File.ReadAllTextAsync(Environment.CurrentDirectory + "\\databaseversion.txt");
                }
                string releaseName = latest.Name;

                var match = Regex.Match(releaseName, @"\d+\.\d+(\.\d+)?");
                if (!match.Success)
                {
                    return;
                }
                Version latestVersion = new Version(match.Value);
                Version currentVersion = new Version(getversion);
                if (latestVersion == currentVersion)
                {
                    await cts.CancelAsync();
                    return;
                }
                await File.WriteAllTextAsync(Environment.CurrentDirectory + "\\databaseversion.txt", latestVersion.ToString());
                var asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".txt"));
                var downloadFolder = Environment.CurrentDirectory + "\\Update";
                Directory.CreateDirectory(downloadFolder);
                var filePath = Path.Combine(downloadFolder, asset.Name);
                await cts.CancelAsync();
                using (var http = new HttpClient())
                using (var response = await http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file = File.Create(filePath))
                    {
                        var buffer = new byte[81920];
                        long totalRead = 0;
                        int read;

                        var lastProgress = -1;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                int progress = (int)((totalRead * 100L) / totalBytes);
                                if (progress != lastProgress)
                                {
                                    main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.label1_Copy.Text = "Downloading Latest Database " + progress.ToString() + "%";
                                        lastProgress = progress;
                                    });

                                }
                            }
                        }


                    }
                    System.IO.File.Copy(downloadFolder + "\\database.txt", Environment.CurrentDirectory + "\\database.txt", overwrite: true);
                }
            } catch (Exception ex)
            {

                await main.Dispatcher.InvokeAsync(async() =>
                {
                    await cts.CancelAsync();
                    main.label1_Copy.Text = ex.Message;
                });
            }
            
        }
        public async Task checkforapplicationupdates()
        {
            dotsAsync("Checking For Application Updates", cts.Token);
            try
            {
                string repo = "MultronWcleaner";
                string owner = "winball501";
                var client = new GitHubClient(new ProductHeaderValue("MultronWcleaner"));
                var releases = await client.Repository.Release.GetAll(owner, repo);
                var latest = releases[0];


                string releaseName = latest.Name;

                var match = Regex.Match(releaseName, @"\d+\.\d+(\.\d+)?");
                if (!match.Success)
                {
                    return;
                }

                Version latestVersion = new Version(match.Value);
                Version currentVersion = new Version("1.20.4");

                if (latestVersion == currentVersion)
                {

                    await cts.CancelAsync();
                    return;
                }
                var asset = latest.Assets.FirstOrDefault(a => a.Name.EndsWith(".zip"));
                var downloadFolder = Environment.CurrentDirectory + "\\Update";
                Directory.CreateDirectory(downloadFolder);
                var filePath = Path.Combine(downloadFolder, asset.Name);
                await cts.CancelAsync();
                using (var http = new HttpClient())
                using (var response = await http.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var file = File.Create(filePath))
                    {
                        var buffer = new byte[81920];
                        long totalRead = 0;
                        int read;

                        var lastProgress = -1;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await file.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                int progress = (int)((totalRead * 100L) / totalBytes);
                                if (progress != lastProgress)
                                {
                                    main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.label1_Copy.Text = "Downloading Update " + progress.ToString() + "%";
                                        lastProgress = progress;
                                    });

                                }
                            }
                        }


                    }
                }
                using (ZipArchive archive = ZipFile.OpenRead(filePath))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(downloadFolder, entry.FullName));

                         if (string.IsNullOrEmpty(entry.Name))
                        {
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        { 
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                             
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
                File.Delete(filePath);
                string updater = Environment.CurrentDirectory + "\\Updater.exe";
                if (File.Exists(updater))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.CurrentDirectory + "\\Updater.exe",
                        UseShellExecute = true
                    });
                    Environment.Exit(0);
                }
                else
                {
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.label1_Copy.Text = "Updater.exe not found";
                    });
                }
           
            } catch (Exception ex)
            {
                await main.Dispatcher.InvokeAsync(async() =>
                {
                    await cts.CancelAsync();
                    main.label1_Copy.Text = ex.Message;
                });
                
            }
         
        }
        public async Task run()
        {
            await checkforapplicationupdates();
            await downloadlatestdatabase();
        }
    }
}
