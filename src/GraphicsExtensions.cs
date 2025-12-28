using System.Drawing;
using System.Drawing.Drawing2D;

namespace FreeMyRam;

/// <summary>
/// Extension methods for Graphics and GraphicsPath
/// </summary>
public static class GraphicsExtensions
{
    public static void AddRoundedRectangle(this GraphicsPath path, Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        Size size = new(diameter, diameter);
        Rectangle arc = new(bounds.Location, size);

        // Top left arc
        path.AddArc(arc, 180, 90);

        // Top right arc
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);

        // Bottom right arc
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);

        // Bottom left arc
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
    }
}
