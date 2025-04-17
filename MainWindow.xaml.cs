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
using System.Windows.Forms;
using System.Diagnostics;

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
    private NotifyIcon? _notifyIcon;
    
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
        _notifyIcon = new NotifyIcon();
        _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        _notifyIcon.Text = "游戏准星工具";
        _notifyIcon.Visible = true;

        ContextMenuStrip contextMenu = new ContextMenuStrip();
        
        ToolStripMenuItem toggleItem = new ToolStripMenuItem("显示/隐藏准星");
        toggleItem.Click += (s, e) => ToggleVisibility();
        contextMenu.Items.Add(toggleItem);
        
        ToolStripMenuItem settingsItem = new ToolStripMenuItem("设置");
        settingsItem.Click += (s, e) => OpenSettings();
        contextMenu.Items.Add(settingsItem);
        
        contextMenu.Items.Add(new ToolStripSeparator());
        
        ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => 
        {
            System.Windows.Application.Current.Shutdown();
        };
        contextMenu.Items.Add(exitItem);
        
        _notifyIcon.ContextMenuStrip = contextMenu;
        
        _notifyIcon.DoubleClick += (s, e) => OpenSettings();
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
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
    }

    private void RegisterHotkeys()
    {
        if (_hotkeyManager == null) return;
        
        _hotkeyManager.RegisterHotKey(Key.Z, alt: true, callback: ToggleVisibility);
        
        _hotkeyManager.RegisterHotKey(Key.X, alt: true, callback: OpenSettings);
        
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
    
    private void ToggleVisibility()
    {
        _isVisible = !_isVisible;
        Visibility = _isVisible ? Visibility.Visible : Visibility.Hidden;
    }

    private void OpenSettings()
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
        if (CrosshairCanvas == null || Crosshair == null) return;
        
        double canvasWidth = CrosshairCanvas.ActualWidth;
        double canvasHeight = CrosshairCanvas.ActualHeight;
        
        if (canvasWidth > 0 && canvasHeight > 0)
        {
            Canvas.SetLeft(Crosshair, (canvasWidth - Crosshair.ActualWidth) / 2);
            Canvas.SetTop(Crosshair, (canvasHeight - Crosshair.ActualHeight) / 2);
            
            // 更新描边位置，与主准星保持一致
            Canvas.SetLeft(CrosshairOutline, Canvas.GetLeft(Crosshair));
            Canvas.SetTop(CrosshairOutline, Canvas.GetTop(Crosshair));
        }
    }

    // 更新准星
    public void UpdateCrosshair()
    {
        if (Crosshair == null) return;
        
        // 更新主准星
        CrosshairRenderer.UpdateCrosshair(Crosshair, _settings);
        
        // 处理描边效果
        if (_settings.EnableOutline && _settings.UseSolidOutline)
        {
            // 使用实线描边，显示额外的描边Path
            CrosshairOutline.Visibility = Visibility.Visible;
            CrosshairOutline.Data = Crosshair.Data; // 确保描边与主准星形状一致
            
            // 设置描边颜色和透明度
            System.Windows.Media.Color outlineColor = _settings.OutlineColor;
            outlineColor.A = Convert.ToByte(255 * _settings.OutlineOpacity);
            CrosshairOutline.Stroke = new SolidColorBrush(outlineColor);
            
            // 设置描边粗细，必须比主准星粗
            CrosshairOutline.StrokeThickness = _settings.BorderThickness + _settings.OutlineThickness * 2;
            
            // 主准星不应该有阴影效果
            Crosshair.Effect = null;
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
            Crosshair.Effect = shadowEffect;
        }
        else
        {
            // 不使用描边
            CrosshairOutline.Visibility = Visibility.Collapsed;
            Crosshair.Effect = null;
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
        Crosshair.Visibility = _isCrosshairVisible ? Visibility.Visible : Visibility.Hidden;
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
}