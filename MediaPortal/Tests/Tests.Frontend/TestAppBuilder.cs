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
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using MediaPortal.Frontend;

// The whole suite runs the application object the product runs, on a platform with no display. "No display" is
// not a compromise here: the gate that will run these tests is a container and later a device build, neither of
// which has a screen either.
[assembly: AvaloniaTestApplication(typeof(Tests.Frontend.TestAppBuilder))]

namespace Tests.Frontend
{
  /// <summary>
  /// How a test builds the application. Kept separate from the tests so that the configuration is stated once.
  /// </summary>
  public static class TestAppBuilder
  {
    static TestAppBuilder()
    {
      // A test run must never wear what the machine's viewer last picked. The application reads its saved theme
      // exactly as a viewer's does - that is the point of saving it - so the tests say which file that memory
      // lives in, and name one that does not exist. Every run then starts as the application ships, in the
      // factory's colours, and only a test's own Apply changes what the application wears. Without this, one
      // click on the real settings screen would recolour every test on the machine - which is exactly what it
      // did once.
      Environment.SetEnvironmentVariable("MP2_THEME_FILE",
          Path.Combine(Path.GetTempPath(), "MediaPortal2-Tests-" + Guid.NewGuid().ToString("N") + "-theme.json"));

      // The same isolation for the skins folder on disk: a test run must not read the skins the machine keeps
      // at D:\Mediaportal\TIF\skins, because those files belong to whoever edits them, and a test whose result
      // changes when a skin file is edited at lunchtime is not a test. The skin tests say which folder the
      // engine reads - either a folder of their own on disk or the copy inside the assembly.
      Environment.SetEnvironmentVariable("MP2_SKINS_DIR",
          Path.Combine(Path.GetTempPath(), "MediaPortal2-Tests-" + Guid.NewGuid().ToString("N") + "-skins"));
    }

    public static AppBuilder BuildAvaloniaApp()
    {
      return AppBuilder.Configure<App>()
          .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
          .WithInterFont();
    }
  }
}
