using System;
using System.Windows.Media;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;

namespace GameTools
{
    /// <summary>
    /// 管理准星的各种设置
    /// </summary>
    public class CrosshairSettings
    {
        // 单例实例
        private static CrosshairSettings? _instance;
        
        // 准星样式枚举
        public enum CrosshairStyle
        {
            Default,    // 默认十字准星带圆心
            Plus,       // 简单十字
            Circle,     // 圆形准星
            Dot,        // 点状准星
            Triangle    // 三角形准星
        }
        
        // 单例模式
        public static CrosshairSettings Instance
        {
            get
            {
                _instance ??= new CrosshairSettings();
                _instance.LoadSettings();
                return _instance;
            }
        }
        
        // 设置文件的路径
        private static readonly string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        
        // 默认设置
        public const double MinSize = 0.2;
        public const double MaxSize = 20.0;
        public const double DefaultSize = 1.0;
        private const double DefaultOpacity = 0.7;
        private const int DefaultStyle = 0;
        private static readonly System.Windows.Media.Color DefaultColor = Colors.Red;
        private const double DefaultBorderThickness = 2.0;
        private const bool DefaultShowFill = true;
        private const bool DefaultEnableOutline = false;
        private static readonly System.Windows.Media.Color DefaultOutlineColor = Colors.Black;
        private const double DefaultOutlineOpacity = 0.8;
        private const double DefaultOutlineThickness = 1.0;
        private const bool DefaultUseSolidOutline = false; // 默认不使用实线描边

        // 设置属性
        public double Size { get; set; } = DefaultSize;
        public double Opacity { get; set; } = DefaultOpacity;
        public int Style { get; set; } = DefaultStyle;
        public System.Windows.Media.Color Color { get; set; } = DefaultColor;
        public double BorderThickness { get; set; } = DefaultBorderThickness; // 边框粗细
        public bool ShowFill { get; set; } = DefaultShowFill; // 是否显示填充
        public bool EnableOutline { get; set; } = DefaultEnableOutline; // 是否启用描边
        public System.Windows.Media.Color OutlineColor { get; set; } = DefaultOutlineColor; // 描边颜色
        public double OutlineOpacity { get; set; } = DefaultOutlineOpacity; // 描边透明度
        public double OutlineThickness { get; set; } = DefaultOutlineThickness; // 描边粗细
        public bool UseSolidOutline { get; set; } = DefaultUseSolidOutline; // 是否使用实线描边（Excel风格）

        // 准星样式名称数组
        public static readonly string[] CrosshairStyles = new string[]
        {
            "默认十字带圆心", // 十字准星带圆心
            "简单十字",     // 简单十字
            "圆形(中空)",   // 圆形
            "点",         // 点
            "三角形(中空)",  // 三角形
            "方形(中空)",   // 方形
            "菱形(中空)",   // 菱形
            "间隔十字",     // 十字准星2
            "双圆环"       // 圆环
        };

        // 私有构造函数（单例模式）
        private CrosshairSettings()
        {
            // 初始化默认设置
            Size = 20;
            BorderThickness = 1.5f;
            Opacity = 0.8;
            Style = 0;
            Color = System.Windows.Media.Colors.Green;
            EnableOutline = false;
            UseSolidOutline = false;
            OutlineThickness = 2.0f;
        }

        // 用于序列化/反序列化的数据传输对象
        private class CrosshairSettingsDto
        {
            public double Size { get; set; }
            public double Opacity { get; set; }
            public int Style { get; set; }
            // 为颜色存储ARGB值
            public byte ColorR { get; set; }
            public byte ColorG { get; set; }
            public byte ColorB { get; set; }
            public byte ColorA { get; set; }
            // 边框和填充控制
            public double BorderThickness { get; set; }
            public bool ShowFill { get; set; }
            public bool EnableOutline { get; set; }
            public byte OutlineColorR { get; set; }
            public byte OutlineColorG { get; set; }
            public byte OutlineColorB { get; set; }
            public byte OutlineColorA { get; set; }
            public double OutlineOpacity { get; set; }
            public double OutlineThickness { get; set; }
            public bool UseSolidOutline { get; set; } // 新增：是否使用实线描边
        }

        // 将设置转换为DTO
        private CrosshairSettingsDto ToDto()
        {
            return new CrosshairSettingsDto
            {
                Size = Size,
                Opacity = Opacity,
                Style = Style,
                ColorR = Color.R,
                ColorG = Color.G,
                ColorB = Color.B,
                ColorA = Color.A,
                BorderThickness = BorderThickness,
                ShowFill = ShowFill,
                EnableOutline = EnableOutline,
                OutlineColorR = OutlineColor.R,
                OutlineColorG = OutlineColor.G,
                OutlineColorB = OutlineColor.B,
                OutlineColorA = OutlineColor.A,
                OutlineOpacity = OutlineOpacity,
                OutlineThickness = OutlineThickness,
                UseSolidOutline = UseSolidOutline
            };
        }

        // 从DTO加载设置
        private void FromDto(CrosshairSettingsDto? dto)
        {
            if (dto == null)
            {
                ResetToDefaults();
                return;
            }
            
            Size = dto.Size;
            Opacity = dto.Opacity;
            Style = dto.Style;
            Color = System.Windows.Media.Color.FromArgb(
                dto.ColorA,
                dto.ColorR,
                dto.ColorG,
                dto.ColorB
            );
            
            // 如果是旧版本保存的数据，这些属性可能不存在，则使用默认值
            BorderThickness = dto.BorderThickness > 0 ? dto.BorderThickness : DefaultBorderThickness;
            ShowFill = dto.ShowFill; // 布尔值默认为false，所以不需要特殊处理
            EnableOutline = dto.EnableOutline;
            OutlineColor = System.Windows.Media.Color.FromArgb(
                dto.OutlineColorA,
                dto.OutlineColorR,
                dto.OutlineColorG,
                dto.OutlineColorB
            );
            OutlineOpacity = dto.OutlineOpacity;
            OutlineThickness = dto.OutlineThickness;
            UseSolidOutline = dto.UseSolidOutline; // 新增：加载是否使用实线描边
        }

        // 加载设置
        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    if (string.IsNullOrEmpty(json))
                    {
                        ResetToDefaults();
                        return;
                    }
                    
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var settingsDto = JsonSerializer.Deserialize<CrosshairSettingsDto>(json, options);
                    if (settingsDto == null)
                    {
                        ResetToDefaults();
                        return;
                    }
                    FromDto(settingsDto);
                }
                else
                {
                    // 如果文件不存在，使用默认设置
                    ResetToDefaults();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载设置时出错: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // 加载失败时使用默认设置
                ResetToDefaults();
            }
        }

        // 保存设置
        public void SaveSettings()
        {
            try
            {
                string? directoryPath = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var settingsDto = ToDto();
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                string json = JsonSerializer.Serialize(settingsDto, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置时出错: {ex.Message}", "错误", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 重置为默认设置
        public void ResetToDefaults()
        {
            Size = DefaultSize;
            Opacity = DefaultOpacity;
            Style = DefaultStyle;
            Color = DefaultColor;
            UseSolidOutline = DefaultUseSolidOutline;
            BorderThickness = DefaultBorderThickness;
            ShowFill = DefaultShowFill;
            EnableOutline = DefaultEnableOutline;
            OutlineThickness = DefaultOutlineThickness;
            OutlineOpacity = DefaultOutlineOpacity;
        }

        // 更改准星大小
        public void IncreaseSize(double amount = 0.1)
        {
            Size = Math.Min(Size + amount, MaxSize); // 限制最大值
        }

        public void DecreaseSize(double amount = 0.1)
        {
            Size = Math.Max(Size - amount, MinSize); // 限制最小值
        }

        // 切换准星样式
        public void NextStyle()
        {
            Style = (Style + 1) % CrosshairStyles.Length;
        }

        public void PreviousStyle()
        {
            Style = (Style - 1 + CrosshairStyles.Length) % CrosshairStyles.Length;
        }

        // 调整透明度
        public void IncreaseOpacity(double amount = 0.1)
        {
            Opacity = Math.Min(Opacity + amount, 1.0);
        }

        public void DecreaseOpacity(double amount = 0.1)
        {
            Opacity = Math.Max(Opacity - amount, 0.1);
        }

        // 调整边框粗细
        public void IncreaseBorderThickness(double amount = 0.5)
        {
            BorderThickness = Math.Min(BorderThickness + amount, 10.0); // 最大边框粗细为10
        }

        public void DecreaseBorderThickness(double amount = 0.5)
        {
            BorderThickness = Math.Max(BorderThickness - amount, 0.5); // 最小边框粗细为0.5
        }

        // 切换填充显示
        public void ToggleFill()
        {
            ShowFill = !ShowFill;
        }

        // 切换描边
        public void ToggleOutline()
        {
            EnableOutline = !EnableOutline;
        }

        // 切换实线描边
        public void ToggleSolidOutline()
        {
            UseSolidOutline = !UseSolidOutline;
        }

        // 调整描边透明度
        public void IncreaseOutlineOpacity(double amount = 0.1)
        {
            OutlineOpacity = Math.Min(OutlineOpacity + amount, 1.0);
        }

        public void DecreaseOutlineOpacity(double amount = 0.1)
        {
            OutlineOpacity = Math.Max(OutlineOpacity - amount, 0.1);
        }

        // 调整描边粗细
        public void IncreaseOutlineThickness(double amount = 0.5)
        {
            OutlineThickness = Math.Min(OutlineThickness + amount, 5.0);
        }

        public void DecreaseOutlineThickness(double amount = 0.5)
        {
            OutlineThickness = Math.Max(OutlineThickness - amount, 0.5);
        }

        private void ValidateValues()
        {
            // 确保所有数值在有效范围内
            Opacity = Math.Clamp(Opacity, 0.1, 1.0);
            Size = Math.Clamp(Size, MinSize, MaxSize);
            Style = Math.Clamp(Style, 0, 7);
            BorderThickness = Math.Clamp(BorderThickness, 0.5, 5.0);
            OutlineThickness = Math.Clamp(OutlineThickness, 0.5, 10.0);
            OutlineOpacity = Math.Clamp(OutlineOpacity, 0.1, 1.0);
        }
    }
} 