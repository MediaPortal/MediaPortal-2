using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace DrawingProbe
{
  /// <summary>
  /// Measures which half of System.Drawing works off Windows. See DrawingProbe.csproj for why
  /// this exists and scripts/ci/README.md for the conclusion drawn from it.
  /// </summary>
  internal static class Program
  {
    private static void Main()
    {
      // Half one: System.Drawing.Primitives.dll. Part of the shared framework, no package
      // reference involved. If this line prints, value types are portable and a spine metric must
      // not count them as Windows coupling.
      Color color = Color.FromArgb(255, 12, 34, 56);
      SizeF size = new SizeF(4f, 3f);
      Rectangle rect = new Rectangle(1, 2, 3, 4);
      Console.WriteLine($"PRIMITIVES WORK: argb={color.Name} size={size.Width}x{size.Height} rect={rect.Width} known={KnownColor.Red}");

      // Half two: System.Drawing.Common.dll. Compiles only because the probe references the
      // package, and on Linux it must refuse at runtime.
      try
      {
        using (Bitmap bitmap = new Bitmap(4, 4))
          bitmap.Save("/tmp/drawing-probe.jpg", ImageFormat.Jpeg);
        Console.WriteLine("IMAGING WORKED - the assumption that GDI+ is Windows-only is wrong");
      }
      catch (Exception ex)
      {
        Exception inner = ex;
        while (inner.InnerException != null)
          inner = inner.InnerException;
        Console.WriteLine($"IMAGING REFUSES: {ex.GetType().FullName}: {ex.Message}");
        Console.WriteLine($"  innermost: {inner.GetType().FullName}: {inner.Message}");
      }
    }
  }
}
