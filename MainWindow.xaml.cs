using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Hardcodet.Wpf.TaskbarNotification;

namespace GameTools;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private HotkeyManager? _hotkeyManager;
    private CrosshairSettings _settings;
    private SettingsWindow? _settingsWindow;
    private bool _isVisible = true;
    private TaskbarIcon? _notifyIcon;
    
    // 准星位置偏移
    private double _xOffset = 0;
    private double _yOffset = 0;
    
    // 位置微调步长（像素）
    private const double PositionAdjustStep = 1.0;

    // Win32 API 常量和方法
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int GWL_EXSTYLE = (-20);
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
    
    // 获取屏幕分辨率
    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    // 准星渲染 - 静态类无需实例化
    // private readonly CrosshairRenderer _crosshairRenderer = new();
    // 窗口位置计时器
    private DispatcherTimer _positionTimer;
    // 鼠标位置是否被锁定
    private bool _isPositionLocked = false;
    // 准星是否可见
    private bool _isCrosshairVisible = true;

    // 新增: 打开网络控制窗口
    private NetworkControlWindow? _networkControlWindow;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // 获取设置实例
        _settings = CrosshairSettings.Instance;
        
        // 窗口属性设置
        this.WindowStyle = WindowStyle.None;
        this.AllowsTransparency = true;
        this.Topmost = true;
        this.ShowInTaskbar = false;
        
        // 设置窗口尺寸和位置
        this.Width = 50;  // 小窗口，仅显示准星
        this.Height = 50; // 小窗口，仅显示准星
        
        // 初始化计时器
        _positionTimer = new DispatcherTimer();
        _positionTimer.Interval = TimeSpan.FromMilliseconds(100); // 100ms更新一次
        _positionTimer.Tick += (s, e) => UpdateCrosshairPosition();
        
        // 窗口加载完成后设置热键和准星
        Loaded += Window_Loaded;
        
        // 窗口关闭前保存设置并注销热键
        Closing += MainWindow_Closing;

        // 初始化系统托盘图标
        InitializeNotifyIcon();
    }

    private void InitializeNotifyIcon()
    {
        try
        {
            _notifyIcon = new TaskbarIcon();
            _notifyIcon.IconSource = System.Windows.Application.Current.FindResource("AppIconResource") as ImageSource;
            _notifyIcon.ToolTipText = "游戏准星工具";
            _notifyIcon.ContextMenu = System.Windows.Application.Current.FindResource("TrayIconContextMenu") as ContextMenu;
            _notifyIcon.TrayMouseDoubleClick += NotifyIcon_TrayLeftMouseDoubleClick;
            
            // 绑定上下文菜单项的点击事件
            if (_notifyIcon.ContextMenu != null)
            {
                // 查找并绑定设置菜单项
                var settingsMenuItem = LogicalTreeHelper.FindLogicalNode(_notifyIcon.ContextMenu, "SettingsMenuItem") as MenuItem;
                if (settingsMenuItem != null)
                    settingsMenuItem.Click += (s, e) => OpenSettings();
                
                // 查找并绑定网络控制菜单项
                var networkMenuItem = LogicalTreeHelper.FindLogicalNode(_notifyIcon.ContextMenu, "NetworkControlMenuItem") as MenuItem;
                if (networkMenuItem != null)
                    networkMenuItem.Click += (s, e) => OpenNetworkControl();
                
                // 查找并绑定退出菜单项
                var exitMenuItem = LogicalTreeHelper.FindLogicalNode(_notifyIcon.ContextMenu, "ExitMenuItem") as MenuItem;
                if (exitMenuItem != null)
                    exitMenuItem.Click += ExitApplication;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"初始化托盘图标失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Window_Loaded(object? sender, RoutedEventArgs? e)
    {
        if (e == null) return;
        
        // 获取窗口句柄
        IntPtr windowHandle = new WindowInteropHelper(this).Handle;
        
        // 设置鼠标点击穿透
        SetClickThrough(windowHandle);
        
        // 初始化热键管理器
        _hotkeyManager = new HotkeyManager(windowHandle);
        
        // 注册全局热键
        RegisterHotkeys();
        
        // 初始化准星
        UpdateCrosshair();
        
        // 设置定时器保持窗口置顶
        DispatcherTimer timer = new DispatcherTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (s, args) => Topmost = true;
        timer.Start();
        
        // 将窗口定位到屏幕中心
        CenterWindowOnScreen();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs? e)
    {
        if (e == null) return;
        
        _settings.SaveSettings();
        
        _hotkeyManager?.UnregisterAllHotKeys();
        
        _settingsWindow?.Close();

        if (_notifyIcon != null)
        {
            _notifyIcon.Dispose();
        }
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyManager == null) return;
        
        _hotkeyManager.RegisterHotKey(Key.Z, alt: true, callback: ToggleVisibility);
        
        _hotkeyManager.RegisterHotKey(Key.X, alt: true, callback: OpenSettings);
        
        // 网络控制相关快捷键
        _hotkeyManager.RegisterHotKey(Key.N, alt: true, callback: OpenNetworkControl);
        
        _hotkeyManager.RegisterHotKey(Key.L, alt: true, callback: ToggleNetworkLimit);
        
        _hotkeyManager.RegisterHotKey(Key.Up, alt: true, callback: MoveUp);
        
        _hotkeyManager.RegisterHotKey(Key.Down, alt: true, callback: MoveDown);
        
        _hotkeyManager.RegisterHotKey(Key.Left, alt: true, callback: MoveLeft);
        
        _hotkeyManager.RegisterHotKey(Key.Right, alt: true, callback: MoveRight);
        
        _hotkeyManager.RegisterHotKey(Key.Home, alt: true, callback: ResetPosition);
        
        _hotkeyManager.RegisterHotKey(Key.Add, alt: true, callback: IncreaseCrosshairSize);
        
        _hotkeyManager.RegisterHotKey(Key.Subtract, alt: true, callback: DecreaseCrosshairSize);
        
        _hotkeyManager.RegisterHotKey(Key.PageUp, alt: true, callback: NextCrosshairStyle);
        
        _hotkeyManager.RegisterHotKey(Key.PageDown, alt: true, callback: PreviousCrosshairStyle);
        
        _hotkeyManager.RegisterHotKey(Key.OemOpenBrackets, alt: true, callback: DecreaseOpacity);
        
        _hotkeyManager.RegisterHotKey(Key.OemCloseBrackets, alt: true, callback: IncreaseOpacity);

        _hotkeyManager.RegisterHotKey(Key.Q, alt: true, callback: () => System.Windows.Application.Current.Shutdown());
    }

    #region 热键回调函数
    
    public void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        Visibility = _isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    public void OpenSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }
    
    // 新增: 打开网络控制窗口
    public void OpenNetworkControl()
    {
        if (_networkControlWindow == null || !_networkControlWindow.IsLoaded)
        {
            _networkControlWindow = new NetworkControlWindow();
            _networkControlWindow.Show();
        }
        else
        {
            _networkControlWindow.Activate();
        }
    }
    
    // 新增: 网络限速快捷切换
    private async void ToggleNetworkLimit()
    {
        await NetworkController.Instance.ToggleNetworkLimit();
    }

    private void MoveUp()
    {
        _yOffset -= PositionAdjustStep;
        Top -= PositionAdjustStep;
    }

    private void MoveDown()
    {
        _yOffset += PositionAdjustStep;
        Top += PositionAdjustStep;
    }

    private void MoveLeft()
    {
        _xOffset -= PositionAdjustStep;
        Left -= PositionAdjustStep;
    }

    private void MoveRight()
    {
        _xOffset += PositionAdjustStep;
        Left += PositionAdjustStep;
    }

    private void ResetPosition()
    {
        _xOffset = 0;
        _yOffset = 0;
        CenterWindowOnScreen();
    }

    private void IncreaseCrosshairSize()
    {
        _settings.IncreaseSize();
        UpdateCrosshair();
    }

    private void DecreaseCrosshairSize()
    {
        _settings.DecreaseSize();
        UpdateCrosshair();
    }

    private void NextCrosshairStyle()
    {
        _settings.NextStyle();
        UpdateCrosshair();
    }

    private void PreviousCrosshairStyle()
    {
        _settings.PreviousStyle();
        UpdateCrosshair();
    }
    
    private void IncreaseOpacity()
    {
        _settings.IncreaseOpacity();
        UpdateCrosshair();
    }
    
    private void DecreaseOpacity()
    {
        _settings.DecreaseOpacity();
        UpdateCrosshair();
    }
    
    #endregion

    // 处理窗口大小改变事件，确保准星始终在中心
    private void Window_SizeChanged(object? sender, SizeChangedEventArgs? e)
    {
        if (e == null) return;
        
        // 更新准星位置到窗口中心
        UpdateCrosshairPosition();
    }

    // 更新准星位置到窗口中心
    private void UpdateCrosshairPosition()
    {
        if (!_isPositionLocked)
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            
            this.Left = (screenWidth - this.Width) / 2 + _xOffset;
            this.Top = (screenHeight - this.Height) / 2 + _yOffset;
        }
        
        // 更新准星在Canvas内的位置，使其居中
        if (CrosshairCanvas == null || CrosshairShape == null) return;
        
        double canvasWidth = CrosshairCanvas.ActualWidth;
        double canvasHeight = CrosshairCanvas.ActualHeight;
        
        if (canvasWidth > 0 && canvasHeight > 0)
        {
            Canvas.SetLeft(CrosshairShape, (canvasWidth - CrosshairShape.ActualWidth) / 2);
            Canvas.SetTop(CrosshairShape, (canvasHeight - CrosshairShape.ActualHeight) / 2);
            
            // 更新描边位置，与主准星保持一致
            Canvas.SetLeft(CrosshairOutline, Canvas.GetLeft(CrosshairShape));
            Canvas.SetTop(CrosshairOutline, Canvas.GetTop(CrosshairShape));
        }
    }

    // 更新准星
    public void UpdateCrosshair()
    {
        if (CrosshairShape == null) return;
        
        // 更新主准星
        CrosshairRenderer.UpdateCrosshair(CrosshairShape, _settings);
        
        // 处理描边效果
        if (_settings.EnableOutline && _settings.UseSolidOutline)
        {
            // 使用实线描边，显示额外的描边Path
            CrosshairOutline.Visibility = Visibility.Visible;
            CrosshairOutline.Data = CrosshairShape.Data; // 确保描边与主准星形状一致
            
            // 设置描边颜色和透明度
            System.Windows.Media.Color outlineColor = _settings.OutlineColor;
            outlineColor.A = Convert.ToByte(255 * _settings.OutlineOpacity);
            CrosshairOutline.Stroke = new SolidColorBrush(outlineColor);
            
            // 设置描边粗细，必须比主准星粗
            CrosshairOutline.StrokeThickness = _settings.BorderThickness + _settings.OutlineThickness * 2;
            
            // 主准星不应该有阴影效果
            CrosshairShape.Effect = null;
        }
        else if (_settings.EnableOutline)
        {
            // 使用DropShadow效果的描边
            CrosshairOutline.Visibility = Visibility.Collapsed;
            
            var shadowEffect = new DropShadowEffect
            {
                Color = _settings.OutlineColor,
                Direction = 0,
                ShadowDepth = 0,
                BlurRadius = _settings.OutlineThickness * 3, // 模糊半径
                Opacity = _settings.OutlineOpacity
            };
            CrosshairShape.Effect = shadowEffect;
        }
        else
        {
            // 不使用描边
            CrosshairOutline.Visibility = Visibility.Collapsed;
            CrosshairShape.Effect = null;
        }
        
        // 确保准星位置正确
        UpdateCrosshairPosition();
    }

    // 处理拖动区域的鼠标左键按下事件
    private void DragArea_MouseLeftButtonDown(object? sender, MouseButtonEventArgs? e)
    {
        if (e == null) return;
        DragMove();
    }

    // 处理调整大小手柄的拖动事件
    private void ResizeThumb_DragDelta(object? sender, System.Windows.Controls.Primitives.DragDeltaEventArgs? e)
    {
        if (e == null) return;
        
        // 确保宽度和高度不小于最小值
        double newWidth = Math.Max(50, Width + e.HorizontalChange);
        double newHeight = Math.Max(50, Height + e.VerticalChange);
        
        Width = newWidth;
        Height = newHeight;
    }

    // 设置点击穿透
    private void SetClickThrough(IntPtr hwnd)
    {
        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
    }
    
    // 将窗口定位到屏幕中心
    private void CenterWindowOnScreen()
    {
        // 使用WPF方法获取主屏幕工作区
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double screenHeight = SystemParameters.PrimaryScreenHeight;
        
        // 计算窗口位置，使准星处于屏幕正中心
        // 考虑准星位于窗口中心，所以窗口也应该在屏幕中心
        Left = (screenWidth - Width) / 2 + _xOffset;
        Top = (screenHeight - Height) / 2 + _yOffset;
        
        // 确保准星控件位于窗口中心
        UpdateCrosshairPosition();
        
        // 输出调试信息
        Console.WriteLine($"屏幕尺寸: {screenWidth}x{screenHeight}");
        Console.WriteLine($"窗口位置: Left={Left}, Top={Top}");
        Console.WriteLine($"窗口尺寸: Width={Width}, Height={Height}");
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e == null) return;
        
        if (Keyboard.Modifiers == ModifierKeys.Alt)
        {
            switch (e.Key)
            {
                case Key.Up:
                    MoveUp();
                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveDown();
                    e.Handled = true;
                    break;
                case Key.Left:
                    MoveLeft();
                    e.Handled = true;
                    break;
                case Key.Right:
                    MoveRight();
                    e.Handled = true;
                    break;
            }
        }
    }

    private void HotKey_OnPressed(object? sender, EventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
        {
            ToggleCrosshairVisibility();
        });
    }

    private void ToggleCrosshairVisibility()
    {
        _isCrosshairVisible = !_isCrosshairVisible;
        CrosshairShape.Visibility = _isCrosshairVisible ? Visibility.Visible : Visibility.Hidden;
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(this);
            _settingsWindow.Show();
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void NotifyIcon_TrayLeftMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        OpenSettings();
    }
    
    private void ExitApplication(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}