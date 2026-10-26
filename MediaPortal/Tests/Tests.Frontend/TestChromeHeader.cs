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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MediaPortal.Frontend.Notifications;
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

    private string _skinsFolderBefore = "";

    [SetUp]
    public void RememberTheSkinsFolder()
    {
      _skinsFolderBefore = SkinEngine.SkinsFolder;
    }

    [TearDown]
    public void PutTheSkinsFolderBack()
    {
      SkinEngine.SkinsFolder = _skinsFolderBefore;
    }

    /// <summary>
    /// Let the worn skin read its pictures from the folder this front end was built from. A skin's markup rides inside
    /// the assembly, but the icons it carries are loose files, and the folder a test run lives in does not hold a whole
    /// skin - so a glyph asked for during a test would be answered by nothing at all, which is a fact about the test
    /// run and not about the skin (the same trick the editor's glyph test uses).
    /// </summary>
    private static void WearTheShippedSkinPictures()
    {
      AssemblyMetadataAttribute? value = typeof(TestChromeHeader).Assembly
          .GetCustomAttributes<AssemblyMetadataAttribute>()
          .FirstOrDefault(a => string.Equals(a.Key, "FrontendSource", StringComparison.Ordinal));

      Assert.That(value?.Value, Is.Not.Null.And.Not.Empty,
          "the test project has to hand FrontendSource over for the skin's pictures to be checkable");

      string folder = System.IO.Path.Combine(value!.Value!, "Skins");
      Assert.That(System.IO.Directory.Exists(folder), Is.True, "there is no skins folder at " + folder);
      SkinEngine.SkinsFolder = folder;
    }

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

        // The head line stands inside the worn skin, and the shell stands the skin at one factor over the
        // window - Nereus is written for a television of 1080 lines and this window is shorter than that.  The
        // question this test asks is about the head line's OWN shrinking, so the SIZE is asked in the skin's
        // points: divided by the factor the skin stands at, the clock has to be the size the skin asked for.
        // Where it STANDS stays a matter of the window, because that is where the viewer looks.
        double scale = window.SkinScale;
        Assert.That(WidthOnTheScreen(window, clock) / scale, Is.EqualTo(clock.DesiredSize.Width).Within(0.5),
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
    /// The complaints these tests exist for. The front end first wrote its sentence about the server into the corner
    /// above the clock, which is the one corner of the worn skin's head line that belongs to the time; it then wrote
    /// the state as a word next to a picture, in a bar that stood at the left end of the line and so lay across the
    /// home's menu, which this very line floats over. The reference said all of it in pictures - its importer was a
    /// circle with a percentage under it (Buttons.xaml:1740 with NotificationsBar.inc:29-31), its recording a blinking
    /// picture with nothing beside it (Buttons.xaml:1717) - in the master's own top row
    /// (master_home.xaml:184, master_bare.xaml:109), with the clock still the furthest thing right.
    /// </summary>
    [AvaloniaTest]
    public void WhatTheServerIsDoingIsSaidInPicturesAndNeverInWords()
    {
      WearTheShippedSkinPictures();
      WithHeader(WIDE, out ShellWindow window, out TextBlock clock, out double width);
      try
      {
        window.Chrome.ReportServerActivity(ServerActivityState.Disconnected,
            "Lost the server. Trying again in 5 seconds.");
        Pump(6);

        foreach (string word in new[] { "disconnected", "connected", "connecting", "Lost the server. Trying again in 5 seconds." })
          Assert.That(SaidOnTheScreen(window, word), Is.Null,
              "the bar says a state with its picture, and the sentence waits on the tooltip; the line drew: "
              + OnTheScreen(window));

        List<Avalonia.Controls.Shapes.Path> drawn = GlyphsOnTheScreen(window);
        Assert.That(drawn, Is.Not.Empty, "the state is drawn, though - as a picture out of the worn skin");
        foreach (Avalonia.Controls.Shapes.Path glyph in drawn)
        {
          Assert.That(RightEdgeOnTheScreen(window, glyph), Is.LessThanOrEqualTo(LeftEdgeOnTheScreen(window, clock)),
              "and every one of them stands left of the time, which keeps the corner furthest right");
          Assert.That(LeftEdgeOnTheScreen(window, glyph), Is.GreaterThan(width / 5),
              "and at the right end of the line rather than its left, where a screen's own words - and at home the "
              + "menu this line floats over - have to be left alone");
        }
      }
      finally
      {
        Close(window);
      }
    }

    /// <summary>
    /// The hint, on the screen: the picture of the loudest thing waiting and, once a second thing joins, the number of
    /// them in brackets under it. This is the reference's notification button (Buttons.xaml:1983-2029), whose picture
    /// was the picture of the loudest notification and whose badge counted the rest.
    /// </summary>
    [AvaloniaTest]
    public void AThingWaitingToBeReadStandsInTheHeadLineLeftOfTheClock()
    {
      WearTheShippedSkinPictures();
      WithHeader(WIDE, out ShellWindow window, out TextBlock clock, out double width);
      try
      {
        NotificationQueue queue = new NotificationQueue();
        window.Chrome.AttachNotifications(queue);
        queue.Enqueue(NotificationType.Info, "the server was quicker", "it dropped two frames");
        queue.Enqueue(NotificationType.Error, "the television stopped", "the card fell out");
        Pump(6);

        TextBlock? counted = SaidOnTheScreen(window, "(2)");
        Assert.That(counted, Is.Not.Null, "the hint counts what is waiting, in brackets; the line drew: "
            + OnTheScreen(window));
        Assert.That(RightEdgeOnTheScreen(window, counted!), Is.LessThan(LeftEdgeOnTheScreen(window, clock)),
            "and it does so left of the clock, which keeps the corner the reference gave the time");

        queue.Dequeue();
        Pump(6);
        Assert.That(SaidOnTheScreen(window, "(1)"), Is.Null,
            "one notification is a picture that says it all - the reference counted only from the second one "
            + "(NotificationsModel.cs:109-112)");

        List<Avalonia.Controls.Shapes.Path> drawn = GlyphsOnTheScreen(window);
        Assert.That(drawn, Is.Not.Empty,
            "and the hint stands on a picture out of the skin's own vectors, not beside an empty space");
      }
      finally
      {
        Close(window);
      }
    }

    /// <summary>
    /// The pictures of the bar stand on one line. They are cut from different shapes - the connected mark is a wide
    /// link, half as high as it is wide, the recording a full circle - so a mark that keeps to the top of the plate it
    /// was given makes the row walk downhill, which is what this catches: every picture has to come to rest with its
    /// own middle on the middle of the line, however it is shaped.
    /// </summary>
    [AvaloniaTest]
    public void ThePicturesOfTheBarStandOnOneLine()
    {
      WearTheShippedSkinPictures();
      WithHeader(WIDE, out ShellWindow window, out TextBlock clock, out double width);
      try
      {
        NotificationQueue queue = new NotificationQueue();
        window.Chrome.AttachNotifications(queue);
        queue.Enqueue(NotificationType.Info, "the server was quicker", "it dropped two frames");
        window.Chrome.ReportServerActivity(ServerActivityState.Filing, "Filing the library", 40);
        window.Chrome.ReportRecording(true);
        window.Chrome.ReportRecordingNotice("The nightly film is being written down");
        Pump(6);

        List<Avalonia.Controls.Shapes.Path> drawn = GlyphsOnTheScreen(window);
        Assert.That(drawn.Count, Is.GreaterThanOrEqualTo(3),
            "the hint, what the server is doing and the recording are all standing on the line; the line drew: "
            + OnTheScreen(window));

        double middle = HeightOnTheScreen(window, drawn[0]);
        foreach (Avalonia.Controls.Shapes.Path glyph in drawn)
        {
          Assert.That(HeightOnTheScreen(window, glyph), Is.EqualTo(middle).Within(1.0),
              "a wide link, a percentage circle and a round recording mark have to sit on one line, not one at the "
              + "top of its plate and the next at the bottom of its own");

          // And the middle of the box has to be the middle of the drawing: a shape stretched into a box of a told
          // size keeps to the top of that box, so a box wider than its art - which a mark cut from a wide link is
          // the moment somebody hands it a square - shows a mark that sits above the line it is supposed to be on.
          double plate = glyph.Bounds.Width / glyph.Bounds.Height;
          double art = glyph.Data!.Bounds.Width / glyph.Data!.Bounds.Height;
          Assert.That(plate, Is.EqualTo(art).Within(0.05 * art),
              "the picture of " + (glyph.Data!.Bounds.Width > glyph.Data!.Bounds.Height
                  ? "a wide mark" : "a tall mark") + " was stretched into a plate that does not hug it, so it is not "
                  + "standing where its plate says it is");
        }
      }
      finally
      {
        Close(window);
      }
    }

    /// <summary>
    /// The pictures the head line is drawing right now - a shape with a geometry behind it that has not been asked to
    /// hide. A glyph whose name the skin does not carry loads no geometry, so an empty list here means the bar drew
    /// nothing at all.
    /// </summary>
    private static List<Avalonia.Controls.Shapes.Path> GlyphsOnTheScreen(ShellWindow window)
    {
      return window.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>()
          .Where(p => p.Data != null && p.IsEffectivelyVisible && p.Bounds.Width > 20).ToList();
    }

    [Test]
    public void TheWordsAndPicturesForTheServerStatesAreTheOnesTheHouseholdRecognises()
    {
      Assert.That(ServerActivity.Glyph(ServerActivityState.Connected), Is.EqualTo("Notification/connected"),
          "listening to the server is said as connected");
      Assert.That(ServerActivity.Glyph(ServerActivityState.Disconnected), Is.EqualTo("Notification/disconnected"),
          "and losing it, or being refused at the door, as disconnected");
      Assert.That(ServerActivity.Words(ServerActivityState.NotListening), Is.EqualTo(""));
      Assert.That(ServerActivity.Glyph(ServerActivityState.NotListening), Is.EqualTo(""));
    }

    /// <summary>
    /// A head line that draws a picture the worn skin does not carry would draw nothing, and the state would go
    /// uncommunicated in exactly the moment it matters. So the names are asked of the skin they are drawn from.
    /// </summary>
    [AvaloniaTest]
    public void ThePicturesTheHeadLineDrawsAreCarriedByTheWornSkin()
    {
      Application app = Application.Current!;
      WearTheShippedSkinPictures();
      Assert.That(SkinEngine.Apply(app, "nereus", out string? problem), Is.True, problem ?? "applied");
      try
      {
        WornSkin worn = SkinEngine.Worn(app)!;
        List<string> wanted = new List<string>
        {
          ServerActivity.Glyph(ServerActivityState.Connected),
          ServerActivity.Glyph(ServerActivityState.Disconnected),
          ServerActivity.Glyph(ServerActivityState.Filing),
          ServerActivity.Glyph(ServerActivityState.Connecting),
          "Notification/recording"
        };
        foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
          wanted.Add(UiNotification.IconFor(type));

        foreach (string glyph in wanted)
        {
          Assert.That(glyph, Is.Not.Empty, "a state the head line knows nothing about is not a state");
          Assert.That(worn.Vectors.Has(glyph), Is.True, "the worn skin carries no picture called " + glyph);
          Assert.That(worn.Vectors.Load(glyph), Is.Not.Null,
              "the worn skin has " + glyph + " but it does not read as a shape");
        }
      }
      finally
      {
        SkinEngine.Clear(app);
      }
    }

    /// <summary>The words a head line is saying, for a failure message that has to be read by a person.</summary>
    private static string OnTheScreen(ShellWindow window)
    {
      return string.Join(" | ", window.GetVisualDescendants().OfType<TextBlock>().Select(t => "'" + t.Text + "'"));
    }

    /// <summary>Finds a word the head line is drawing, by what it says.</summary>
    private static TextBlock? SaidOnTheScreen(ShellWindow window, string words)
    {
      return window.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Text == words);
    }

    /// <summary>
    /// Where the clock is drawn, in the window's own coordinates. The head line is scaled as a whole when it has    /// to be, so a control inside it keeps the size it asked for while the picture of it on the screen is
    /// smaller; a test that read <c>Bounds</c> would be reading the wish and not the drawing, which is exactly
    /// the difference between a clock that fits and one whose last digit is off the television.
    /// </summary>
    private static double LeftEdgeOnTheScreen(ShellWindow window, Control thing)
    {
      return Where(window, thing, new Point(0, 0)).X;
    }

    private static double RightEdgeOnTheScreen(ShellWindow window, Control thing)
    {
      return Where(window, thing, new Point(thing.Bounds.Width, 0)).X;
    }

    private static double WidthOnTheScreen(ShellWindow window, Control thing)
    {
      return RightEdgeOnTheScreen(window, thing) - LeftEdgeOnTheScreen(window, thing);
    }

    /// <summary>The height a thing is standing at on the screen, taken through its own middle.</summary>
    private static double HeightOnTheScreen(ShellWindow window, Control thing)
    {
      return Where(window, thing, new Point(thing.Bounds.Width / 2, thing.Bounds.Height / 2)).Y;
    }

    private static Point Where(ShellWindow window, Control thing, Point inside)
    {
      Point? onScreen = thing.TranslatePoint(inside, window);
      Assert.That(onScreen, Is.Not.Null, "the thing is not on the screen at all");
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
