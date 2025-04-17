using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace GameTools
{
    public static class CrosshairRenderer
    {
        // 创建准星路径数据
        public static Geometry CreateCrosshairGeometry(int style, double size)
        {
            GeometryGroup geometryGroup = new GeometryGroup();
            
            switch (style)
            {
                case 0: // 默认 (十字带圆心)
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(-10 * size, 0), new System.Windows.Point(10 * size, 0)));
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(0, -10 * size), new System.Windows.Point(0, 10 * size)));
                    geometryGroup.Children.Add(new EllipseGeometry(new System.Windows.Point(0, 0), 3 * size, 3 * size));
                    break;
                
                case 1: // 简单十字
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(-10 * size, 0), new System.Windows.Point(10 * size, 0)));
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(0, -10 * size), new System.Windows.Point(0, 10 * size)));
                    break;
                
                case 2: // 圆形 (中空)
                    geometryGroup.Children.Add(new EllipseGeometry(new System.Windows.Point(0, 0), 10 * size, 10 * size));
                    break;
                
                case 3: // 点
                    geometryGroup.Children.Add(new EllipseGeometry(new System.Windows.Point(0, 0), 4 * size, 4 * size));
                    break;
                
                case 4: // 三角形 (中空)
                    PathFigure figure = new PathFigure();
                    figure.StartPoint = new System.Windows.Point(0, -10 * size);
                    figure.Segments.Add(new LineSegment(new System.Windows.Point(10 * size, 10 * size), true));
                    figure.Segments.Add(new LineSegment(new System.Windows.Point(-10 * size, 10 * size), true));
                    figure.IsClosed = true;
                    
                    PathGeometry pathGeometry = new PathGeometry();
                    pathGeometry.Figures.Add(figure);
                    geometryGroup.Children.Add(pathGeometry);
                    break;
                    
                case 5: // 方形 (中空)
                    geometryGroup.Children.Add(new RectangleGeometry(new Rect(-10 * size, -10 * size, 20 * size, 20 * size)));
                    break;
                    
                case 6: // 菱形 (中空)
                    PathFigure diamondFigure = new PathFigure();
                    diamondFigure.StartPoint = new System.Windows.Point(0, -10 * size);
                    diamondFigure.Segments.Add(new LineSegment(new System.Windows.Point(10 * size, 0), true));
                    diamondFigure.Segments.Add(new LineSegment(new System.Windows.Point(0, 10 * size), true));
                    diamondFigure.Segments.Add(new LineSegment(new System.Windows.Point(-10 * size, 0), true));
                    diamondFigure.IsClosed = true;
                    
                    PathGeometry diamondGeometry = new PathGeometry();
                    diamondGeometry.Figures.Add(diamondFigure);
                    geometryGroup.Children.Add(diamondGeometry);
                    break;
                    
                case 7: // 十字准星2 (带间隔)
                    // 水平线（左侧）
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(-12 * size, 0), new System.Windows.Point(-4 * size, 0)));
                    // 水平线（右侧）
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(4 * size, 0), new System.Windows.Point(12 * size, 0)));
                    // 垂直线（上侧）
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(0, -12 * size), new System.Windows.Point(0, -4 * size)));
                    // 垂直线（下侧）
                    geometryGroup.Children.Add(new LineGeometry(new System.Windows.Point(0, 4 * size), new System.Windows.Point(0, 12 * size)));
                    break;
                    
                case 8: // 圆环 (双圆)
                    geometryGroup.Children.Add(new EllipseGeometry(new System.Windows.Point(0, 0), 10 * size, 10 * size));
                    geometryGroup.Children.Add(new EllipseGeometry(new System.Windows.Point(0, 0), 5 * size, 5 * size));
                    break;
            }
            
            return geometryGroup;
        }

        // 更新准星
        public static void UpdateCrosshair(Path crosshair, CrosshairSettings settings)
        {
            if (crosshair == null) throw new ArgumentNullException(nameof(crosshair));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            
            // 根据当前选择的准星样式创建形状
            crosshair.Data = CreateCrosshairGeometry(settings.Style, settings.Size);
            
            // 设置准星颜色
            SolidColorBrush brush = new SolidColorBrush(settings.Color);
            brush.Opacity = settings.Opacity;
            
            // 根据准星样式决定是否填充
            switch (settings.Style)
            {
                case 0: // 默认十字带圆心 - 圆心填充
                case 3: // 点 - 填充
                    crosshair.Fill = brush;
                    break;
                case 2: // 圆形 - 中空
                case 4: // 三角形 - 中空
                case 5: // 方形 - 中空
                case 6: // 菱形 - 中空
                case 7: // 十字准星2 - 中空
                case 8: // 圆环 - 中空
                    crosshair.Fill = null;
                    break;
                default: // 对于其他样式，根据是否需要边框设置
                    crosshair.Fill = settings.ShowFill ? brush : null;
                    break;
            }
            
            // 设置边框
            crosshair.Stroke = brush;
            crosshair.StrokeThickness = settings.BorderThickness;
            
            // 如果启用了描边，添加描边效果
            if (settings.EnableOutline)
            {
                // 创建描边效果
                SolidColorBrush outlineBrush = new SolidColorBrush(settings.OutlineColor);
                outlineBrush.Opacity = settings.OutlineOpacity;
                
                // 由于WPF Path不直接支持多重描边，我们使用DropShadowEffect来模拟描边效果
                DropShadowEffect outline = new DropShadowEffect
                {
                    Color = settings.OutlineColor,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = settings.OutlineThickness * 3,
                    Opacity = settings.OutlineOpacity
                };
                
                crosshair.Effect = outline;
            }
            else
            {
                // 如果未启用描边，清除效果
                crosshair.Effect = null;
            }
        }
    }
} 