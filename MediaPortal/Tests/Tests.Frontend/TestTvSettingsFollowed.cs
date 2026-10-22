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
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// The television settings being followed by the screens they belong to, rather than merely being written down:
  /// the guide making its columns out of the hours it was given, dropping the episode line it was told to drop, and
  /// hiding the bouquet it was told to hide; the tuner waiting the length it was told to wait and reading a typed
  /// number the way it was told to read it; and the watching screen starting a picture only when it was asked to.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A settings file nobody obeys is worse than none, because it makes a viewer believe they have changed something.
  /// The reference read its settings in exactly the places these tests look at: the hours and the rows at the grid's
  /// own construction (<c>Controls/EpgGrid.cs:93-99</c>), the two channel names in the header's width
  /// (<c>Skin/Nereus/screens/SlimTvClient-fullguide.xaml:22-30</c>), the series line where a programme's words are
  /// written (<c>ProgramListItem.cs:112</c>), the hidden bouquet and the meaning of a typed number when the channel
  /// list is built (<c>Helpers/ChannelContext.cs:190-194</c> and <c>218-231</c>), the timeout and the automatic start
  /// where the zapping and the television's home are (<c>Models/ChannelZapModel.cs:99</c> and
  /// <c>Models/SlimTvClientModel.cs:553-565</c>).
  /// </para>
  /// <para>
  /// The guide is deliberately made to stand on a window it did not have to be asked about: its columns are floored
  /// from the epoch, so a programme anchored to the same floor lands on a column line at any width, which is what
  /// lets these tests name an exact column for a grid six hours wide instead of settling for "somewhere near the
  /// middle".
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvSettingsFollowed
  {
    #region The scripted backend

    /// <summary>
    /// Two bouquets, the first of them named the one the reference's <c>HideAllChannelsGroup</c> switch threw away.
    /// It is first on purpose: a guide that offers it opens on it, so a test that hides it is watching a bouquet
    /// disappear from where it would otherwise be standing.
    /// </summary>
    private const string BOUQUETS_JSON = "{\"total\":2,\"entries\":["
        + "{\"uuid\":\"all\",\"name\":\"All Channels\",\"services\":[\"s1\",\"s2\",\"s3\"]},"
        + "{\"uuid\":\"family\",\"name\":\"Family\",\"services\":[\"s1\",\"s3\"]}]}";

    /// <summary>
    /// Three television channels whose printed numbers are deliberately not their places: ZDF is the second channel
    /// of the bouquet but is printed 12, and Sparten the third but printed 44. That is the whole difference between
    /// the reference's two ways of reading a number typed on a remote.
    /// </summary>
    private const string CHANNELS_JSON = "{\"total\":3,\"entries\":["
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\","
        + "\"streamurl\":\"//stream/channelid/3\",\"tags\":[\"all\",\"family\"],\"services\":[\"s1\"]},"
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\","
        + "\"streamurl\":\"//stream/channelid/12\",\"tags\":[\"all\"],\"services\":[\"s2\"]},"
        + "{\"number\":44,\"name\":\"Sparten\",\"epgchannelid\":\"epg-sparten\","
        + "\"streamurl\":\"//stream/channelid/44\",\"tags\":[\"all\",\"family\"],\"services\":[\"s3\"]}]}";

    private const string SERVICES_JSON = "{\"total\":3,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1},{\"uuid\":\"s3\",\"type\":1}]}";

    /// <summary>
    /// The moment a guide of this width opens on: the epoch floored to a whole number of columns, which is how the
    /// guide itself floors it. Every fixture hangs off this, so a programme can be expected in an exact column.
    /// </summary>
    private static long FloorTo(int columnSeconds)
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      return now - now % columnSeconds;
    }

    /// <summary>
    /// A film three hours into the window on the first channel, carrying its episode and its genre; a bulletin
    /// running now on the second, so that the guide has something to stand on when it opens.
    /// </summary>
    private static string EventsFor(int columnSeconds)
    {
      long start = FloorTo(columnSeconds);
      return "{\"total\":2,\"entries\":["
          + "{\"event_id\":7001,\"start\":" + (start + 10800) + ",\"stop\":" + (start + 14400)
          + ",\"title\":\"Tatort\",\"contentLabel\":\"Movie\",\"genre\":[6],"
          + "\"seasonNumber\":7,\"episodeNumber\":3,\"episodeOnscreen\":\"S07 E03\","
          + "\"channel\":\"epg-ard\",\"channelname\":\"Das Erste\"},"
          + "{\"event_id\":7002,\"start\":" + (start - 300) + ",\"stop\":" + (start + 3300)
          + ",\"title\":\"Tagesschau\",\"contentLabel\":\"News\",\"channel\":\"epg-zdf\","
          + "\"channelname\":\"ZDF\"}]}";
    }

    private class Scripted : HttpMessageHandler
    {
      /// <summary>How wide a column the fixtures were hung on, so that the columns come out whole.</summary>
      internal int ColumnSeconds = 120;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUETS_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal) ? CHANNELS_JSON
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : path.EndsWith("/api/epg/events", StringComparison.Ordinal) ? EventsFor(ColumnSeconds)
            : "{\"total\":0,\"entries\":[]}";

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    #endregion

    #region Putting a guide or a tuner on a backend

    private static TvBackendSettings Backend()
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return settings;
    }

    /// <summary>
    /// A guide wide enough to hold the fixtures, which hang three hours into the window: at the reference's own two
    /// hours, or this guide's default three, the film would be standing just off the right-hand edge.
    /// </summary>
    private static TvBackendSettings WideBackend()
    {
      TvBackendSettings settings = Backend();
      settings.VisibleHours = 6;
      return settings;
    }

    private static Scripted WideScripted()
    {
      return new Scripted { ColumnSeconds = 240 };
    }

    private static TvGuideViewModel OpenedGuide(TvBackendSettings settings, Scripted backend)
    {
      TvGuideViewModel guide = new TvGuideViewModel(TvMediaType.Television, delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); });
      guide.ReloadAsync().GetAwaiter().GetResult();
      return guide;
    }

    /// <summary>A tuner over this backend, not yet asked for anything.</summary>
    private static TvTunerViewModel TunerOn(TvBackendSettings settings, Scripted backend)
    {
      return new TvTunerViewModel(TvMediaType.Television, delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); });
    }

    private static TvTunerViewModel OpenedTuner(TvBackendSettings settings, Scripted backend)
    {
      TvTunerViewModel tuner = TunerOn(settings, backend);
      tuner.ReloadAsync().GetAwaiter().GetResult();
      return tuner;
    }

    private static TvGuideBlock Block(TvGuideViewModel guide, string title)
    {
      TvGuideBlock? block = guide.Rows.SelectMany(r => r.Blocks).FirstOrDefault(b => b.Title == title);
      Assert.That(block, Is.Not.Null, "no '" + title + "' on the grid; the grid has "
          + string.Join(", ", guide.Rows.SelectMany(r => r.Blocks).Select(b => b.Title)));
      return block!;
    }

    #endregion

    #region The guide, made out of the answers it was given

    [Test]
    public void TheGuideMakesItsColumnsOutOfTheHoursItWasToldToShow()
    {
      TvBackendSettings settings = Backend();
      settings.VisibleHours = 6;
      Scripted backend = new Scripted();
      backend.ColumnSeconds = 240;

      TvGuideViewModel guide = OpenedGuide(settings, backend);

      Assert.That(guide.ColumnMinutes, Is.EqualTo(4),
          "the reference divided its hours by its seventy-five columns (EpgGrid.cs:99); this guide has ninety, and "
          + "six hours across ninety columns is four minutes a column");
      Assert.That(guide.WindowMinutes, Is.EqualTo(360));
      Assert.That(guide.End - guide.Start, Is.EqualTo(TimeSpan.FromHours(6)));

      // "Tatort" was hung exactly three hours into the window, which at four minutes a column is column forty-five.
      Assert.That(Block(guide, "Tatort").Column, Is.EqualTo(45),
          "a programme has to stand in the column its own beginning falls in at whatever width the viewer asked "
          + "for, or the hour labels above it are lying");
      Assert.That(Block(guide, "Tatort").ColumnSpan, Is.EqualTo(15), "and it has to be as long as it really is");
    }

    [Test]
    public void TheDefaultWindowIsTheOneTheGuideAlwaysHad()
    {
      TvGuideViewModel guide = OpenedGuide(Backend(), new Scripted());

      Assert.That(guide.ColumnMinutes, Is.EqualTo(2), "three hours over ninety columns, which is what this guide "
          + "drew before the number was a setting at all");
      Assert.That(guide.WindowMinutes, Is.EqualTo(TvGuideViewModel.WINDOW_MINUTES));
      Assert.That(guide.VisibleRows, Is.EqualTo(9), "the reference's own number of rows");
    }

    [Test]
    public void ANumberOfRowsIsCarriedToWhateverDrawsTheGrid()
    {
      TvBackendSettings settings = Backend();
      settings.VisibleRows = 4;

      TvGuideViewModel guide = OpenedGuide(settings, new Scripted());

      Assert.That(guide.VisibleRows, Is.EqualTo(4),
          "the screen divides the room it has by this number, exactly as the reference divided its own height by "
          + "EpgNumberOfRows (EpgGrid.cs:428)");
    }

    [Test]
    public void AEpisodeLineIsSaidOrNotSaidAsTheViewerAsked()
    {
      TvGuideViewModel with = OpenedGuide(WideBackend(), WideScripted());
      Assert.That(Block(with, "Tatort").SeriesLine, Is.Not.Empty,
          "the reference showed a programme's series and episode unless a viewer said otherwise");

      with.Press(Block(with, "Tatort"));
      Assert.That(with.DetailsSeries, Is.Not.Empty, "and the words along the bottom are the same answer");

      TvBackendSettings quiet = WideBackend();
      quiet.ShowSeriesInfo = false;
      TvGuideViewModel without = OpenedGuide(quiet, WideScripted());

      Assert.That(Block(without, "Tatort").SeriesLine, Is.Empty,
          "its ShowSeriesInfo switch hid the line (ProgramListItem.cs:112), which is the same switch by another "
          + "name here");
      without.Press(Block(without, "Tatort"));
      Assert.That(without.DetailsSeries, Is.Empty);
      Assert.That(Block(without, "Tatort").Title, Is.EqualTo("Tatort"),
          "and the programme itself is still named, which is the whole point of hiding one line rather than the "
          + "block");
    }

    [Test]
    public void TheBlocksAreOneColourWhenSomebodyRodeTheGenreColours()
    {
      TvGuideViewModel coloured = OpenedGuide(WideBackend(), WideScripted());
      Assert.That(Block(coloured, "Tatort").Fill, Is.EqualTo(TvGenreColors.MOVIE),
          "a film is painted the colour the skin gives films");

      TvBackendSettings flat = WideBackend();
      flat.ShowGenreColors = false;
      TvGuideViewModel plain = OpenedGuide(flat, WideScripted());

      Assert.That(plain.Rows.SelectMany(r => r.Blocks).Select(b => b.Fill).Distinct().ToList(),
          Is.EqualTo(new[] { TvGenreColors.FLAT }),
          "with ShowEpgGenreColors off the reference filled every block with the same thing, and a guide that "
          + "changes the meaning of every other switch but this one would be a guide with a dead switch");
    }

    [Test]
    public void TheBouquetCalledAllChannelsIsOfferedOrNotAsTheViewerAsked()
    {
      TvGuideViewModel shown = OpenedGuide(Backend(), new Scripted());
      Assert.That(shown.Groups.Select(g => g.Name), Is.EqualTo(new[] { "All Channels", "Family" }));

      TvBackendSettings hiding = Backend();
      hiding.HideAllChannelsGroup = true;
      TvGuideViewModel without = OpenedGuide(hiding, new Scripted());

      Assert.That(without.Groups.Select(g => g.Name), Is.EqualTo(new[] { "Family" }),
          "the reference's filter threw the bouquet named \"All Channels\" away by name and nothing else "
          + "(ChannelContext.cs:218-231)");
      Assert.That(without.SelectedGroup!.Name, Is.EqualTo("Family"),
          "and a guide that opens on a bouquet has to open on one it actually has");
    }

    [Test]
    public void TheGuideFollowsTheSettingsThatWereChangedWhileItStoodThere()
    {
      TvBackendSettings settings = Backend();
      Scripted backend = new Scripted();
      TvGuideViewModel guide = OpenedGuide(settings, backend);
      Assert.That(guide.ColumnMinutes, Is.EqualTo(2));

      // Somebody changes the settings screen and comes back to a guide that was already open.
      settings.VisibleHours = 6;
      backend.ColumnSeconds = 240;
      guide.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(guide.ColumnMinutes, Is.EqualTo(4),
          "the grid is rebuilt out of what is true when it is built, which is the reference's rule - it read its "
          + "settings once per grid and never while one was being looked at");
      Assert.That(guide.WindowMinutes, Is.EqualTo(360));
    }

    #endregion

    #region The zapping, done the way it was asked for

    [Test]
    public void ANumberTypedMeansAPlaceInTheBouquetUnlessTheViewerWantsThePrintedNumber()
    {
      TvTunerViewModel byPlace = OpenedTuner(Backend(), new Scripted());
      Assert.That(byPlace.ZapByChannelIndex, Is.True, "on unless somebody turned it off");

      byPlace.PressDigit(2);
      Assert.That(byPlace.CommitDigits(), Is.True);
      Assert.That(byPlace.Tuned!.Name, Is.EqualTo("ZDF"),
          "the second channel of the bouquet, whatever is printed on it - the reference renumbered the bouquet one "
          + "to n for exactly this (ChannelContext.cs:190-194)");

      TvBackendSettings byNumber = Backend();
      byNumber.ZapByChannelIndex = false;
      TvTunerViewModel printed = OpenedTuner(byNumber, new Scripted());

      printed.PressDigit(1);
      printed.PressDigit(2);
      Assert.That(printed.CommitDigits(), Is.True);
      Assert.That(printed.Tuned!.Name, Is.EqualTo("ZDF"),
          "off means the number printed on the channel, which for ZDF is 12 and not 2 "
          + "(ChannelZapModel.cs:173-181)");

      printed.PressDigit(2);
      Assert.That(printed.CommitDigits(), Is.False);
      Assert.That(printed.Status, Does.Contain("2"),
          "and a number that is printed on nothing says so, which is the silence the reference left its viewers in");
    }

    [Test]
    public void TheWaitingAfterTheLastDigitComesFromTheSettingsRatherThanFromTheCode()
    {
      TvBackendSettings settings = Backend();
      settings.ZapTimeoutSeconds = 4.5;
      Assert.That(OpenedTuner(settings, new Scripted()).ZapTimeoutSeconds, Is.EqualTo(4.5),
          "the screen hands this to its timer, so a viewer who cannot press two buttons in two seconds is not left "
          + "on two channels for one number");

      TvBackendSettings nonsense = Backend();
      nonsense.ZapTimeoutSeconds = 900;
      Assert.That(OpenedTuner(nonsense, new Scripted()).ZapTimeoutSeconds,
          Is.EqualTo(TvTunerViewModel.ZAP_TIMEOUT_SECONDS),
          "a timeout of a quarter of a minute is a television that seems to have stopped, so the reference's own "
          + "two seconds answer instead");
    }

    [Test]
    public void TheWatchingScreenStaysDarkUntilSomebodyAsksUnlessItIsToldToStartItself()
    {
      List<TvChannel> askedFor = new List<TvChannel>();

      TvTunerViewModel quiet = TunerOn(Backend(), new Scripted());
      quiet.PlayRequested += delegate (TvChannel channel, MediaPortal.Frontend.Player.MediaLocation location)
      {
        askedFor.Add(channel);
      };
      quiet.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(quiet.Tuned, Is.Null,
          "the reference did not start a picture on its own, and a box that makes noise before anybody has chosen "
          + "anything is a box nobody wants in a bedroom");
      Assert.That(askedFor, Is.Empty);

      TvBackendSettings eager = Backend();
      eager.AutoStartTV = true;
      TvTunerViewModel keen = TunerOn(eager, new Scripted());
      keen.PlayRequested += delegate (TvChannel channel, MediaPortal.Frontend.Player.MediaLocation location)
      {
        askedFor.Add(channel);
      };
      keen.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(keen.Tuned, Is.Not.Null, "with the switch on, arriving at Live TV is arriving at television");
      Assert.That(keen.Tuned!.Name, Is.EqualTo("Das Erste"),
          "the channel the navigation stands on, which is where the reference's last channel was kept "
          + "(SlimTvClientModel.cs:562-565) - and this front end has no memory of a last channel yet");
      Assert.That(askedFor.Select(c => c.Name).ToList(), Is.EqualTo(new[] { "Das Erste" }));
    }

    [Test]
    public void AChannelSomebodyCameToTheScreenWithIsNotOverruledByTheAutomaticStart()
    {
      TvBackendSettings settings = Backend();
      settings.AutoStartTV = true;

      TvTunerViewModel tuner = TunerOn(settings, new Scripted());

      // The way the guide hands a channel over: the tune is asked for before the bouquet has even been read.
      tuner.Tune(new TvChannel(44, "Sparten", "epg-sparten", new Uri("http://tvbox/stream/44"),
          new[] { "all" }, TvMediaType.Television));
      tuner.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Sparten"),
          "somebody who pressed their way here out of a guide wants that programme, not the top of a list");
    }

    #endregion

    #region What the screen draws out of the settings

    private static Window ShowOnScreen(TvGuideViewModel guide)
    {
      Window window = new Window
      {
        Content = new TvGuideView(guide),
        Width = 1280,
        Height = 720
      };
      window.Show();
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
      Dispatcher.UIThread.RunJobs();
      return window;
    }

    /// <summary>
    /// One row of the drawn grid: a grid of the guide's ninety-one columns that is not the twenty-pixel strip of
    /// hours along the top.
    /// </summary>
    private static List<Grid> DrawnRows(Window window)
    {
      return window.GetVisualDescendants().OfType<Grid>()
          .Where(g => g.ColumnDefinitions.Count == TvGuideViewModel.COLUMNS + 1 && g.Height > 20)
          .ToList();
    }

    [Test]
    [AvaloniaTest]
    public void TheColumnOfChannelsIsOnlyAsWideAsWhatItIsAllowedToShow()
    {
      TvGuideViewModel both = OpenedGuide(Backend(), new Scripted());
      Window full = ShowOnScreen(both);
      Assert.That(DrawnRows(full).First().ColumnDefinitions[0].Width.Value, Is.EqualTo(290),
          "ninety pixels for a number and two hundred for a name, which is the reference's own sum "
          + "(SlimTvClient-fullguide.xaml:22-30) less the hundred and sixty it allowed for a logo this client has "
          + "no pictures of");

      TvBackendSettings nameOnly = Backend();
      nameOnly.ShowChannelNumbers = false;
      Window narrow = ShowOnScreen(OpenedGuide(nameOnly, new Scripted()));
      Assert.That(DrawnRows(narrow).First().ColumnDefinitions[0].Width.Value, Is.EqualTo(200));

      List<TextBlock> heads = narrow.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.Text == "3" || t.Text == "12" || t.Text == "44")
          .ToList();
      Assert.That(heads, Is.Empty, "and the number is not drawn narrow, it is not drawn at all");

      TvBackendSettings neither = Backend();
      neither.ShowChannelNumbers = false;
      neither.ShowChannelNames = false;
      Window none = ShowOnScreen(OpenedGuide(neither, new Scripted()));
      Assert.That(DrawnRows(none).First().ColumnDefinitions[0].Width.Value, Is.EqualTo(0),
          "nothing to say at the head of a row is a column of nothing, and the grid gets the width back");
    }

    [Test]
    [AvaloniaTest]
    public void TheRowsAreAsTallAsTheRoomTheyHaveDividedByTheRowsThatWereAskedFor()
    {
      TvBackendSettings few = Backend();
      few.VisibleRows = 3;
      List<Grid> three = DrawnRows(ShowOnScreen(OpenedGuide(few, new Scripted())));

      TvBackendSettings many = Backend();
      many.VisibleRows = 9;
      List<Grid> nine = DrawnRows(ShowOnScreen(OpenedGuide(many, new Scripted())));

      Assert.That(three, Is.Not.Null.And.Not.Empty);
      Assert.That(nine, Is.Not.Null.And.Not.Empty);

      double tall = three.Average(r => r.Height);
      double small = nine.Average(r => r.Height);

      Assert.That(tall, Is.GreaterThan(small * 2),
          "three rows out of the same screen are more than twice the height of nine - asking for fewer channels is "
          + "asking for bigger ones, which is what the reference's ActualHeight / rows meant");
      Assert.That(small, Is.GreaterThanOrEqualTo(20).And.LessThanOrEqualTo(160),
          "and the rows stay readable at both ends of what a viewer can type into the box");
    }

    #endregion
  }
}
