using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GameTools
{
    public partial class SettingsWindow : Window
    {
        private CrosshairSettings _settings;
        private MainWindow _mainWindow;

        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _settings = CrosshairSettings.Instance;

            // 加载当前设置到UI
            LoadSettingsToUI();
            
            // 添加窗口加载完成事件，确保准星居中
            this.Loaded += SettingsWindow_Loaded;
            
            // 添加Canvas大小变化事件
            PreviewCanvas.SizeChanged += PreviewCanvas_SizeChanged;
        }
        
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载完成后，重新更新预览准星位置
            UpdatePreviewPosition();
        }
        
        private void PreviewCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Canvas大小变化时更新准星位置
            UpdatePreviewPosition();
        }
        
        private void UpdatePreviewPosition()
        {
            if (PreviewCanvas.ActualWidth > 0 && PreviewCanvas.ActualHeight > 0)
            {
                // 计算中心位置
                double centerX = PreviewCanvas.ActualWidth / 2;
                double centerY = PreviewCanvas.ActualHeight / 2;
                
                // 设置准星位置
                Canvas.SetLeft(PreviewCrosshair, centerX);
                Canvas.SetTop(PreviewCrosshair, centerY);
                
                Console.WriteLine($"更新准星预览位置: {centerX}, {centerY}");
            }
            else
            {
                // 如果Canvas尚未完成布局，使用固定值
                Canvas.SetLeft(PreviewCrosshair, 150);
                Canvas.SetTop(PreviewCrosshair, 50);
                
                Console.WriteLine("Canvas尚未测量，使用默认位置");
            }
        }

        private void LoadSettingsToUI()
        {
            // 加载准星样式
            StyleComboBox.ItemsSource = CrosshairSettings.CrosshairStyles;
            StyleComboBox.SelectedIndex = _settings.Style;
            
            // 加载大小和透明度
            SizeSlider.Value = _settings.Size;
            OpacitySlider.Value = _settings.Opacity;
            
            // 加载边框粗细和填充设置
            BorderThicknessSlider.Value = _settings.BorderThickness;
            ShowFillCheckBox.IsChecked = _settings.ShowFill;
            
            // 加载描边设置
            EnableOutlineCheckBox.IsChecked = _settings.EnableOutline;
            OutlineOpacitySlider.Value = _settings.OutlineOpacity;
            OutlineThicknessSlider.Value = _settings.OutlineThickness;
            
            // 更新预览
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // 更新预览准星
            CrosshairRenderer.UpdateCrosshair(PreviewCrosshair, _settings);

            // 更新准星位置
            UpdatePreviewPosition();
        }

        private void StyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StyleComboBox.SelectedIndex >= 0)
            {
                _settings.Style = StyleComboBox.SelectedIndex;
                UpdatePreview();
            }
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _settings.Size = SizeSlider.Value;
                UpdatePreview();
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _settings.Opacity = e.NewValue;
                UpdatePreview();
            }
        }

        private void BorderThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _settings.BorderThickness = e.NewValue;
                UpdatePreview();
            }
        }

        private void ShowFillCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _settings.ShowFill = ShowFillCheckBox.IsChecked ?? true;
                UpdatePreview();
            }
        }

        private void EnableOutlineCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _settings.EnableOutline = EnableOutlineCheckBox.IsChecked ?? false;
                UpdatePreview();
            }
        }

        private void OutlineOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _settings.OutlineOpacity = e.NewValue;
                UpdatePreview();
            }
        }

        private void OutlineThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _settings.OutlineThickness = e.NewValue;
                UpdatePreview();
            }
        }

        // 颜色按钮点击事件
        private void RedButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Red;
            UpdatePreview();
        }

        private void GreenButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Green;
            UpdatePreview();
        }

        private void BlueButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Blue;
            UpdatePreview();
        }

        private void YellowButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Yellow;
            UpdatePreview();
        }

        private void MagentaButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Magenta;
            UpdatePreview();
        }

        private void CyanButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Cyan;
            UpdatePreview();
        }

        private void WhiteButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.White;
            UpdatePreview();
        }

        private void OrangeButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.Color = Colors.Orange;
            UpdatePreview();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // 重置设置
            _settings.ResetToDefaults();
            
            // 更新UI
            LoadSettingsToUI();
            
            // 更新主窗口准星
            _mainWindow.UpdateCrosshair();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存设置
            SaveSettingsFromUI();
            
            // 关闭设置窗口
            Close();
        }

        private void SaveSettingsFromUI()
        {
            // 保存样式、大小和透明度
            _settings.Style = StyleComboBox.SelectedIndex;
            _settings.Size = SizeSlider.Value;
            _settings.Opacity = OpacitySlider.Value;
            
            // 保存边框粗细和填充设置
            _settings.BorderThickness = BorderThicknessSlider.Value;
            _settings.ShowFill = ShowFillCheckBox.IsChecked ?? true;
            
            // 保存描边设置
            _settings.EnableOutline = EnableOutlineCheckBox.IsChecked ?? false;
            _settings.OutlineOpacity = OutlineOpacitySlider.Value;
            _settings.OutlineThickness = OutlineThicknessSlider.Value;
            
            // 保存设置
            _settings.SaveSettings();
            
            // 更新主窗口中的准星
            _mainWindow.UpdateCrosshair();
        }
    }
} 