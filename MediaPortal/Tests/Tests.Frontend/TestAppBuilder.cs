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
    public static AppBuilder BuildAvaloniaApp()
    {
      return AppBuilder.Configure<App>()
          .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true })
          .WithInterFont();
    }
  }
}
