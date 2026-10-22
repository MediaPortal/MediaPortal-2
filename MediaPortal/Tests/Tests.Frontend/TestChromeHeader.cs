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
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.Views;
using NUnit.Framework;

namespace Tests.Frontend
{
  /// <summary>
  /// The head line of the worn skin, measured on the screen it is drawn on. The reference's header row was
  /// built for a television: its clock, its two quiet weather lines and its weather picture want their full
  /// width and are placed from the right, so on a wide screen nothing is ever asked to give way. A window that
  /// is narrower than that row is where the row has to make a decision, and the tests here are about which one
  /// it makes: the reference let its clock run past the edge of the master and the last second of it disappeared
  /// off the television, which is a poor way to learn what time it is.
  /// </summary>
  [TestFixture]
  public class TestChromeHeader
  {
    /// <summary>A window wide enough that nothing in the head line has to give way.</summary>
    private const double WIDE = 1280;

    /// <summary>
    /// A window narrower than the head line asks for - the clock, the day over the temperature, the weather
    /// picture and the gutters between them together want more than this.
    /// </summary>
    private const double NARROW = 560;

    [AvaloniaTest]
    public void TheTimeKeepsItsSizeOnAWideScreen()
    {
      WithHeader(WIDE, out ShellWindow window, out TextBlock clock, out double width);
      try
      {
        Assert.That(width, Is.EqualTo(WIDE));
        Assert.That(WidthOnTheScreen(window, clock), Is.EqualTo(clock.DesiredSize.Width).Within(0.5),
            "on a screen with room the clock is drawn at the size the skin asked for, not scaled");
        Assert.That(RightEdgeOnTheScreen(window, clock), Is.LessThan(WIDE), "the clock stands inside the window");
        Assert.That(RightEdgeOnTheScreen(window, clock), Is.GreaterThan(WIDE - 40),
            "and it stands at the right edge of the screen, where the reference's master docked it");
      }
      finally
      {
        Close(window);
      }
    }

    [AvaloniaTest]
    public void TheTimeIsNeverCutOffWhenTheWindowIsNarrowerThanTheHeadLine()
    {
      WithHeader(NARROW, out ShellWindow window, out TextBlock clock, out double width);
      try
      {
        Assert.That(width, Is.EqualTo(NARROW));
        double right = RightEdgeOnTheScreen(window, clock);
        Assert.That(right, Is.LessThanOrEqualTo(NARROW),
            "the last digit of the clock has to stay on the screen, however narrow the window gets");
        Assert.That(LeftEdgeOnTheScreen(window, clock), Is.GreaterThanOrEqualTo(0),
            "and it stays there by standing whole on the screen, not by hanging off its left");
        Assert.That(WidthOnTheScreen(window, clock), Is.LessThan(clock.DesiredSize.Width),
            "the head line gives way by shrinking as a whole, which is how it stays on the screen");
        Assert.That(WidthOnTheScreen(window, clock), Is.GreaterThan(0), "and shrinking it does not mean hiding it");
      }
      finally
      {
        Close(window);
      }
    }

    /// <summary>
    /// Where the clock is drawn, in the window's own coordinates. The head line is scaled as a whole when it has
    /// to be, so a control inside it keeps the size it asked for while the picture of it on the screen is
    /// smaller; a test that read <c>Bounds</c> would be reading the wish and not the drawing, which is exactly
    /// the difference between a clock that fits and one whose last digit is off the television.
    /// </summary>
    private static double LeftEdgeOnTheScreen(ShellWindow window, TextBlock clock)
    {
      return Where(window, clock, new Point(0, 0)).X;
    }

    private static double RightEdgeOnTheScreen(ShellWindow window, TextBlock clock)
    {
      return Where(window, clock, new Point(clock.Bounds.Width, 0)).X;
    }

    private static double WidthOnTheScreen(ShellWindow window, TextBlock clock)
    {
      return RightEdgeOnTheScreen(window, clock) - LeftEdgeOnTheScreen(window, clock);
    }

    private static Point Where(ShellWindow window, TextBlock clock, Point inside)
    {
      Point? onScreen = clock.TranslatePoint(inside, window);
      Assert.That(onScreen, Is.Not.Null, "the clock is not on the screen at all");
      return onScreen!.Value;
    }

    /// <summary>
    /// Put the worn skin's head line on the screen at the width the test asks for, with everything the weather
    /// has to say on it - the day, the temperature, the picture and the clock - because the complaint was about
    /// the last digit of the clock, and that digit only runs off when the whole row is standing there.
    /// </summary>
    private static void WithHeader(double width, out ShellWindow window, out TextBlock clock,
        out double measured)
    {
      ShellWindow shown = new ShellWindow(new Label());
      shown.Width = width;
      shown.Height = 720;
      shown.Show();
      Pump(6);

      // The skin goes on after the window is open, which is when the window learns what to wear: it listens to
      // the engine, and the head line is put on by that listening, exactly as it is when somebody switches the
      // theme on the couch.
      Application app = Application.Current!;
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "applied");

      ChromeInfo chrome = shown.Chrome;
      chrome.ShowWeatherSlot = true;
      chrome.Clock = "19:12";
      chrome.Date = "Do 22 Oktober";
      chrome.WeatherTemperature = "8°C";
      chrome.WeatherIcon = "Sunny.png";
      Pump(12);
      shown.UpdateLayout();

      TextBlock? found = shown.GetVisualDescendants().OfType<TextBlock>()
          .FirstOrDefault(t => t.Text == chrome.Clock);
      Assert.That(found, Is.Not.Null,
          "the head line of the worn skin did not draw a clock; the window said: "
          + string.Join(" | ", shown.GetVisualDescendants().OfType<TextBlock>().Select(t => "'" + t.Text + "'")));
      window = shown;
      clock = found!;
      measured = shown.Bounds.Width;
    }

    private static void Pump(int times)
    {
      for (int i = 0; i < times; i++)
      {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(5);
      }
    }

    /// <summary>
    /// Take the screen down again and hand the application's resources back, so that a suite which ran this
    /// first does not leave every later screen wearing Nereus.
    /// </summary>
    private static void Close(ShellWindow window)
    {
      window.Close();
      SkinEngine.Clear(Application.Current!);
      Pump(4);
    }
  }
}
