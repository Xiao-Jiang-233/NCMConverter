using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NCMConverter
{
    public static class LibDownloader
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/taurusxin/ncmdump/releases/latest";
        private const string DLL_FILENAME = "libncmdump.dll";
        private const string VERSION_FILENAME = "lib_version.txt";
        private const string DOWNLOAD_ZIP_PATTERN = "libncmdump-";

        private static string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
        private static string LocalDllPath => Path.Combine(AppDirectory, DLL_FILENAME);
        private static string LocalVersionPath => Path.Combine(AppDirectory, VERSION_FILENAME);

        public static async Task<bool> CheckAndUpdateLibraryAsync(Action<string>? log = null)
        {
            log ??= _ => { };
            log("开始检查核心库更新...");
            string currentVersion = ReadLocalVersion();
            log($"本地版本: {currentVersion}");
            JsonElement? remoteReleaseInfo = null;
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NCMConverter/1.0");
                var json = await client.GetStringAsync(GITHUB_API_URL);
                remoteReleaseInfo = JsonDocument.Parse(json).RootElement;
            }
            catch (Exception ex)
            {
                log($"网络错误: 无法获取远程版本信息。{ex.Message}");
                if (File.Exists(LocalDllPath))
                {
                    log("将使用本地已有的DLL文件。");
                    return true;
                }
                else
                {
                    log("网络错误且本地无可用DLL，启动失败。");
                    return false;
                }
            }
            if (remoteReleaseInfo.HasValue)
            {
                string latestVersionTag = remoteReleaseInfo.Value.GetProperty("tag_name").GetString() ?? "0.0.0";
                log($"最新版本: {latestVersionTag}");
                if (IsVersionNewer(latestVersionTag, currentVersion))
                {
                    log("发现新版本，准备执行更新流程。");
                    string? downloadUrl = null;
                    foreach (var asset in remoteReleaseInfo.Value.GetProperty("assets").EnumerateArray())
                    {
                        var name = asset.GetProperty("name").GetString();
                        if (name != null && name.StartsWith(DOWNLOAD_ZIP_PATTERN) && name.EndsWith(".zip"))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        log("错误: 在最新版本中未找到匹配的下载文件。");
                        return true;
                    }
                    bool updateSuccessful = await PerformAtomicUpdate(downloadUrl, latestVersionTag, log);
                    if (updateSuccessful)
                    {
                        log("核心库更新成功！");
                        return true;
                    }
                    else
                    {
                        log("核心库更新失败。");
                        if (File.Exists(LocalDllPath))
                            return true;
                        else
                            return false;
                    }
                }
                else
                {
                    log("本地版本已是最新，无需更新。");
                    return true;
                }
            }
            return true;
        }

        private static async Task<bool> PerformAtomicUpdate(string url, string newVersion, Action<string> log)
        {
            string tempZipPath = Path.GetTempFileName();
            string tempDllPath = Path.GetTempFileName();
            try
            {
                log($"正在下载到临时文件: {tempZipPath}");
                using (var client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(tempZipPath, data);
                }
                log($"正在解压到临时文件: {tempDllPath}");
                using (var archive = ZipFile.OpenRead(tempZipPath))
                {
                    var entry = archive.GetEntry(DLL_FILENAME);
                    if (entry == null)
                        throw new Exception($"ZIP包中未找到 {DLL_FILENAME}");
                    entry.ExtractToFile(tempDllPath, true);
                }
                if (!FileIsValid(tempDllPath))
                    throw new Exception("下载的DLL文件已损坏。");
                log("正在用新文件替换旧文件...");
                File.Copy(tempDllPath, LocalDllPath, true);
                log("更新版本记录文件...");
                File.WriteAllText(LocalVersionPath, newVersion);
                return true;
            }
            catch (Exception ex)
            {
                log($"原子更新过程中发生错误: {ex.Message}");
                return false;
            }
            finally
            {
                log("清理临时文件...");
                TryDelete(tempZipPath);
                TryDelete(tempDllPath);
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static string ReadLocalVersion()
        {
            if (File.Exists(LocalVersionPath) && File.Exists(LocalDllPath))
                return File.ReadAllText(LocalVersionPath).Trim();
            else
                return "0.0.0";
        }

        private static bool IsVersionNewer(string newVersion, string oldVersion)
        {
            string v1 = newVersion.TrimStart('v', 'V');
            string v2 = oldVersion.TrimStart('v', 'V');
            return new Version(v1) > new Version(v2);
        }

        private static bool FileIsValid(string path)
        {
            try
            {
                var fi = new FileInfo(path);
                if (fi.Length < 1024) return false;
                using var fs = File.OpenRead(path);
                byte[] header = new byte[2];
                fs.Read(header, 0, 2);
                return header[0] == 'M' && header[1] == 'Z'; // PE头
            }
            catch { return false; }
        }
    }
}
