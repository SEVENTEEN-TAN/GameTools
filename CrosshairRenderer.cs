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
        public static Geometry CreateCrosshairGeometry(int style, double size, double borderThickness = 1.0)
        {
            StreamGeometry geometry = new StreamGeometry();
            
            using (StreamGeometryContext context = geometry.Open())
            {
                switch (style)
                {
                    case 0: // 井字形状准星
                        // 确保线条的长度足够
                        double lineLength = Math.Max(10, size * 1.5);
                        
                        // 计算线条之间的间距，使其随着准星大小调整而保持合适的比例
                        // 当准星小时，保持最小间距，随着准星变大，间距按比例增加，但增长速度稍慢些
                        double lineSpacing = Math.Max(3, size * 0.6);
                        
                        // 限制最大间距，防止间距过大
                        lineSpacing = Math.Min(lineSpacing, lineLength * 0.4);
                        
                        // 上方水平线
                        context.BeginFigure(new System.Windows.Point(-lineLength, -lineSpacing), false, false);
                        context.LineTo(new System.Windows.Point(lineLength, -lineSpacing), true, false);
                        
                        // 下方水平线
                        context.BeginFigure(new System.Windows.Point(-lineLength, lineSpacing), false, false);
                        context.LineTo(new System.Windows.Point(lineLength, lineSpacing), true, false);
                        
                        // 左侧垂直线
                        context.BeginFigure(new System.Windows.Point(-lineSpacing, -lineLength), false, false);
                        context.LineTo(new System.Windows.Point(-lineSpacing, lineLength), true, false);
                        
                        // 右侧垂直线
                        context.BeginFigure(new System.Windows.Point(lineSpacing, -lineLength), false, false);
                        context.LineTo(new System.Windows.Point(lineSpacing, lineLength), true, false);
                        
                        return geometry;
                        
                    case 1: // 十字准星
                        // 水平线
                        context.BeginFigure(new System.Windows.Point(-size, 0), false, false);
                        context.LineTo(new System.Windows.Point(size, 0), true, false);
                        
                        // 垂直线
                        context.BeginFigure(new System.Windows.Point(0, -size), false, false);
                        context.LineTo(new System.Windows.Point(0, size), true, false);
                        break;
                        
                    case 2: // 圆形
                        EllipseGeometry circleGeom = new EllipseGeometry(new System.Windows.Point(0, 0), size, size);
                        return circleGeom;
                        
                    case 3: // 点
                        EllipseGeometry dotGeom = new EllipseGeometry(new System.Windows.Point(0, 0), size/2, size/2);
                        return dotGeom;
                        
                    case 4: // 三角形
                        double triangleSize = size * 1.5;
                        context.BeginFigure(new System.Windows.Point(0, -triangleSize), true, true);
                        context.LineTo(new System.Windows.Point(triangleSize * 0.866, triangleSize / 2), true, false);
                        context.LineTo(new System.Windows.Point(-triangleSize * 0.866, triangleSize / 2), true, false);
                        break;
                        
                    case 5: // 方形
                        context.BeginFigure(new System.Windows.Point(-size, -size), true, true);
                        context.LineTo(new System.Windows.Point(size, -size), true, false);
                        context.LineTo(new System.Windows.Point(size, size), true, false);
                        context.LineTo(new System.Windows.Point(-size, size), true, false);
                        break;
                        
                    case 6: // 菱形
                        context.BeginFigure(new System.Windows.Point(0, -size * 1.5), true, true);
                        context.LineTo(new System.Windows.Point(size, 0), true, false);
                        context.LineTo(new System.Windows.Point(0, size * 1.5), true, false);
                        context.LineTo(new System.Windows.Point(-size, 0), true, false);
                        break;
                        
                    case 7: // 十字准星2（中间空心）
                        double gap = size / 3;
                        
                        // 左边横线
                        context.BeginFigure(new System.Windows.Point(-size, 0), false, false);
                        context.LineTo(new System.Windows.Point(-gap, 0), true, false);
                        
                        // 右边横线
                        context.BeginFigure(new System.Windows.Point(gap, 0), false, false);
                        context.LineTo(new System.Windows.Point(size, 0), true, false);
                        
                        // 上边竖线
                        context.BeginFigure(new System.Windows.Point(0, -size), false, false);
                        context.LineTo(new System.Windows.Point(0, -gap), true, false);
                        
                        // 下边竖线
                        context.BeginFigure(new System.Windows.Point(0, gap), false, false);
                        context.LineTo(new System.Windows.Point(0, size), true, false);
                        break;
                        
                    case 8: // 圆环
                        // 外圆
                        EllipseGeometry outerCircle = new EllipseGeometry(new System.Windows.Point(0, 0), size, size);
                        // 内圆（用于挖空）
                        EllipseGeometry innerCircle = new EllipseGeometry(new System.Windows.Point(0, 0), size - borderThickness * 2, size - borderThickness * 2);
                        // 合并为圆环
                        return Geometry.Combine(outerCircle, innerCircle, GeometryCombineMode.Exclude, null);
                        
                    default:
                        // 默认为十字准星
                        context.BeginFigure(new System.Windows.Point(-size, 0), false, false);
                        context.LineTo(new System.Windows.Point(size, 0), true, false);
                        
                        context.BeginFigure(new System.Windows.Point(0, -size), false, false);
                        context.LineTo(new System.Windows.Point(0, size), true, false);
                        break;
                }
            }
            
            geometry.Freeze();
            return geometry;
        }

        // 更新准星
        public static void UpdateCrosshair(Path crosshair, CrosshairSettings settings)
        {
            if (crosshair == null) return;

            // 根据样式和尺寸创建几何图形
            crosshair.Data = CreateCrosshairGeometry(settings.Style, settings.Size, settings.BorderThickness);

            // 设置描边颜色和透明度
            System.Windows.Media.Color strokeColor = settings.Color;
            strokeColor.A = Convert.ToByte(255 * settings.Opacity);
            crosshair.Stroke = new SolidColorBrush(strokeColor);
            crosshair.StrokeThickness = settings.BorderThickness;
            
            // 根据ShowFill设置填充
            if (settings.ShowFill)
            {
                System.Windows.Media.Color fillColor = settings.Color;
                fillColor.A = Convert.ToByte(255 * settings.Opacity * 0.5); // 填充透明度为描边的一半
                crosshair.Fill = new SolidColorBrush(fillColor);
            }
            else
            {
                crosshair.Fill = null;
            }

            // 描边效果现在在主窗口和设置窗口中分别处理
            // 此处不再处理实线描边逻辑
        }
    }
} 