using SharpDX;

namespace MediaPortal.UI.SkinEngine
{
  public static class SharpDXExtensions
  {
    public static System.Drawing.RectangleF ToDrawingRectF(this RectangleF rectangleF)
    {
      return new System.Drawing.RectangleF(rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
    }
    public static System.Drawing.SizeF ToDrawingSizeF(this Size2F size2F)
    {
      return new System.Drawing.SizeF(size2F.Width, size2F.Height);
    }
    public static Size2F ToSize2F(this System.Drawing.SizeF sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize2(this System.Drawing.Size sizeF)
    {
      return new Size2(sizeF.Width, sizeF.Height);
    }
    public static Size2 ToSize(this Size2F sizeF)
    {
      return new Size2((int)sizeF.Width, (int)sizeF.Height);
    }
    public static Size2F ToSize2F(this Size2 sizeF)
    {
      return new Size2F(sizeF.Width, sizeF.Height);
    }
    public static bool IsEmpty(this Size2F sizeF)
    {
      return sizeF.Width == 0.0 && sizeF.Height == 0.0;
    }
    public static RectangleF CreateRectangleF(Vector2 location, Size2F size)
    {
      return new RectangleF(location.X, location.Y, size.Width, size.Height);
    }
    public static Color FromArgb(int alpha, Color color)
    {
      // Attention: the documentation of this constructor is not correct!
      return new Color(color.G, color.B, alpha, color.R);
    }
    public static Color FromArgb(int alpha, int r, int g, int b)
    {
      // Attention: the documentation of this constructor is not correct!
      return new Color(g, b, alpha, r);
    }
  }
}
