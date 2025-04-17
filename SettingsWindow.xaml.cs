using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Effects;

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
            StyleComboBox.SelectedIndex = (int)_settings.Style;
            
            // 加载大小和透明度
            SizeSlider.Value = _settings.Size;
            OpacitySlider.Value = _settings.Opacity;
            
            // 加载边框粗细和填充设置
            BorderThicknessSlider.Value = _settings.BorderThickness;
            ShowFillCheckBox.IsChecked = _settings.ShowFill;
            
            // 加载描边设置
            EnableOutlineCheckbox.IsChecked = _settings.EnableOutline;
            OutlineOpacitySlider.Value = _settings.OutlineOpacity;
            OutlineStyleCheckbox.IsChecked = _settings.UseSolidOutline;
            OutlineWidthSlider.Value = _settings.OutlineThickness;
            
            // 更新准星颜色按钮的UI
            UpdateColorButtonUI();
            
            // 更新预览
            UpdatePreview();
            UpdatePreviewPosition();
        }

        private void UpdatePreview()
        {
            // 更新准星样式
            CrosshairRenderer.UpdateCrosshair(PreviewCrosshair, _settings);
            
            // 更新准星描边
            if (_settings.EnableOutline)
            {
                if (_settings.UseSolidOutline)
                {
                    // 使用实线描边
                    PreviewOutline.Visibility = Visibility.Visible;
                    // 确保与主准星使用相同的几何形状
                    PreviewOutline.Data = PreviewCrosshair.Data;
                    
                    // 设置描边颜色和透明度
                    System.Windows.Media.Color outlineColor = _settings.OutlineColor;
                    outlineColor.A = Convert.ToByte(255 * _settings.OutlineOpacity);
                    PreviewOutline.Stroke = new SolidColorBrush(outlineColor);
                    
                    // 设置描边粗细，必须比主准星粗
                    PreviewOutline.StrokeThickness = _settings.BorderThickness + _settings.OutlineThickness * 2;
                    
                    // 移除阴影效果
                    PreviewCrosshair.Effect = null;
                }
                else
                {
                    // 使用阴影效果
                    PreviewOutline.Visibility = Visibility.Collapsed;
                    
                    DropShadowEffect shadowEffect = new DropShadowEffect
                    {
                        Color = _settings.OutlineColor,
                        Direction = 0,
                        ShadowDepth = 0,
                        BlurRadius = _settings.OutlineThickness * 3,
                        Opacity = _settings.OutlineOpacity
                    };
                    
                    PreviewCrosshair.Effect = shadowEffect;
                }
            }
            else
            {
                // 禁用所有描边效果
                PreviewOutline.Visibility = Visibility.Collapsed;
                PreviewCrosshair.Effect = null;
            }
            
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

        private void EnableOutlineCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.EnableOutline = EnableOutlineCheckbox.IsChecked ?? false;
                UpdatePreview();
            }
        }

        private void OutlineStyleCheckbox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_settings != null)
            {
                _settings.UseSolidOutline = OutlineStyleCheckbox.IsChecked ?? false;
                UpdatePreview();
            }
        }

        private void OutlineWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_settings != null)
            {
                _settings.OutlineThickness = (float)OutlineWidthSlider.Value;
                UpdatePreview();
            }
        }

        private void OutlineOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_settings != null)
            {
                _settings.OutlineOpacity = e.NewValue;
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
            _settings.EnableOutline = EnableOutlineCheckbox.IsChecked ?? false;
            _settings.OutlineOpacity = OutlineOpacitySlider.Value;
            _settings.UseSolidOutline = OutlineStyleCheckbox.IsChecked ?? false;
            _settings.OutlineThickness = (float)OutlineWidthSlider.Value;
            
            // 保存设置
            _settings.SaveSettings();
            
            // 更新主窗口中的准星
            _mainWindow.UpdateCrosshair();
        }

        private void UpdateColorButtonUI()
        {
            // 直接在XAML中添加x:Name属性后，我们可以这样访问按钮
            var redButton = this.FindName("RedButton") as System.Windows.Controls.Button;
            var greenButton = this.FindName("GreenButton") as System.Windows.Controls.Button;
            var blueButton = this.FindName("BlueButton") as System.Windows.Controls.Button;
            var yellowButton = this.FindName("YellowButton") as System.Windows.Controls.Button;
            var magentaButton = this.FindName("MagentaButton") as System.Windows.Controls.Button;
            var cyanButton = this.FindName("CyanButton") as System.Windows.Controls.Button;
            var whiteButton = this.FindName("WhiteButton") as System.Windows.Controls.Button;
            var orangeButton = this.FindName("OrangeButton") as System.Windows.Controls.Button;
            
            // 更新颜色按钮的UI
            if (redButton != null) redButton.Background = new SolidColorBrush(Colors.Red); 
            if (greenButton != null) greenButton.Background = new SolidColorBrush(Colors.Green);
            if (blueButton != null) blueButton.Background = new SolidColorBrush(Colors.Blue);
            if (yellowButton != null) yellowButton.Background = new SolidColorBrush(Colors.Yellow);
            if (magentaButton != null) magentaButton.Background = new SolidColorBrush(Colors.Magenta);
            if (cyanButton != null) cyanButton.Background = new SolidColorBrush(Colors.Cyan);
            if (whiteButton != null) whiteButton.Background = new SolidColorBrush(Colors.White);
            if (orangeButton != null) orangeButton.Background = new SolidColorBrush(Colors.Orange);
        }

        #region 轮廓颜色按钮事件处理
        private void BlackOutlineButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.OutlineColor = System.Windows.Media.Colors.Black;
            UpdatePreview();
        }

        private void WhiteOutlineButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.OutlineColor = System.Windows.Media.Colors.White;
            UpdatePreview();
        }

        private void RedOutlineButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.OutlineColor = System.Windows.Media.Colors.Red;
            UpdatePreview();
        }

        private void GreenOutlineButton_Click(object sender, RoutedEventArgs e)
        {
            _settings.OutlineColor = System.Windows.Media.Colors.Green;
            UpdatePreview();
        }
        #endregion
    }
} 