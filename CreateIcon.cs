using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

class Program
{
    static void Main()
    {
        string outputDir = @"L:\1c_modul\hermass\src\LlamaStudio\Assets";
        
        // Create a beautiful "L" icon with gradient background
        using (var bmp = new Bitmap(256, 256))
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                
                // Create rounded rectangle background
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    int radius = 40;
                    path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
                    path.AddArc(256 - radius * 2, 0, radius * 2, radius * 2, 270, 90);
                    path.AddArc(256 - radius * 2, 256 - radius * 2, radius * 2, radius * 2, 0, 90);
                    path.AddArc(0, 256 - radius * 2, radius * 2, radius * 2, 90, 90);
                    path.CloseFigure();
                    
                    // Gradient background (purple to blue)
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new Rectangle(0, 0, 256, 256),
                        Color.FromArgb(120, 80, 255),   // Purple
                        Color.FromArgb(60, 140, 255),   // Blue
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        g.FillPath(brush, path);
                    }
                    
                    // Add border
                    using (var pen = new Pen(Color.FromArgb(255, 255, 255), 3))
                    {
                        g.DrawPath(pen, path);
                    }
                }
                
                // Draw "L" letter
                using (var font = new Font("Segoe UI", 140, FontStyle.Bold))
                {
                    string text = "L";
                    SizeF size = g.MeasureString(text, font);
                    float x = (256 - size.Width) / 2;
                    float y = (256 - size.Height) / 2;
                    
                    // Text shadow
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                    {
                        g.DrawString(text, font, shadowBrush, x + 2, y + 2);
                    }
                    
                    // Main text (white with slight transparency for glow effect)
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        g.DrawString(text, font, textBrush, x, y);
                    }
                }
            }
            
            // Save as ICO
            using (var icon = Icon.FromHandle(bmp.GetHicon()))
            {
                using (var fs = new FileStream(Path.Combine(outputDir, "app-icon.ico"), FileMode.Create))
                {
                    icon.Save(fs);
                }
            }
        }
        
        Console.WriteLine("Icon created successfully!");
    }
}
