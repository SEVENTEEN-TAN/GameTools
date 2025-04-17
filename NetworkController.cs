using MessageBox = System.Windows.MessageBox;
using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace GameTools
{
    /// <summary>
    /// 网络控制器 - 管理应用程序网络限速
    /// </summary>
    public class NetworkController : INetworkLimiter
    {
        #region 单例实现
        private static NetworkController _instance;
        public static NetworkController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new NetworkController();
                }
                return _instance;
            }
        }
        
        private NetworkController()
        {
            Initialize();
        }
        #endregion

        #region 属性和字段
        // 网络限速目标应用程序
        public ProcessInfo TargetProcess { get; private set; }
        
        // 当前限速状态
        public bool IsLimitActive { get; private set; }
        
        // 上行速度限制 (KB/s)
        public int UploadLimit { get; set; } = 100;
        
        // 下行速度限制 (KB/s)
        public int DownloadLimit { get; set; } = 100;
        
        // 保存常用进程列表
        public List<ProcessInfo> FrequentProcesses { get; private set; } = new List<ProcessInfo>();
        
        // 音频提示开关
        public bool EnableSoundNotification { get; set; } = true;
        
        // 提示音文件路径
        private string _limitOnSoundPath;
        private string _limitOffSoundPath;
        
        // 网络限速引擎 - 目前使用NetLimiter作为参考实现方案
        private INetworkLimiter _networkLimiter;

        // 是否限制网络
        private bool _isNetworkLimited;
        public bool IsNetworkLimited
        {
            get { return _isNetworkLimited; }
            set
            {
                if (_isNetworkLimited != value)
                {
                    _isNetworkLimited = value;
                    OnNetworkLimitedChanged?.Invoke(this, value);
                    SaveSettings();
                }
            }
        }

        // 上传限制(KB/s)
        private int _uploadLimitKBps;
        public int UploadLimitKBps
        {
            get { return _uploadLimitKBps; }
            set
            {
                if (_uploadLimitKBps != value)
                {
                    _uploadLimitKBps = value;
                    OnUploadLimitChanged?.Invoke(this, value);
                    SaveSettings();
                }
            }
        }

        // 下载限制(KB/s)
        private int _downloadLimitKBps;
        public int DownloadLimitKBps
        {
            get { return _downloadLimitKBps; }
            set
            {
                if (_downloadLimitKBps != value)
                {
                    _downloadLimitKBps = value;
                    OnDownloadLimitChanged?.Invoke(this, value);
                    SaveSettings();
                }
            }
        }

        // 限制的进程名称
        private string _limitedProcessName = string.Empty;
        public string LimitedProcessName
        {
            get { return _limitedProcessName; }
            set
            {
                if (_limitedProcessName != value)
                {
                    _limitedProcessName = value;
                    OnLimitedProcessNameChanged?.Invoke(this, value);
                    SaveSettings();
                }
            }
        }

        // 限制的进程ID
        private int _limitedProcessId;
        public int LimitedProcessId
        {
            get { return _limitedProcessId; }
            set
            {
                if (_limitedProcessId != value)
                {
                    _limitedProcessId = value;
                    OnLimitedProcessIdChanged?.Invoke(this, value);
                    SaveSettings();
                }
            }
        }
        #endregion

        #region 事件
        // 网络限制状态改变事件
        public event EventHandler<bool> OnNetworkLimitedChanged;

        // 上传限制改变事件
        public event EventHandler<int> OnUploadLimitChanged;

        // 下载限制改变事件
        public event EventHandler<int> OnDownloadLimitChanged;

        // 限制的进程名称改变事件
        public event EventHandler<string> OnLimitedProcessNameChanged;

        // 限制的进程ID改变事件
        public event EventHandler<int> OnLimitedProcessIdChanged;
        #endregion

        #region 初始化和配置
        private void Initialize()
        {
            try
            {
                // 确定提示音文件路径
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _limitOnSoundPath = Path.Combine(baseDir, "Sounds", "success.wav");
                _limitOffSoundPath = Path.Combine(baseDir, "Sounds", "fail.wav");
                
                // 创建声音文件目录
                Directory.CreateDirectory(Path.Combine(baseDir, "Sounds"));
                
                // 初始化网络限速引擎
                InitializeNetworkLimiter();
                
                // 加载常用进程列表
                LoadFrequentProcesses();
                
                // 日志
                Console.WriteLine("网络控制器初始化成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化网络控制器出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 初始化网络限速器
        /// </summary>
        private void InitializeNetworkLimiter()
        {
            // 使用MockNetworkLimiter实现网络限速进行测试
            // 策略模式：可以在此处更换不同的网络限速实现
            _networkLimiter = new MockNetworkLimiter();
        }
        
        private void LoadFrequentProcesses()
        {
            // 在实际实现中，这里将从配置文件加载常用进程列表
            // TODO: 实现从配置文件加载
            FrequentProcesses.Clear();
        }
        
        public void SaveSettings()
        {
            // 保存网络控制器配置到文件
            // TODO: 实现配置保存
        }
        #endregion

        #region 进程管理
        public List<ProcessInfo> GetRunningProcesses()
        {
            var processList = new List<ProcessInfo>();
            
            try
            {
                var allProcesses = Process.GetProcesses();
                
                foreach (var process in allProcesses)
                {
                    try
                    {
                        // 排除系统进程
                        if (string.IsNullOrEmpty(process.MainWindowTitle) && process.ProcessName.ToLower() != "explorer")
                            continue;
                        
                        // 创建进程信息对象
                        var processInfo = new ProcessInfo
                        {
                            Id = process.Id,
                            Name = process.ProcessName,
                            WindowTitle = process.MainWindowTitle,
                            FilePath = GetProcessFilePath(process)
                        };
                        
                        processList.Add(processInfo);
                    }
                    catch
                    {
                        // 忽略无法访问的进程
                    }
                }
                
                // 按名称排序
                processList = processList.OrderBy(p => p.Name).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取进程列表出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
            return processList;
        }
        
        private string GetProcessFilePath(Process process)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT ExecutablePath FROM Win32_Process WHERE ProcessId = {process.Id}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["ExecutablePath"]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }
            
            return string.Empty;
        }
        
        public void SetTargetProcess(ProcessInfo process)
        {
            TargetProcess = process;
            
            // 如果当前有活动的限速，则需要重新应用到新的目标进程
            if (IsLimitActive)
            {
                // 先停止当前限速
                StopNetworkLimit();
                // 再应用到新进程
                ApplyNetworkLimit();
            }
            
            // 将进程添加到常用列表（如果不存在）
            if (!FrequentProcesses.Any(p => p.Id == process.Id))
            {
                FrequentProcesses.Add(process);
                if (FrequentProcesses.Count > 10) // 限制常用列表大小
                {
                    FrequentProcesses.RemoveAt(0);
                }
                SaveSettings();
            }
        }
        #endregion

        #region 网络限速控制
        public async Task<bool> ApplyNetworkLimit()
        {
            if (TargetProcess == null)
            {
                MessageBox.Show("请先选择目标进程", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            
            try
            {
                // 检查进程是否仍在运行
                if (!IsProcessRunning(TargetProcess.Id))
                {
                    MessageBox.Show("目标进程已不在运行", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                
                // 应用网络限速，使用SetLimit方法
                bool success = await _networkLimiter.SetLimit(TargetProcess.Id, TargetProcess.Name, UploadLimit, DownloadLimit);
                
                if (success)
                {
                    IsLimitActive = true;
                    
                    // 播放提示音
                    if (EnableSoundNotification)
                    {
                        PlaySound(_limitOnSoundPath);
                    }
                }
                else
                {
                    MessageBox.Show("应用网络限速失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"应用网络限速时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<bool> StopNetworkLimit()
        {
            if (!IsLimitActive || TargetProcess == null)
            {
                return true; // 没有活动的限速，直接返回成功
            }
            
            try
            {
                // 停止网络限速
                bool success = await _networkLimiter.RemoveLimit(TargetProcess.Id);
                
                if (success)
                {
                    IsLimitActive = false;
                    
                    // 播放提示音
                    if (EnableSoundNotification)
                    {
                        PlaySound(_limitOffSoundPath);
                    }
                }
                else
                {
                    MessageBox.Show("停止网络限速失败", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"停止网络限速时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        
        public async Task<bool> ToggleNetworkLimit()
        {
            if (IsLimitActive)
            {
                return await StopNetworkLimit();
            }
            else
            {
                return await ApplyNetworkLimit();
            }
        }
        
        private bool IsProcessRunning(int processId)
        {
            try
            {
                Process.GetProcessById(processId);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region 辅助方法
        private void PlaySound(string soundPath)
        {
            try
            {
                // 记录日志
                Console.WriteLine($"正在播放声音: {soundPath}");
                
                if (File.Exists(soundPath))
                {
                    try
                    {
                        string extension = Path.GetExtension(soundPath).ToLower();
                        Console.WriteLine($"声音文件存在，文件扩展名: {extension}");
                        
                        if (extension == ".wav")
                        {
                            // 对于WAV文件使用SoundPlayer
                            Console.WriteLine("使用SoundPlayer播放WAV文件");
                            using (var player = new SoundPlayer(soundPath))
                            {
                                player.Play();
                                Console.WriteLine("WAV声音播放指令已发送");
                            }
                        }
                        else
                        {
                            // 对于其他格式(如MP3)，使用系统声音
                            Console.WriteLine($"文件格式{extension}不是WAV，使用系统声音替代");
                            if (soundPath.Contains("success"))
                            {
                                SystemSounds.Asterisk.Play();
                                Console.WriteLine("播放系统Asterisk声音");
                            }
                            else
                            {
                                SystemSounds.Hand.Play();
                                Console.WriteLine("播放系统Hand声音");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"播放声音出错: {ex.Message}");
                        // 出错时使用系统声音
                        if (soundPath.Contains("success"))
                        {
                            SystemSounds.Asterisk.Play();
                            Console.WriteLine("播放系统Asterisk声音");
                        }
                        else
                        {
                            SystemSounds.Hand.Play();
                            Console.WriteLine("播放系统Hand声音");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"声音文件不存在: {soundPath}，使用系统声音代替");
                    // 使用系统提示音
                    if (soundPath.Contains("success"))
                    {
                        SystemSounds.Asterisk.Play();
                        Console.WriteLine("播放系统Asterisk声音");
                    }
                    else
                    {
                        SystemSounds.Hand.Play();
                        Console.WriteLine("播放系统Hand声音");
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录任何错误
                Console.WriteLine($"播放声音时发生错误: {ex.Message}");
            }
        }
        #endregion

        #region INetworkLimiter 接口实现
        // 设置网络限速
        public async Task<bool> SetLimit(int processId, string processName, int uploadLimitKBps, int downloadLimitKBps)
        {
            try
            {
                // 如果之前有限速的进程，先移除限速
                if (IsNetworkLimited && LimitedProcessId > 0 && LimitedProcessId != processId)
                {
                    await RemoveLimit(LimitedProcessId);
                }

                // 更新限速状态
                IsNetworkLimited = true;
                UploadLimitKBps = uploadLimitKBps;
                DownloadLimitKBps = downloadLimitKBps;
                LimitedProcessName = processName;
                LimitedProcessId = processId;

                // 使用NetshFirewallManager创建限速规则
                var result = await NetshFirewallManager.Instance.ApplyLimits(processId, uploadLimitKBps, downloadLimitKBps);
                
                if (!result)
                {
                    // 如果设置失败，恢复状态
                    IsNetworkLimited = false;
                    LimitedProcessId = 0;
                    LimitedProcessName = string.Empty;
                    return false;
                }

                SaveSettings();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"设置网络限速时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                // 如果发生异常，恢复状态
                IsNetworkLimited = false;
                LimitedProcessId = 0;
                LimitedProcessName = string.Empty;
                return false;
            }
        }

        // 移除网络限速
        public async Task<bool> RemoveLimit(int processId)
        {
            try
            {
                // 检查是否是当前限速的进程
                if (LimitedProcessId == processId)
                {
                    bool result = await NetshFirewallManager.Instance.RemoveLimit(processId);
                    
                    if (result)
                    {
                        // 清除限速状态
                        IsNetworkLimited = false;
                        LimitedProcessId = 0;
                        LimitedProcessName = string.Empty;
                        SaveSettings();
                    }

                    return result;
                }
                else if (processId <= 0)
                {
                    // 如果进程ID无效，尝试移除当前限速
                    if (LimitedProcessId > 0)
                    {
                        bool result = await NetshFirewallManager.Instance.RemoveLimit(LimitedProcessId);
                        
                        if (result)
                        {
                            // 清除限速状态
                            IsNetworkLimited = false;
                            LimitedProcessId = 0;
                            LimitedProcessName = string.Empty;
                            SaveSettings();
                        }

                        return result;
                    }
                }

                // 如果没有找到活动的限速，返回成功
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"移除网络限速时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// 进程信息类 - 存储进程的基本信息
    /// </summary>
    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WindowTitle { get; set; }
        public string FilePath { get; set; }
        
        public override string ToString()
        {
            return string.IsNullOrEmpty(WindowTitle) 
                ? Name 
                : $"{Name} - {WindowTitle}";
        }
    }

    /// <summary>
    /// 网络限速接口 - 定义网络限速的基本操作
    /// </summary>
    public interface INetworkLimiter
    {
        /// <summary>
        /// 设置网络限速
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="processName">进程名称</param>
        /// <param name="uploadLimitKBps">上传限制(KB/s)</param>
        /// <param name="downloadLimitKBps">下载限制(KB/s)</param>
        /// <returns>是否设置成功</returns>
        Task<bool> SetLimit(int processId, string processName, int uploadLimitKBps, int downloadLimitKBps);

        /// <summary>
        /// 移除网络限速
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>是否成功移除限速</returns>
        Task<bool> RemoveLimit(int processId);
    }

    /// <summary>
    /// 模拟网络限速实现 - 用于开发和测试
    /// </summary>
    public class MockNetworkLimiter : INetworkLimiter
    {
        public Task<bool> ApplyLimit(int processId, int uploadLimitKBps, int downloadLimitKBps)
        {
            // 模拟网络限速操作
            Console.WriteLine($"模拟对进程 {processId} 应用网络限速: 上行 {uploadLimitKBps} KB/s, 下行 {downloadLimitKBps} KB/s");
            
            // 模拟操作延迟
            return Task.FromResult(true);
        }

        public Task<bool> RemoveLimit(int processId)
        {
            // 模拟移除网络限速
            Console.WriteLine($"模拟移除进程 {processId} 的网络限速");
            
            // 模拟操作延迟
            return Task.FromResult(true);
        }

        public Task<bool> SetLimit(int processId, string processName, int uploadLimitKBps, int downloadLimitKBps)
        {
            // 模拟设置网络限速
            Console.WriteLine($"模拟设置进程 {processId} 的网络限速: 上行 {uploadLimitKBps} KB/s, 下行 {downloadLimitKBps} KB/s");
            
            // 模拟操作延迟
            return Task.FromResult(true);
        }
    }
} 