using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace KatSecurityCore
{
    public class NativeInstaller
    {
        private const string SecretSalt = "KATT_PRO_MODDER_9999_@_YEN_BAI";

        [UnmanagedCallersOnly(EntryPoint = "DownloadAndInstallNative")]
        public static unsafe bool DownloadAndInstallNative(
            IntPtr modIdPtr, IntPtr tokenPtr, IntPtr emailPtr, IntPtr rootPathPtr, IntPtr tagPtr,
            delegate* unmanaged<int, double, int> progressCallback)
        {
            try
            {
                string modId = Marshal.PtrToStringAnsi(modIdPtr) ?? "";
                string token = Marshal.PtrToStringAnsi(tokenPtr) ?? "";
                string email = Marshal.PtrToStringAnsi(emailPtr) ?? "";
                string rootPath = Marshal.PtrToStringAnsi(rootPathPtr) ?? "";
                string tag = Marshal.PtrToStringAnsi(tagPtr) ?? "";

                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-KATT-AUTH", token);

                // 1. Tải Zip Mod
                string zipUrl = $"https://restless-shadow-7b0c.cuonghyhy0999.workers.dev?action=download&filetype=zip&mod={modId}&email={email}";
                var zipRes = client.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
                if (!zipRes.IsSuccessStatusCode) return false;

                long totalBytes = zipRes.Content.Headers.ContentLength ?? 1024 * 1024;
                using var stream = zipRes.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                string tempZip = Path.Combine(Path.GetTempPath(), $"{modId}.zip");

                var startTime = DateTime.Now;
                using (var fs = new FileStream(tempZip, FileMode.Create))
                {
                    byte[] buffer = new byte[8192];
                    long totalRead = 0;
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, read);
                        totalRead += read;
                        if (progressCallback != null)
                        {
                            double elapsedSec = (DateTime.Now - startTime).TotalSeconds;
                            double speed = elapsedSec > 0 ? (totalRead / elapsedSec / 1024.0 / 1024.0) : 0;

                            // GỌI UI VÀ CHỜ CÂU TRẢ LỜI
                            int shouldContinue = progressCallback((int)((totalRead * 100) / totalBytes), speed);

                            // NẾU UI TRẢ VỀ 0 LÀ LỆNH HỦY!
                            if (shouldContinue == 0)
                            {
                                fs.Dispose(); // Đóng file ngay
                                if (File.Exists(tempZip)) File.Delete(tempZip); // Xóa file đang tải dở
                                return false; // Thoát hàm lập tức
                            }
                        }
                    }
                }

                // 2. Tải file bản quyền thật
                string iniUrl = $"https://restless-shadow-7b0c.cuonghyhy0999.workers.dev?action=download&filetype=ini&mod={modId}&email={email}";
                byte[] iniData = client.GetByteArrayAsync(iniUrl).GetAwaiter().GetResult();

                // 3. TÍNH TOÁN ĐƯỜNG DẪN ẨN & DỌN RÁC BẢN CŨ TRƯỚC KHI GIẢI NÉN
                using var sha = SHA256.Create();
                var hDirName = "." + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(tag + "h_meta" + SecretSalt + token))).Replace("-", "").ToLower().Substring(0, 12);
                var hFileName = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(modId + "f" + SecretSalt + token))).Replace("-", "").ToLower().Substring(0, 12) + ".ini";

                string hDir = Path.Combine(rootPath, "..", hDirName);
                string hFile = Path.Combine(hDir, hFileName);

                string modsFolder = Path.Combine(rootPath, "Mods");
                string vPath = Path.Combine(AppContext.BaseDirectory, "Versions");
                string folderLogPath = Path.Combine(vPath, $"{modId}.folder");

                // Dọn thư mục Mod game cũ (Nếu là Update)
                if (File.Exists(folderLogPath))
                {
                    string oldFolderName = File.ReadAllText(folderLogPath);
                    if (!string.IsNullOrEmpty(oldFolderName))
                    {
                        string oldModPath = Path.Combine(modsFolder, oldFolderName);
                        if (Directory.Exists(oldModPath)) Directory.Delete(oldModPath, true);
                    }
                }

                // Dọn thư mục ẩn cũ
                if (Directory.Exists(hDir)) Directory.Delete(hDir, true);

                // 4. BẮT ĐẦU GIẢI NÉN BẢN MỚI
                string folderName = "";
                using (ZipArchive archive = ZipFile.OpenRead(tempZip))
                {
                    folderName = archive.Entries.FirstOrDefault(e => e.FullName.Contains("/"))?.FullName.Split('/')[0] ?? "";
                    archive.ExtractToDirectory(modsFolder, true);
                }

                // 5. TẠO LẠI THƯ MỤC ẨN VÀ LƯU FILE BẢN QUYỀN
                var di = Directory.CreateDirectory(hDir);
                di.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                File.WriteAllBytes(hFile, iniData);

                // ========================================================
                // 6. MA TRẬN CHAOS 4.1 - FIX LOGIC INJECT CHUẨN 3DMIGOTO
                // ========================================================
                string fullModPath = Path.Combine(modsFolder, folderName);
                if (Directory.Exists(fullModPath))
                {
                    var allIniFiles = Directory.GetFiles(fullModPath, "*.ini", SearchOption.AllDirectories);
                    if (allIniFiles.Length > 0)
                    {
                        Random rnd = new Random();
                        int realFileIndex = rnd.Next(allIniFiles.Length);
                        string[] fakeFolders = { "..\\..\\TextureCache", "..\\ShaderFixes", "..\\..\\..\\.sys_cache", "..\\Materials", "..\\..\\..\\.backup", "..\\..\\..\\.3dmigoto", ".." };

                        foreach (var targetIniPath in allIniFiles)
                        {
                            var lines = File.ReadAllLines(targetIniPath).ToList();

                            // 6.1 Nhét Link Thật vào đúng chỗ (Chỉ cho 1 file duy nhất)
                            if (targetIniPath == allIniFiles[realFileIndex])
                            {
                                string relReal = Uri.UnescapeDataString(new Uri(targetIniPath).MakeRelativeUri(new Uri(hFile)).ToString().Replace('/', '\\'));

                                int existingIncludeIndex = lines.FindIndex(l => l.Trim().Equals("[Include]", StringComparison.OrdinalIgnoreCase));

                                if (existingIncludeIndex >= 0)
                                {
                                    // Chèn ngay dưới chữ [Include] có sẵn
                                    lines.Insert(existingIncludeIndex + 1, $"include = {relReal}");
                                }
                                else
                                {
                                    // Tạo mới ở đầu file
                                    lines.Insert(0, "");
                                    lines.Insert(0, $"include = {relReal}");
                                    lines.Insert(0, "[Include]");
                                }
                            }

                            // 6.2 Nã 10-15 Link Rác (Tạo section [Include] riêng và chèn TRƯỚC các section khác)
                            int junkCount = rnd.Next(10, 16);
                            for (int j = 0; j < junkCount; j++)
                            {
                                string randomFolder = fakeFolders[rnd.Next(fakeFolders.Length)];
                                string randomFileName = Guid.NewGuid().ToString("N").Substring(0, 12) + ".ini";
                                string junkLine = $"include = {randomFolder}\\{randomFileName}";

                                var sectionIndices = Enumerable.Range(0, lines.Count)
                                                               .Where(i => lines[i].Trim().StartsWith("[") && lines[i].Trim().EndsWith("]"))
                                                               .ToList();

                                if (sectionIndices.Count > 0)
                                {
                                    // Chèn TRƯỚC section có sẵn để tránh làm hỏng nó
                                    int targetIndex = sectionIndices[rnd.Next(sectionIndices.Count)];
                                    lines.Insert(targetIndex, "");
                                    lines.Insert(targetIndex + 1, "[Include]");
                                    lines.Insert(targetIndex + 2, junkLine);
                                }
                                else
                                {
                                    lines.Add("");
                                    lines.Add("[Include]");
                                    lines.Add(junkLine);
                                }
                            }

                            File.WriteAllLines(targetIniPath, lines);
                        }
                    }
                }

                if (File.Exists(tempZip)) File.Delete(tempZip);

                Directory.CreateDirectory(vPath);
                File.WriteAllText(folderLogPath, folderName);

                if (progressCallback != null) progressCallback(100, 0.0);
                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText("katt_dll_error.log", ex.ToString());
                return false;
            }
        }

        [UnmanagedCallersOnly(EntryPoint = "UninstallModNative")]
        public static unsafe bool UninstallModNative(IntPtr modIdPtr, IntPtr tokenPtr, IntPtr rootPathPtr, IntPtr tagPtr, IntPtr folderNamePtr)
        {
            try
            {
                string modId = Marshal.PtrToStringAnsi(modIdPtr) ?? "";
                string token = Marshal.PtrToStringAnsi(tokenPtr) ?? "";
                string rootPath = Marshal.PtrToStringAnsi(rootPathPtr) ?? "";
                string tag = Marshal.PtrToStringAnsi(tagPtr) ?? "";
                string folderName = Marshal.PtrToStringAnsi(folderNamePtr) ?? "";

                using var sha = SHA256.Create();
                var hDirName = "." + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(tag + "h_meta" + SecretSalt + token))).Replace("-", "").ToLower().Substring(0, 12);
                var hFileName = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(modId + "f" + SecretSalt + token))).Replace("-", "").ToLower().Substring(0, 12) + ".ini";

                string hDir = Path.Combine(rootPath, "..", hDirName);
                string hFile = Path.Combine(hDir, hFileName);

                if (File.Exists(hFile)) File.Delete(hFile);

                if (Directory.Exists(hDir))
                {
                    var filesInHDir = Directory.GetFiles(hDir, "*.ini");
                    foreach (var f in filesInHDir) File.Delete(f);

                    if (!Directory.EnumerateFileSystemEntries(hDir).Any())
                    {
                        var di = new DirectoryInfo(hDir);
                        di.Attributes &= ~(FileAttributes.Hidden | FileAttributes.System);
                        di.Delete();
                    }
                }

                if (!string.IsNullOrEmpty(folderName))
                {
                    string fullModPath = Path.Combine(rootPath, "Mods", folderName);
                    if (Directory.Exists(fullModPath))
                    {
                        Directory.Delete(fullModPath, true);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                File.WriteAllText("katt_dll_uninstall_error.log", ex.ToString());
                return false;
            }
        }
    }
}