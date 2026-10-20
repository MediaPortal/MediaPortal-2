/*
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace MediaPortal.Frontend.Views
{
  /// <summary>
  /// What the application itself paints, saved as a picture: the window's visual tree rendered into a
  /// bitmap and written to disk.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A screenshot taken of the desktop can lie - remote displays, KVM boxes, and display drivers have all
  /// been seen to hand out stale frames of screens that closed hours ago, and an operator who cannot tell a
  /// live frame from a ghost draws conclusions about skins that were never on the screen. The framework
  /// knows only one honest picture: the one it renders itself. This class asks the visual tree to draw into
  /// a bitmap, which no capture pipeline between the graphics card and the file can fake.
  /// </para>
  /// <para>
  /// The key F12 takes one picture of the whole window into the temporary folder; with the environment
  /// variable <c>MP2_AUTOSHOT</c> naming a file, the window also takes one by itself a few seconds after it
  /// opened, so a session that nobody is sitting in front of can still say what it showed.
  /// </para>
  /// </remarks>
  public static class ScreenshotService
  {
    /// <summary>
    /// Renders <paramref name="visual"/> and writes it as a PNG to <paramref name="path"/>. Returns the path
    /// on success and a message saying what went wrong otherwise - a picture is wanted exactly when
    /// something is off, and a service that throws would be a poor witness.
    /// </summary>
    public static string Capture(Visual visual, string path)
    {
      try
      {
        int width = (int)Math.Ceiling(visual.Bounds.Width);
        int height = (int)Math.Ceiling(visual.Bounds.Height);
        if (width <= 0 || height <= 0)
          return "nothing to capture: the visual has no size";

        using (var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96)))
        {
          bitmap.Render(visual);
          using (var file = File.Create(path))
          {
            bitmap.Save(file);
          }
        }
        return path;
      }
      catch (Exception e)
      {
        return "screenshot failed: " + e.Message;
      }
    }

    /// <summary>
    /// The path an F12 press writes to: the temporary folder, with the time in the name so presses do not
    /// overwrite each other.
    /// </summary>
    public static string NextTempPath()
    {
      return Path.Combine(Path.GetTempPath(), "mp2-shot-" + DateTime.Now.ToString("HHmmss") + ".png");
    }
  }
}
