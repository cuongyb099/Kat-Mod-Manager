using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int ProgressCallback(int percentage, double speed);

namespace KATTHEDEV
{
    public partial class MainWindow : Window
    {
        [DllImport("KatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool DownloadAndInstallNative(
    string modId, string token, string email, string rootPath, string tag, ProgressCallback callback);

        [DllImport("KatDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool UninstallModNative(
            string modId, string token, string rootPath, string tag, string folderName);

        private const string AuthWorkerUrl = "https://orange-rain-70ca.cuonghyhy0999.workers.dev";
        private const string DownloadWorkerUrl = "https://restless-shadow-7b0c.cuonghyhy0999.workers.dev";
        private const string ClientId = "5Vi58Er3EXKF8V-_g_jgyy8Ng6Kj1bVtMO-zWdp0aTA-IxJQLLgIxL8_EPoTCLRJ";
        private string secureConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kat_secure.dat");

        private string _currentEmail = "", _accessToken = "", _xxmiRootPath = "", _wwmiPath = "", _efmiPath = "", _currentLang = "en";
        private ObservableCollection<string> _queueIds = new ObservableCollection<string>();
        private List<ModItem> _allMods = new List<ModItem>();
        private CancellationTokenSource? _cts;
        private bool _isDownloading = false;
        private string _lastGameTag = "wuthering_waves"; // Mặc định là WuWa
        private Dictionary<string, Dictionary<string, string>> _dict = new Dictionary<string, Dictionary<string, string>>()
        {
            ["en"] = new Dictionary<string, string> { { "settings", "⚙️ SETTINGS" }, { "queue", "QUEUE" }, { "logout", "LOGOUT" }, { "ready", "System Ready" }, { "set_title", "APP SETTINGS" }, { "path_title", "XXMI ROOT FOLDER" }, { "browse", "BROWSE" }, { "lang", "LANGUAGE" }, { "save", "CLOSE & SAVE" }, { "miss_title", "MISSING XXMI PATH" }, { "miss_desc", "Please select XXMI root folder to continue." }, { "miss_btn", "SELECT XXMI FOLDER" }, { "login_member", "MEMBER ACCESS" }, { "login_btn", "LOGIN WITH PATREON" }, { "login_status", "Please login to access your mods" }, { "st_activate", "ACTIVATE" }, { "st_installed", "INSTALLED" }, { "st_update", "UPDATE" }, { "st_down", "DOWNLOADING..." }, { "st_del", "🗑 DELETE MOD" }, { "tab_store", "STORE" }, { "tab_lib", "LIBRARY" }, { "st_info", "🔗 PATREON POST" }, { "wwmi_path", "CUSTOM WWMI PATH (OPTIONAL)" }, { "efmi_path", "CUSTOM EFMI PATH (OPTIONAL)" }, { "update_all", "UPDATE ALL" }, { "search", "🔍 Search mods..." } },
            ["vi"] = new Dictionary<string, string> { { "settings", "⚙️ CÀI ĐẶT" }, { "queue", "HÀNG CHỜ" }, { "logout", "ĐĂNG XUẤT" }, { "ready", "Hệ thống sẵn sàng" }, { "set_title", "CÀI ĐẶT ỨNG DỤNG" }, { "path_title", "THƯ MỤC GỐC XXMI" }, { "browse", "CHỌN" }, { "lang", "NGÔN NGỮ" }, { "save", "ĐÓNG & LƯU" }, { "miss_title", "THIẾU ĐƯỜNG DẪN" }, { "miss_desc", "Vui lòng chọn thư mục gốc XXMI để tiếp tục." }, { "miss_btn", "CHỌN THƯ MỤC XXMI" }, { "login_member", "TRUY CẬP THÀNH VIÊN" }, { "login_btn", "ĐĂNG NHẬP PATREON" }, { "login_status", "Vui lòng đăng nhập để sử dụng" }, { "st_activate", "KÍCH HOẠT" }, { "st_installed", "ĐÃ CÀI ĐẶT" }, { "st_update", "CẬP NHẬT" }, { "st_down", "ĐANG TẢI..." }, { "st_del", "🗑 XÓA MOD" }, { "tab_store", "CỬA HÀNG" }, { "tab_lib", "THƯ VIỆN" }, { "st_info", "🔗 XEM BÀI VIẾT" }, { "wwmi_path", "ĐƯỜNG DẪN WWMI RIÊNG (TÙY CHỌN)" }, { "efmi_path", "ĐƯỜNG DẪN EFMI RIÊNG (TÙY CHỌN)" }, { "update_all", "CẬP NHẬT TẤT CẢ" }, { "search", "🔍 Tìm kiếm mod..." } },
            ["zh"] = new Dictionary<string, string> { { "settings", "⚙️ 设置" }, { "queue", "队列" }, { "logout", "登出" }, { "ready", "系统就绪" }, { "set_title", "应用设置" }, { "path_title", "XXMI 根目录" }, { "browse", "浏览" }, { "lang", "语言" }, { "save", "关闭并保存" }, { "miss_title", "缺少路径" }, { "miss_desc", "请选择 XXMI 根文件夹以继续。" }, { "miss_btn", "选择 XXMI 文件夹" }, { "login_member", "会员访问" }, { "login_btn", "通过 PATREON 登录" }, { "login_status", "请登录以访问您的模组" }, { "st_activate", "激活" }, { "st_installed", "已安装" }, { "st_update", "更新" }, { "st_down", "下载中..." }, { "st_del", "🗑 删除模组" }, { "tab_store", "商店" }, { "tab_lib", "库" }, { "st_info", "🔗 查看详情" }, { "wwmi_path", "自定义 WWMI 路径 (可选)" }, { "efmi_path", "自定义 EFMI 路径 (可选)" }, { "update_all", "全部更新" }, { "search", "🔍 搜索模组..." } }
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            itemsQueue.ItemsSource = _queueIds;

            this.Loaded += async (s, e) => {
                UpdateLang();
                foreach (ComboBoxItem item in cboLang.Items)
                    if (item.Tag?.ToString() == _currentLang) { cboLang.SelectedItem = item; break; }

                foreach (ComboBoxItem item in cboGames.Items)
                {
                    if (item.Tag?.ToString() == _lastGameTag)
                    {
                        cboGames.SelectedItem = item;
                        break;
                    }
                }

                // --- BẬT BẢNG CHỌN ĐƯỜNG DẪN NẾU CHƯA CÓ ---
                if (string.IsNullOrEmpty(_xxmiRootPath))
                {
                    pnlPathMissing.Visibility = Visibility.Visible;
                }
                else
                {
                    pnlPathMissing.Visibility = Visibility.Collapsed;
                }

                if (!string.IsNullOrEmpty(_currentEmail) && !string.IsNullOrEmpty(_accessToken))
                {
                    pnlLogin.Visibility = Visibility.Collapsed;
                    lblUserInfo.Text = $"User: {_currentEmail}";

                    if (!string.IsNullOrEmpty(_xxmiRootPath))
                    {
                        await RefreshMods();
                    }
                }
            };
        }

        // ==========================================
        // HÀM DÒ ĐƯỜNG DẪN ĐA NĂNG
        // ==========================================
        private string GetManagerRootPath(string tag)
        {
            // 1. ƯU TIÊN ĐƯỜNG DẪN CUSTOM NẾU CÓ
            if (tag == "wuthering_waves" && !string.IsNullOrEmpty(_wwmiPath) && Directory.Exists(_wwmiPath))
                return _wwmiPath;
            if (tag != "wuthering_waves" && !string.IsNullOrEmpty(_efmiPath) && Directory.Exists(_efmiPath))
                return _efmiPath;

            // 2. NẾU KHÔNG CÓ, LỤC LÕI TRONG JSON CỦA XXMI
            string importerKey = tag == "wuthering_waves" ? "WWMI" : "EFMI";
            string targetFolder = importerKey; // Mặc định là tên thư mục
            string configPath = Path.Combine(_xxmiRootPath, "XXMI Launcher Config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(configPath));
                    if (doc.RootElement.TryGetProperty("Importers", out var impEl) &&
                        impEl.TryGetProperty(importerKey, out var gameEl) &&
                        gameEl.TryGetProperty("Importer", out var innerImpEl) &&
                        innerImpEl.TryGetProperty("importer_folder", out var folderEl))
                    {
                        string f = folderEl.GetString();
                        if (!string.IsNullOrEmpty(f)) targetFolder = f.Trim('/', '\\');
                    }
                }
                catch { }
            }

            if (Path.IsPathRooted(targetFolder)) return targetFolder; // Nếu JSON là ổ đĩa khác
            return Path.Combine(_xxmiRootPath, targetFolder); // Nếu JSON là thư mục con của XXMI
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
        private void tabMain_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (this.IsLoaded) ApplyFilter(); }

        private void ApplyFilter()
        {
            if (_allMods == null) return;
            string s = txtSearch.Text.ToLower().Trim(); bool isLib = tabMain.SelectedIndex == 1;
            lstMods.ItemsSource = _allMods.Where(m => (string.IsNullOrEmpty(s) || m.display_name.ToLower().Contains(s) || m.mod_id.ToLower().Contains(s)) && (!isLib || m.isInstalled)).ToList();
        }

        private void UpdateLang()
        {
            var d = _dict[_currentLang];
            btnSettings.Content = d["settings"]; btnQueue.Content = d["queue"]; btnLogout.Content = d["logout"]; lblSettingsTitle.Text = d["set_title"]; lblPathTitle.Text = d["path_title"]; btnBrowse.Content = d["browse"]; lblLangTitle.Text = d["lang"]; btnSaveSettings.Content = d["save"]; lblStatusMain.Text = d["ready"]; tabStore.Header = d["tab_store"]; tabLibrary.Header = d["tab_lib"];
            lblWwmiPathTitle.Text = d["wwmi_path"];
            lblEfmiPathTitle.Text = d["efmi_path"];
            if (lblSearchPlaceholder != null) lblSearchPlaceholder.Text = d.GetValueOrDefault("search", "🔍 Search mods...");
            btnUpdateAll.Content = d.GetValueOrDefault("update_all", "UPDATE ALL");
            if (_allMods != null) foreach (var m in _allMods) m.UpdateTexts(d);
            ApplyFilter();
        }

        private async void btnUpdateAll_Click(object sender, RoutedEventArgs e)
        {
            if (_allMods == null || !_allMods.Any()) return;

            // 1. Quét tìm tất cả các Mod đang có trạng thái Cập Nhật (st_update)
            var modsToUpdate = _allMods.Where(m => m.StatusKey == "st_update").ToList();

            if (modsToUpdate.Count == 0)
            {
                string msg = _currentLang == "vi" ? "Tất cả Mod đã ở phiên bản mới nhất!"
                           : (_currentLang == "zh" ? "所有模组已是最新版本！"
                           : "All mods are up to date!");
                MessageBox.Show(msg, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int addedCount = 0;
            foreach (var m in modsToUpdate)
            {
                // 2. Chặn trùng lặp: Nếu Mod chưa có trong hàng chờ và chưa bị bấm tải
                if (!_queueIds.Contains(m.mod_id) && m.StatusKey != "st_down")
                {
                    _queueIds.Add(m.mod_id); // Nhét vào hàng chờ

                    // Đổi giao diện thẻ Mod thành Đang tải...
                    m.StatusKey = "st_down";
                    m.UpdateTexts(_dict[_currentLang]);
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                // 3. Mở bảng Queue lên cho khách xem tiến trình vã Mod
                brdQueue.Visibility = Visibility.Visible;

                // 4. Đề pa máy: Nếu hệ thống đang rảnh thì kích hoạt hàm ProcessQueue chạy luôn!
                if (!_isDownloading)
                {
                    await ProcessQueue();
                }
            }
        }

        private async Task RefreshMods()
        {
            if (string.IsNullOrEmpty(_accessToken) || string.IsNullOrEmpty(_xxmiRootPath)) return;

            string tag = (cboGames.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wuthering_waves";
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-KATT-AUTH", _accessToken);

            try
            {
                var url = $"{DownloadWorkerUrl}?action=list&email={_currentEmail}&hwid={Environment.MachineName}&game={tag}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Forbidden || response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        string msg = _currentLang == "vi" ? "Phiên đăng nhập không hợp lệ hoặc bạn đang đăng nhập ở máy khác.\nVui lòng đăng nhập lại!"
                                   : (_currentLang == "zh" ? "登录会话已失效或在其他设备上登录。\n请重新登录！"
                                   : "Invalid session or logged in on another device.\nPlease login again!");

                        string title = _currentLang == "vi" ? "Xác thực thất bại" : (_currentLang == "zh" ? "验证失败" : "Auth Failed");
                        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);

                        _currentEmail = ""; _accessToken = ""; SaveConfig();
                        if (File.Exists(secureConfigPath)) File.Delete(secureConfigPath);
                        pnlLogin.Visibility = Visibility.Visible;
                        return;
                    }

                    string srvErr = _currentLang == "vi" ? $"Server trả về lỗi: {response.StatusCode}"
                                  : (_currentLang == "zh" ? $"服务器错误: {response.StatusCode}"
                                  : $"Server returned error: {response.StatusCode}");
                    MessageBox.Show(srvErr, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                using var doc = JsonDocument.Parse(jsonString);
                if (doc.RootElement.TryGetProperty("mods", out var modsElement))
                {
                    var mods = JsonSerializer.Deserialize<List<ModItem>>(modsElement.GetRawText(), options) ?? new List<ModItem>();
                    string vPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Versions");
                    var d = _dict[_currentLang];

                    foreach (var m in mods)
                    {
                        string f = Path.Combine(vPath, $"{m.mod_id}.ver");
                        m.isInstalled = File.Exists(f);
                        m.StatusKey = m.isInstalled ? (File.ReadAllText(f) == m.version ? "st_installed" : "st_update") : "st_activate";
                        m.UpdateTexts(d);
                    }
                    _allMods = mods; ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                string connErr = _currentLang == "vi" ? "Lỗi kết nối: " : (_currentLang == "zh" ? "连接错误: " : "Connection error: ");
                MessageBox.Show(connErr + ex.Message, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is ModItem m)
            {
                // 1. CHẶN ĐỨNG: Nếu Mod đang được tải hoặc đã có trong hàng chờ rồi thì cấm bấm!
                if (m.StatusKey == "st_down" || _queueIds.Contains(m.mod_id)) return;

                // 2. Nhét Mod vào cuối hàng chờ
                _queueIds.Add(m.mod_id);

                // 3. Khóa nút ngay lập tức để khách khỏi spam
                m.StatusKey = "st_down";
                m.UpdateTexts(_dict[_currentLang]);

                // 4. Nếu hệ thống đang rảnh (không có Mod nào đang tải), thì nổ máy chạy luôn!
                if (!_isDownloading)
                {
                    await ProcessQueue();
                }
            }
        }

        private async Task ProcessQueue()
        {
            // 1. Kiểm tra xem còn ai xếp hàng không
            if (_queueIds.Count == 0)
            {
                _isDownloading = false;
                pnlProgress.Visibility = Visibility.Collapsed;
                return;
            }

            _isDownloading = true;

            // 2. Lấy ID thằng đầu tiên ra NHƯNG KHÔNG XÓA VỘI (để bảng Queue vẫn hiện)
            string nextId = _queueIds[0];

            var m = _allMods.FirstOrDefault(x => x.mod_id == nextId);
            if (m == null)
            {
                _queueIds.RemoveAt(0); // Lỗi không tìm thấy thì mới xóa rác
                await ProcessQueue();
                return;
            }

            // 3. Hiển thị UI tải
            pnlProgress.Visibility = Visibility.Visible;
            lblCurrentMod.Text = m.display_name;
            prgDownload.Value = 0;
            _cts = new CancellationTokenSource();

            try
            {
                // Vít ga tải
                await DoDownload(m, _cts.Token);
                m.StatusKey = "st_installed";
                m.isInstalled = true;
            }
            catch
            {
                // Tải lỗi hoặc bị hủy
                m.StatusKey = "st_activate";
            }
            finally
            {
                m.UpdateTexts(_dict[_currentLang]);
                ApplyFilter();

                // 4. TẢI XONG XUÔI RỒI MỚI ĐÁ KHỎI HÀNG CHỜ
                if (_queueIds.Contains(nextId))
                {
                    _queueIds.Remove(nextId);
                }

                // 5. Chạy tiếp thằng đằng sau
                await ProcessQueue();
            }
        }

        // BỔ SUNG: Nâng cấp luôn hàm bấm nút X (Xóa khỏi Queue) cho khỏi sinh lỗi
        private void btnRemoveFromQueue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                string idToRemove = b.Tag?.ToString() ?? "";

                // TH1: Khách bấm X trúng cái Mod đang tải ở Top 1 -> Bóp cổ bắt hủy tải luôn!
                if (_queueIds.Count > 0 && _queueIds[0] == idToRemove)
                {
                    _cts?.Cancel(); // Lệnh này sẽ quăng lỗi, nhảy xuống finally của ProcessQueue và tự xóa khỏi Queue
                }
                else // TH2: Xóa cái Mod đang xếp hàng chờ ở phía dưới
                {
                    _queueIds.Remove(idToRemove);

                    // Trả lại trạng thái cho Mod bên ngoài Cửa hàng
                    var m = _allMods.FirstOrDefault(x => x.mod_id == idToRemove);
                    if (m != null)
                    {
                        m.StatusKey = "st_activate";
                        m.UpdateTexts(_dict[_currentLang]);
                    }
                }
            }
        }

        private async Task StartQueue(ModItem m)
        {
            _isDownloading = true; m.StatusKey = "st_down"; m.UpdateTexts(_dict[_currentLang]);
            pnlProgress.Visibility = Visibility.Visible; lblCurrentMod.Text = m.display_name; prgDownload.Value = 0; _cts = new CancellationTokenSource();
            try { await DoDownload(m, _cts.Token); m.StatusKey = "st_installed"; m.isInstalled = true; }
            catch { m.StatusKey = "st_activate"; }
            finally { m.UpdateTexts(_dict[_currentLang]); _isDownloading = false; if (_queueIds.Count > 0) { var n = _allMods.FirstOrDefault(x => x.mod_id == _queueIds[0]); _queueIds.RemoveAt(0); if (n != null) await StartQueue(n); } else pnlProgress.Visibility = Visibility.Collapsed; ApplyFilter(); }
        }

        private async Task DoDownload(ModItem mod, CancellationToken token)
        {
            string tag = (cboGames.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wuthering_waves";
            string rootPath = GetManagerRootPath(tag);

            ProgressCallback progressHandler = (percent, speed) => {
                if (_cts != null && _cts.IsCancellationRequested) return 0;

                Dispatcher.Invoke(() => {
                    prgDownload.Value = percent;
                    lblPercentage.Text = $"{percent}%";
                    lblSpeed.Text = $"{speed:F2} MB/s";
                    if (percent >= 100)
                    {
                        lblStatusMain.Text = _currentLang == "vi" ? "Đang hoàn tất xử lý..."
                                           : (_currentLang == "zh" ? "正在处理..."
                                           : "Processing final steps...");
                    }
                });
                return 1;
            };

            await Task.Run(() => {
                bool success = DownloadAndInstallNative(mod.mod_id, _accessToken, _currentEmail, rootPath, tag, progressHandler);

                if (!success)
                {
                    if (_cts != null && _cts.IsCancellationRequested)
                    {
                        throw new OperationCanceledException("Cancelled by user");
                    }
                    else
                    {
                        string errMsg = _currentLang == "vi" ? "Lỗi cài đặt! Sếp mở file 'katt_dll_error.log' ở thư mục chạy để bắt bệnh nhé."
                                      : (_currentLang == "zh" ? "安装失败！请查看目录下的 'katt_dll_error.log' 了解详细信息。"
                                      : "Install failed! Please check 'katt_dll_error.log' in the root folder.");

                        Dispatcher.Invoke(() => MessageBox.Show(errMsg, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Error));
                        throw new Exception("Native DLL Error");
                    }
                }
                else
                {
                    // Update the status bar success message
                    Dispatcher.Invoke(() => {
                        lblStatusMain.Text = _currentLang == "vi" ? "Cài đặt thành công!"
                                           : (_currentLang == "zh" ? "安装成功！"
                                           : "Installation successful!");
                    });
                }
            }, token);
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is ModItem m)
            {
                string msg = _currentLang == "vi" ? $"Xác nhận xóa Mod: {m.display_name}?"
                           : (_currentLang == "zh" ? $"确定要删除模组: {m.display_name} 吗？"
                           : $"Confirm delete Mod: {m.display_name}?");

                string title = _currentLang == "vi" ? "Xác nhận" : (_currentLang == "zh" ? "确认" : "Confirm");

                if (MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string tag = (cboGames.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wuthering_waves";
                        string rootPath = GetManagerRootPath(tag);

                        string vP = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Versions");
                        string folderFile = Path.Combine(vP, $"{m.mod_id}.folder");
                        string folderName = File.Exists(folderFile) ? File.ReadAllText(folderFile) : "";

                        bool deleted = UninstallModNative(m.mod_id, _accessToken, rootPath, tag, folderName);

                        if (deleted)
                        {
                            if (File.Exists(folderFile)) File.Delete(folderFile);
                            string verFile = Path.Combine(vP, $"{m.mod_id}.ver");
                            if (File.Exists(verFile)) File.Delete(verFile);

                            m.isInstalled = false;
                            m.StatusKey = "st_activate";
                            m.UpdateTexts(_dict[_currentLang]);
                            ApplyFilter();

                            lblStatusMain.Text = _currentLang == "vi" ? "Đã xóa mod thành công!"
                                               : (_currentLang == "zh" ? "模组删除成功！"
                                               : "Mod deleted successfully!");
                        }
                        else
                        {
                            string errDel = _currentLang == "vi" ? "Xóa mod thất bại! Check file 'katt_dll_uninstall_error.log' xem nó vướng quyền Admin hay đang mở game không nhé."
                                          : (_currentLang == "zh" ? "删除失败！请查看 'katt_dll_uninstall_error.log'，确保游戏未在运行或具有管理员权限。"
                                          : "Delete failed! Check 'katt_dll_uninstall_error.log' (ensure the game is closed and app has Admin rights).");
                            MessageBox.Show(errDel, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        string sysErr = _currentLang == "vi" ? "Lỗi hệ thống khi xóa: " : (_currentLang == "zh" ? "删除时发生系统错误: " : "System error on delete: ");
                        MessageBox.Show(sysErr + ex.Message, "KatTDev", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private async void btnBrowseWwmi_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true)
            {
                _wwmiPath = d.FolderName;
                txtWwmiPath.Text = _wwmiPath;
                SaveConfig();
                await RefreshMods();
            }
        }

        private async void btnBrowseEfmi_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true)
            {
                _efmiPath = d.FolderName;
                txtEfmiPath.Text = _efmiPath;
                SaveConfig();
                await RefreshMods();
            }
        }

        private void btnOpenPatreon_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string u && !string.IsNullOrEmpty(u))
                Process.Start(new ProcessStartInfo(u) { UseShellExecute = true });
        }


        private void SaveConfig()
        {
            try
            {
                // Lấy Tag của game đang chọn hiện tại
                string selectedGame = (cboGames.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wuthering_waves";

                var c = new Dictionary<string, string> {
            { "path", _xxmiRootPath },
            { "email", _currentEmail },
            { "token", _accessToken },
            { "lang", _currentLang },
            { "wwmi", _wwmiPath },
            { "efmi", _efmiPath },
            { "game", selectedGame } // <-- THÊM DÒNG NÀY ĐỂ LƯU GAME
        };
                File.WriteAllBytes(secureConfigPath, ProtectedData.Protect(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(c)), null, DataProtectionScope.CurrentUser));
            }
            catch { }
        }

        private void LoadConfig()
        {
            if (!File.Exists(secureConfigPath)) return;
            try
            {
                byte[] dec = ProtectedData.Unprotect(File.ReadAllBytes(secureConfigPath), null, DataProtectionScope.CurrentUser);
                var c = JsonSerializer.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(dec));
                if (c != null)
                {
                    _xxmiRootPath = c.GetValueOrDefault("path", "");
                    _currentEmail = c.GetValueOrDefault("email", "");
                    _accessToken = c.GetValueOrDefault("token", "");
                    _currentLang = c.GetValueOrDefault("lang", "en");
                    _wwmiPath = c.GetValueOrDefault("wwmi", "");
                    _efmiPath = c.GetValueOrDefault("efmi", "");
                    _lastGameTag = c.GetValueOrDefault("game", "wuthering_waves"); // <-- LOAD GAME LÊN
                }
            }
            catch { }
        }

        private void btnOpenSettings_Click(object sender, RoutedEventArgs e)
        {
            txtPathDisplay.Text = _xxmiRootPath;
            txtWwmiPath.Text = _wwmiPath;
            txtEfmiPath.Text = _efmiPath;
            pnlSettings.Visibility = Visibility.Visible;
        }

        // Nhớ thêm chữ 'async' sếp nhé
        private async void btnCloseSettings_Click(object sender, RoutedEventArgs e)
        {
            pnlSettings.Visibility = Visibility.Collapsed;

            // Hứng dữ liệu khách vừa gõ hoặc Paste vào TextBox
            _xxmiRootPath = txtPathDisplay.Text.Trim();
            _wwmiPath = txtWwmiPath.Text.Trim();
            _efmiPath = txtEfmiPath.Text.Trim();

            SaveConfig();

            // Load lại Mod luôn lỡ khách vừa đổi đường dẫn
            await RefreshMods();
        }

        private async void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true)
            {
                _xxmiRootPath = d.FolderName;
                SaveConfig();
                pnlPathMissing.Visibility = Visibility.Collapsed;
                txtPathDisplay.Text = _xxmiRootPath;
                await RefreshMods();
            }
        }

        private async void btnLoginPatreon_Click(object sender, RoutedEventArgs e)
        {
            string h = $"{Environment.MachineName}-KATT";
            string scopes = "identity identity[email] identity.memberships";

            Process.Start(new ProcessStartInfo($"https://www.patreon.com/oauth2/authorize?response_type=code&client_id={ClientId}&redirect_uri={AuthWorkerUrl}/auth/callback&scope={Uri.EscapeDataString(scopes)}&state={h}") { UseShellExecute = true });
            btnLoginPatreon.IsEnabled = false; prgLogin.Visibility = Visibility.Visible; using var c = new HttpClient();
            for (int i = 0; i < 40; i++) { await Task.Delay(3000); try { var res = await c.GetAsync($"{AuthWorkerUrl}/check-auth?hwid={h}"); if (res.IsSuccessStatusCode) { var d = JsonSerializer.Deserialize<Dictionary<string, string>>(await res.Content.ReadAsStringAsync()); if (d != null && d["status"] == "success") { _currentEmail = d["email"]; _accessToken = d["token"]; SaveConfig(); pnlLogin.Visibility = Visibility.Collapsed; lblUserInfo.Text = $"User: {_currentEmail}"; await RefreshMods(); break; } } } catch { } }
            btnLoginPatreon.IsEnabled = true; prgLogin.Visibility = Visibility.Collapsed;
        }
      
        private void btnLogout_Click(object sender, RoutedEventArgs e) { _currentEmail = ""; _accessToken = ""; SaveConfig(); File.Delete(secureConfigPath); pnlLogin.Visibility = Visibility.Visible; }
        private void btnToggleQueue_Click(object sender, RoutedEventArgs e) => brdQueue.Visibility = brdQueue.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        private void btnCancelCurrent_Click(object sender, RoutedEventArgs e) => _cts?.Cancel();
        private async void cboGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                SaveConfig(); // Lưu lại ngay khi đổi game
                await RefreshMods();
            }
        }
        private void cboLang_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (this.IsLoaded) { _currentLang = (cboLang.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "en"; UpdateLang(); } }
    }

    public class ModItem : INotifyPropertyChanged
    {
        public string mod_id { get; set; } = ""; public string display_name { get; set; } = ""; public string patreon_url { get; set; } = ""; public string img_url { get; set; } = ""; public string version { get; set; } = ""; public string StatusKey { get; set; } = "st_activate";
        private string _btnT = "", _delT = "", _infoT = ""; public string btnText { get => _btnT; set { _btnT = value; OnProp("btnText"); } }
        public string deleteText { get => _delT; set { _delT = value; OnProp("deleteText"); } }
        public string infoText { get => _infoT; set { _infoT = value; OnProp("infoText"); } }
        private bool _isI = false; public bool isInstalled { get => _isI; set { _isI = value; OnProp("isInstalled"); } }
        public void UpdateTexts(Dictionary<string, string> d) { btnText = d.GetValueOrDefault(StatusKey, "ACTIVATE"); deleteText = d.GetValueOrDefault("st_del", "DELETE"); infoText = d.GetValueOrDefault("st_info", "INFO"); }
        public event PropertyChangedEventHandler PropertyChanged; void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}