using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// 解决命名冲突
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace GameTools
{
    /// <summary>
    /// NetworkControlWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NetworkControlWindow : Window
    {
        private NetworkController _networkController;
        private List<ProcessInfo> _allProcesses;
        private Dictionary<int, BitmapSource> _processIcons = new Dictionary<int, BitmapSource>();
        
        // 过滤器
        private string _searchFilter = string.Empty;

        public NetworkControlWindow()
        {
            InitializeComponent();
            
            // 注册窗口加载事件
            Loaded += NetworkControlWindow_Loaded;
            
            // 注册窗口关闭事件
            Closing += NetworkControlWindow_Closing;
        }

        private void NetworkControlWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 获取网络控制器实例
                if (_networkController == null)
                {
                    _networkController = NetworkController.Instance;
                    Console.WriteLine("网络控制器实例获取成功");
                }
                
                // 初始化界面
                InitializeUI();
                Console.WriteLine("界面初始化完成");
                
                // 显示加载中状态
                StatusText.Text = "状态: 正在加载进程列表...";
                LoadingPanel.Visibility = Visibility.Visible; // 显示加载指示器
                
                // 在后台线程加载进程列表，避免UI卡顿
                Task.Run(async () =>
                {
                    try
                    {
                        // 获取所有进程
                        var processes = _networkController.GetRunningProcesses();
                        
                        // 回到UI线程更新界面
                        await Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                _allProcesses = processes;
                                Console.WriteLine($"获取到{_allProcesses.Count}个进程");
                                
                                // 应用过滤器
                                ApplyProcessFilter();
                                
                                // 加载常用进程
                                LoadFrequentProcesses();
                                Console.WriteLine("常用进程加载完成");
                                
                                // 更新状态
                                UpdateStatusText();
                                StatusText.Text = "状态: 进程列表加载完成";
                                Console.WriteLine("状态文本更新完成");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"在UI线程更新进程列表时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            finally
                            {
                                LoadingPanel.Visibility = Visibility.Collapsed; // 隐藏加载指示器
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show($"在后台加载进程列表时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusText.Text = "状态: 加载进程列表失败";
                            
                            // 确保_allProcesses不为空，以防后续使用时发生异常
                            if (_allProcesses == null)
                            {
                                _allProcesses = new List<ProcessInfo>();
                            }
                            
                            LoadingPanel.Visibility = Visibility.Collapsed; // 隐藏加载指示器
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化网络控制窗口时出错: {ex.Message}\n\n堆栈跟踪:\n{ex.StackTrace}", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"异常: {ex.Message}\n{ex.StackTrace}");
                LoadingPanel.Visibility = Visibility.Collapsed; // 隐藏加载指示器
            }
        }

        private void NetworkControlWindow_Closing(object sender, CancelEventArgs e)
        {
            // 窗口关闭时保存设置
            _networkController.SaveSettings();
        }

        private void InitializeUI()
        {
            try
            {
                // 确保控制器实例不为空
                if (_networkController == null)
                {
                    _networkController = NetworkController.Instance;
                    Console.WriteLine("InitializeUI中获取网络控制器实例");
                }
                
                // 设置初始值
                UploadSlider.Value = _networkController.UploadLimit;
                DownloadSlider.Value = _networkController.DownloadLimit;
                UploadTextBox.Text = _networkController.UploadLimit.ToString();
                DownloadTextBox.Text = _networkController.DownloadLimit.ToString();
                
                // 设置声音提示复选框状态
                SoundNotificationCheckBox.IsChecked = _networkController.EnableSoundNotification;
                
                // 更新当前选中进程信息
                UpdateSelectedProcessInfo(_networkController.TargetProcess);
                
                Console.WriteLine("InitializeUI完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化UI时出错: {ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"InitializeUI异常: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void LoadProcessList()
        {
            try
            {
                // 显示加载中消息
                StatusText.Text = "状态: 正在加载进程列表...";
                Console.WriteLine("开始加载进程列表");
                
                // 确保控制器实例不为空
                if (_networkController == null)
                {
                    _networkController = NetworkController.Instance;
                    Console.WriteLine("LoadProcessList中获取网络控制器实例");
                }
                
                // 获取所有进程
                _allProcesses = _networkController.GetRunningProcesses();
                Console.WriteLine($"获取到{_allProcesses.Count}个进程");
                
                // 应用过滤器
                ApplyProcessFilter();
                
                StatusText.Text = "状态: 进程列表加载完成";
                Console.WriteLine("进程列表加载完成");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载进程列表出错: {ex.Message}\n\n{ex.StackTrace}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"LoadProcessList异常: {ex.Message}\n{ex.StackTrace}");
                StatusText.Text = "状态: 加载进程列表失败";
                
                // 确保_allProcesses不为空，以防后续使用时发生异常
                if (_allProcesses == null)
                {
                    _allProcesses = new List<ProcessInfo>();
                }
            }
        }

        private void ApplyProcessFilter()
        {
            // 清除当前列表
            ProcessListBox.Items.Clear();
            
            // 过滤进程
            var filteredProcesses = _allProcesses;
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                string filter = _searchFilter.ToLower();
                filteredProcesses = _allProcesses.Where(p => 
                    p.Name.ToLower().Contains(filter) || 
                    p.WindowTitle.ToLower().Contains(filter)).ToList();
            }
            
            // 添加到列表框
            foreach (var process in filteredProcesses)
            {
                ProcessListBox.Items.Add(process);
            }
            
            // 如果当前有选中的进程，在列表中选中它
            if (_networkController.TargetProcess != null)
            {
                foreach (ProcessInfo process in ProcessListBox.Items)
                {
                    if (process.Id == _networkController.TargetProcess.Id)
                    {
                        ProcessListBox.SelectedItem = process;
                        break;
                    }
                }
            }
            
            // 在后台批量加载图标，减少UI线程负担
            Task.Run(async () => 
            {
                var processesToLoadIcons = filteredProcesses
                    .Where(p => !_processIcons.ContainsKey(p.Id) && 
                           !string.IsNullOrEmpty(p.FilePath) && 
                           File.Exists(p.FilePath))
                    .ToList();
                
                foreach (var process in processesToLoadIcons)
                {
                    try
                    {
                        await LoadProcessIconAsync(process);
                    }
                    catch
                    {
                        // 忽略单个图标加载失败
                    }
                }
            });
        }

        private async Task LoadProcessIconAsync(ProcessInfo process)
        {
            try
            {
                // 创建图标对象
                using (Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(process.FilePath))
                {
                    if (icon != null)
                    {
                        // 创建位图源
                        BitmapSource iconSource = await Task.Run(() =>
                        {
                            return Imaging.CreateBitmapSourceFromHIcon(
                                icon.Handle, 
                                Int32Rect.Empty, 
                                BitmapSizeOptions.FromEmptyOptions());
                        });
                        
                        // 更新图标缓存
                        await Dispatcher.InvokeAsync(() =>
                        {
                            _processIcons[process.Id] = iconSource;
                            
                            // 更新UI
                            UpdateProcessIcon(process.Id, iconSource);
                        });
                    }
                }
            }
            catch
            {
                // 忽略图标加载错误
            }
        }

        private void UpdateProcessIcon(int processId, BitmapSource iconSource)
        {
            // 查找所有代表该进程的项目
            foreach (ProcessInfo process in ProcessListBox.Items)
            {
                if (process.Id == processId)
                {
                    var container = ProcessListBox.ItemContainerGenerator.ContainerFromItem(process) as ListBoxItem;
                    if (container != null)
                    {
                        var icon = FindChildByName<System.Windows.Controls.Image>(container, "ProcessIcon");
                        if (icon != null)
                        {
                            icon.Source = iconSource;
                        }
                    }
                }
            }
        }

        private T FindChildByName<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;
            
            T foundChild = null;
            
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                // 如果子元素是我们要找的类型并且名称匹配
                T childType = child as T;
                if (childType != null)
                {
                    FrameworkElement frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                
                // 递归查找
                foundChild = FindChildByName<T>(child, childName);
                
                if (foundChild != null)
                    break;
            }
            
            return foundChild;
        }

        private void LoadFrequentProcesses()
        {
            // 清空面板
            FrequentProcessPanel.Children.Clear();
            
            // 添加常用进程按钮
            foreach (var process in _networkController.FrequentProcesses)
            {
                var button = new Button
                {
                    Content = process.Name,
                    Tag = process,
                    Margin = new Thickness(2),
                    Padding = new Thickness(5, 2, 5, 2)
                };
                
                button.Click += FrequentProcessButton_Click;
                FrequentProcessPanel.Children.Add(button);
            }
        }

        private void UpdateSelectedProcessInfo(ProcessInfo process)
        {
            if (process != null)
            {
                SelectedProcessText.Text = process.ToString();
                ProcessPathText.Text = $"路径: {process.FilePath}";
                
                // 更新限速按钮文本
                ToggleLimitButton.Content = _networkController.IsLimitActive ? "停止限速" : "启用限速";
            }
            else
            {
                SelectedProcessText.Text = "未选择进程";
                ProcessPathText.Text = "路径: ";
                ToggleLimitButton.Content = "启用限速";
            }
        }

        private void UpdateStatusText()
        {
            if (_networkController.IsLimitActive && _networkController.TargetProcess != null)
            {
                StatusText.Text = $"状态: 正在限制 {_networkController.TargetProcess.Name} (上行: {_networkController.UploadLimit} KB/s, 下行: {_networkController.DownloadLimit} KB/s)";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                StatusText.Text = "状态: 未限速";
                StatusText.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        #region 事件处理
        
        // 搜索框事件
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "搜索应用程序...")
            {
                SearchTextBox.Text = string.Empty;
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                SearchTextBox.Text = "搜索应用程序...";
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text != "搜索应用程序...")
            {
                _searchFilter = SearchTextBox.Text;
                ApplyProcessFilter();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchTextBox.Focus();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // 显示加载中状态
            StatusText.Text = "状态: 正在刷新进程列表...";
            LoadingPanel.Visibility = Visibility.Visible; // 显示加载指示器
            
            // 禁用刷新按钮，防止重复点击
            RefreshButton.IsEnabled = false;
            
            // 在后台线程加载进程列表
            Task.Run(async () =>
            {
                try
                {
                    // 获取所有进程
                    var processes = _networkController.GetRunningProcesses();
                    
                    // 回到UI线程更新界面
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            _allProcesses = processes;
                            Console.WriteLine($"刷新获取到{_allProcesses.Count}个进程");
                            
                            // 应用过滤器
                            ApplyProcessFilter();
                            
                            StatusText.Text = "状态: 进程列表刷新完成";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"刷新进程列表时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            StatusText.Text = "状态: 刷新进程列表失败";
                        }
                        finally
                        {
                            // 恢复刷新按钮
                            RefreshButton.IsEnabled = true;
                            LoadingPanel.Visibility = Visibility.Collapsed; // 隐藏加载指示器
                        }
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"刷新进程列表时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                        StatusText.Text = "状态: 刷新进程列表失败";
                        
                        // 恢复刷新按钮
                        RefreshButton.IsEnabled = true;
                        LoadingPanel.Visibility = Visibility.Collapsed; // 隐藏加载指示器
                    });
                }
            });
        }

        // 进程选择事件
        private void ProcessListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProcess = ProcessListBox.SelectedItem as ProcessInfo;
            if (selectedProcess != null)
            {
                _networkController.SetTargetProcess(selectedProcess);
                UpdateSelectedProcessInfo(selectedProcess);
            }
        }

        private void FrequentProcessButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && button.Tag is ProcessInfo process)
            {
                // 检查进程是否仍在运行
                if (IsProcessRunning(process.Id))
                {
                    _networkController.SetTargetProcess(process);
                    UpdateSelectedProcessInfo(process);
                    
                    // 在列表中选中该进程
                    ProcessListBox.SelectedItem = ProcessListBox.Items.Cast<ProcessInfo>()
                        .FirstOrDefault(p => p.Id == process.Id);
                }
                else
                {
                    MessageBox.Show($"进程 {process.Name} 已不在运行", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // 从常用列表中移除
                    _networkController.FrequentProcesses.Remove(process);
                    LoadFrequentProcesses();
                }
            }
        }

        // 速度设置事件
        private void UploadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && UploadTextBox != null)
            {
                int value = (int)e.NewValue;
                UploadTextBox.Text = value.ToString();
                _networkController.UploadLimit = value;
                
                // 如果当前正在限速，更新限速设置
                if (_networkController.IsLimitActive)
                {
                    UpdateNetworkLimit();
                }
            }
        }

        private void DownloadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && DownloadTextBox != null)
            {
                int value = (int)e.NewValue;
                DownloadTextBox.Text = value.ToString();
                _networkController.DownloadLimit = value;
                
                // 如果当前正在限速，更新限速设置
                if (_networkController.IsLimitActive)
                {
                    UpdateNetworkLimit();
                }
            }
        }

        private void UploadTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateUploadLimitFromTextBox();
        }

        private void DownloadTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateDownloadLimitFromTextBox();
        }

        private void SpeedTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox == UploadTextBox)
                {
                    UpdateUploadLimitFromTextBox();
                }
                else if (textBox == DownloadTextBox)
                {
                    UpdateDownloadLimitFromTextBox();
                }
                
                // 移除焦点
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(textBox), null);
                e.Handled = true;
            }
        }

        private void UpdateUploadLimitFromTextBox()
        {
            if (int.TryParse(UploadTextBox.Text, out int value))
            {
                // 限制范围
                value = Math.Max(0, Math.Min(10000, value));
                UploadTextBox.Text = value.ToString();
                UploadSlider.Value = value;
                _networkController.UploadLimit = value;
                
                // 如果当前正在限速，更新限速设置
                if (_networkController.IsLimitActive)
                {
                    UpdateNetworkLimit();
                }
            }
            else
            {
                // 恢复原值
                UploadTextBox.Text = ((int)UploadSlider.Value).ToString();
            }
        }

        private void UpdateDownloadLimitFromTextBox()
        {
            if (int.TryParse(DownloadTextBox.Text, out int value))
            {
                // 限制范围
                value = Math.Max(0, Math.Min(10000, value));
                DownloadTextBox.Text = value.ToString();
                DownloadSlider.Value = value;
                _networkController.DownloadLimit = value;
                
                // 如果当前正在限速，更新限速设置
                if (_networkController.IsLimitActive)
                {
                    UpdateNetworkLimit();
                }
            }
            else
            {
                // 恢复原值
                DownloadTextBox.Text = ((int)DownloadSlider.Value).ToString();
            }
        }

        // 预设按钮事件
        private void LowPresetButton_Click(object sender, RoutedEventArgs e)
        {
            UploadSlider.Value = 50;
            DownloadSlider.Value = 50;
        }

        private void MediumPresetButton_Click(object sender, RoutedEventArgs e)
        {
            UploadSlider.Value = 200;
            DownloadSlider.Value = 200;
        }

        private void HighPresetButton_Click(object sender, RoutedEventArgs e)
        {
            UploadSlider.Value = 500;
            DownloadSlider.Value = 500;
        }

        private void CustomPresetButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开自定义预设对话框（TODO: 实现）
            MessageBox.Show("自定义预设功能即将推出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UnlimitedButton_Click(object sender, RoutedEventArgs e)
        {
            // 设置为非常高的值
            UploadSlider.Value = 0;
            DownloadSlider.Value = 0;
        }

        // 声音设置
        private void SoundNotificationCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_networkController != null)
            {
                _networkController.EnableSoundNotification = SoundNotificationCheckBox.IsChecked ?? true;
            }
        }

        // 控制按钮
        private async void ToggleLimitButton_Click(object sender, RoutedEventArgs e)
        {
            // 禁用按钮防止重复点击
            ToggleLimitButton.IsEnabled = false;
            
            try
            {
                await ToggleNetworkLimit();
            }
            finally
            {
                // 恢复按钮
                ToggleLimitButton.IsEnabled = true;
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var helpText = "网络限速功能使用说明:\n\n" +
                "1. 从列表中选择需要限速的应用程序\n" +
                "2. 调整上行和下行速度限制滑块\n" +
                "3. 点击「启用限速」按钮应用设置\n\n" +
                "快捷键:\n" +
                "Alt+N - 打开网络控制面板\n" +
                "Alt+L - 快速启用/禁用网络限制\n\n" +
                "注意: 部分应用可能需要管理员权限才能正常限速";
            
            MessageBox.Show(helpText, "帮助", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        #endregion

        #region 辅助方法
        
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
        
        private async Task ToggleNetworkLimit()
        {
            bool success = await _networkController.ToggleNetworkLimit();
            if (success)
            {
                // 更新UI
                ToggleLimitButton.Content = _networkController.IsLimitActive ? "停止限速" : "启用限速";
                UpdateStatusText();
            }
        }
        
        private async void UpdateNetworkLimit()
        {
            if (_networkController.IsLimitActive)
            {
                // 停止当前限速
                await _networkController.StopNetworkLimit();
                
                // 重新应用限速
                await _networkController.ApplyNetworkLimit();
                
                // 更新状态文本
                UpdateStatusText();
            }
        }
        
        #endregion
    }
} 