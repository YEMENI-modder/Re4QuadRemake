using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Re4QuadExtremeEditor.src.Forms;

namespace Re4QuadExtremeEditor.src.Class
{
    /// <summary>
    /// Checks GitHub Releases for a newer version (compared against this assembly's
    /// AssemblyVersion, i.e. the "vX.X.X.X" tag on GitHub), and, if found, offers an in-app
    /// update: downloads the "Re4QuadRemake.zip" asset, extracts it, stages the new .exe as
    /// "<exe>.new" alongside a "Change to new Version.bat" that performs the actual swap once
    /// the app is closed (the running .exe can't overwrite itself).
    /// </summary>
    public static class UpdateChecker
    {
        private const string RepoOwner = "YEMENI-modder";
        private const string RepoName = "Re4QuadRemake";
        private const string ApiUrl = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";
        private const string AssetFileName = "Re4QuadRemake.zip";
        private const string ZipInnerFolderName = "Re4QuadRemake";

        /// <summary>
        /// Runs the check in the background and, if an update is available, shows the update
        /// dialog on the UI thread. Never throws outward (network failure/parsing errors are
        /// simply ignored, same as before).
        /// </summary>
        public static async void CheckForUpdatesAsync(Form ownerForm)
        {
            try
            {
                ReleaseInfo release = await GetLatestReleaseInfoAsync();
                if (release == null || release.Version == null) return;

                Version current = Assembly.GetExecutingAssembly().GetName().Version;

                if (release.Version > current)
                {
                    if (ownerForm != null && ownerForm.IsHandleCreated)
                    {
                        ownerForm.BeginInvoke((MethodInvoker)(() => OfferUpdate(ownerForm, release, current)));
                    }
                    else
                    {
                        OfferUpdate(ownerForm, release, current);
                    }
                }
            }
            catch
            {
                // no internet, GitHub API rate limit, repo unavailable, etc: fail silently
            }
        }

        private class ReleaseInfo
        {
            public Version Version;
            public string TagName;
            public string DownloadUrl;
            public long SizeBytes;
        }

        private static async Task<ReleaseInfo> GetLatestReleaseInfoAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Re4QuadRemake-UpdateChecker");

                string json = await client.GetStringAsync(ApiUrl);
                JObject obj = JObject.Parse(json);
                string tag = obj["tag_name"]?.ToString();
                if (string.IsNullOrEmpty(tag)) return null;

                // strip common "v"/"V" prefix (e.g. "v1.2.0.0" -> "1.2.0.0")
                string versionText = tag.TrimStart('v', 'V');
                if (!Version.TryParse(versionText, out Version parsedVersion))
                {
                    return null;
                }

                JArray assets = obj["assets"] as JArray;
                if (assets == null) return null;

                foreach (var assetToken in assets)
                {
                    string name = assetToken["name"]?.ToString();
                    if (string.Equals(name, AssetFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        string downloadUrl = assetToken["browser_download_url"]?.ToString();
                        long size = assetToken["size"]?.ToObject<long>() ?? 0;

                        if (string.IsNullOrEmpty(downloadUrl)) return null;

                        return new ReleaseInfo
                        {
                            Version = parsedVersion,
                            TagName = tag,
                            DownloadUrl = downloadUrl,
                            SizeBytes = size
                        };
                    }
                }

                // release exists but doesn't have the expected asset yet
                return null;
            }
        }

        private static async void OfferUpdate(Form ownerForm, ReleaseInfo release, Version current)
        {
            using (UpdateAvailableForm dialog = new UpdateAvailableForm(current.ToString(), release.Version.ToString(), release.SizeBytes))
            {
                DialogResult result = ownerForm != null ? dialog.ShowDialog(ownerForm) : dialog.ShowDialog();

                if (result != DialogResult.OK || !dialog.UserChoseUpdate)
                {
                    return;
                }
            }

            await PerformUpdateAsync(ownerForm, release);
        }

        private static async Task PerformUpdateAsync(Form ownerForm, ReleaseInfo release)
        {
            UpdateProgressForm progressForm = new UpdateProgressForm();
            if (ownerForm != null && ownerForm.IsHandleCreated)
            {
                ownerForm.BeginInvoke((MethodInvoker)(() => progressForm.Show(ownerForm)));
            }
            else
            {
                progressForm.Show();
            }

            string tempZipPath = null;
            string tempExtractDir = null;

            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                string exeDir = Path.GetDirectoryName(exePath);
                string exeFileName = Path.GetFileName(exePath);

                tempZipPath = Path.Combine(Path.GetTempPath(), "Re4QuadRemake_Update_" + Guid.NewGuid().ToString("N") + ".zip");
                tempExtractDir = Path.Combine(Path.GetTempPath(), "Re4QuadRemake_Update_" + Guid.NewGuid().ToString("N"));

                // 1) download the zip
                progressForm.SetStatus("Downloading update...");
                await DownloadFileAsync(release.DownloadUrl, tempZipPath, progressForm);

                // 2) extract it
                progressForm.SetStatus("Extracting update...");
                progressForm.SetProgress(0);
                Directory.CreateDirectory(tempExtractDir);
                ZipFile.ExtractToDirectory(tempZipPath, tempExtractDir);

                // the zip is expected to contain a single top-level folder, e.g. "Re4QuadRemake\..."
                string sourceRoot = Path.Combine(tempExtractDir, ZipInnerFolderName);
                if (!Directory.Exists(sourceRoot))
                {
                    // fall back to the extraction root itself, in case the zip has no wrapper folder
                    sourceRoot = tempExtractDir;
                }

                // 3) copy everything over the current installation, except the running .exe itself,
                //    which is staged as "<exe>.new" instead (can't overwrite a running executable).
                progressForm.SetStatus("Installing update...");
                string stagedExePath = Path.Combine(exeDir, exeFileName + ".new");
                CopyDirectoryStagingExe(sourceRoot, exeDir, exeFileName, stagedExePath);

                // 4) create "Change to new Version.bat" next to the .exe
                progressForm.SetStatus("Finishing up...");
                string batPath = Path.Combine(exeDir, "Change to new Version.bat");
                CreateSwapBatFile(batPath, exeFileName, exeDir);

                progressForm.Close();

                MessageBox.Show(
                    "The update has been downloaded.\n\nPlease close the application and run \"Change to new Version.bat\" to finish updating.",
                    "Update Ready",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressForm.Close();
                MessageBox.Show(
                    "The update could not be completed:\n\n" + ex.Message,
                    "Update Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                TryDelete(tempZipPath);
                TryDeleteDirectory(tempExtractDir);
            }
        }

        private static async Task DownloadFileAsync(string url, string destinationPath, UpdateProgressForm progressForm)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Re4QuadRemake-UpdateChecker");

                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (Stream httpStream = await response.Content.ReadAsStreamAsync())
                    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[81920];
                        long totalRead = 0;
                        int bytesRead;

                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;

                            if (totalBytes.HasValue && totalBytes.Value > 0)
                            {
                                int percent = (int)((totalRead * 100) / totalBytes.Value);
                                progressForm.SetProgress(percent);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recursively copies sourceDir into destinationDir. Every file is copied/overwritten
        /// normally, except one named exactly exeFileName (case-insensitive) sitting at the
        /// source root, which is instead copied to stagedExePath (i.e. "<exe>.new") so the
        /// currently-running executable is never touched directly.
        /// </summary>
        private static void CopyDirectoryStagingExe(string sourceDir, string destinationDir, string exeFileName, string stagedExePath)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);

                if (string.Equals(fileName, exeFileName, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(filePath, stagedExePath, true);
                    continue;
                }

                string destFilePath = Path.Combine(destinationDir, fileName);
                File.Copy(filePath, destFilePath, true);
            }

            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dirPath);
                string destSubDir = Path.Combine(destinationDir, dirName);

                // exeFileName only ever appears at the source root, so subfolders are copied as-is.
                CopyDirectoryStagingExe(dirPath, destSubDir, exeFileName, stagedExePath);
            }
        }

        /// <summary>
        /// Writes a .bat file that deletes the current .exe and renames "<exe>.new" to take its
        /// place, once the app has been closed. Includes a short retry loop in case the process
        /// hasn't fully released the file handle yet.
        /// </summary>
        private static void CreateSwapBatFile(string batPath, string exeFileName, string exeDir)
        {
            string content =
                "@echo off" + Environment.NewLine +
                "cd /d \"" + exeDir + "\"" + Environment.NewLine +
                ":retry" + Environment.NewLine +
                "del \"" + exeFileName + "\"" + Environment.NewLine +
                "if exist \"" + exeFileName + "\" (" + Environment.NewLine +
                "    timeout /t 1 /nobreak >nul" + Environment.NewLine +
                "    goto retry" + Environment.NewLine +
                ")" + Environment.NewLine +
                "ren \"" + exeFileName + ".new\" \"" + exeFileName + "\"" + Environment.NewLine +
                "start \"\" \"" + exeFileName + "\"" + Environment.NewLine +
                "del \"%~f0\"" + Environment.NewLine;

            File.WriteAllText(batPath, content, System.Text.Encoding.ASCII);
        }

        private static void TryDelete(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path); } catch (Exception) { }
        }

        private static void TryDeleteDirectory(string path)
        {
            try { if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) Directory.Delete(path, true); } catch (Exception) { }
        }
    }
}
