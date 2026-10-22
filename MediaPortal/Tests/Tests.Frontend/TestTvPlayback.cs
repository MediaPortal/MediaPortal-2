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
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// Watching a channel: which address reaches the player, what a typed number means, what the strip says, and
  /// what happens when the backend has nothing to say about the programme.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference's watching surface said almost nothing out loud. A channel with no stream, a card that could
  /// not be had, or a number matching nothing left the viewer on a black screen with no sentence anywhere
  /// (<c>TV/SlimTvClient/Player/Providers/SlimTVMPExtendedProvider.cs:246-247</c>, and
  /// <c>Models/ChannelZapModel.cs:141-181</c>, where an unmatched number simply never fired a tune). A port that
  /// copied that silence would copy the thing that made people give up on it, so most of what is measured here is
  /// the opposite: what is said when a channel cannot be had.
  /// </para>
  /// <para>
  /// The other half of this file is about the digits, because that is the one part of watching television that
  /// cannot be checked by looking. Two seconds of quiet end a number (<c>ZapTimeout</c>, defaulted to two in
  /// <c>Settings/SlimTvClientSettings.cs:47-49</c>), the number counts as a position in the bouquet rather than as
  /// the number printed on a channel (<c>ZapByChannelIndex</c>, on by default), a lone zero is zap-back rather
  /// than channel nought (<c>Models/ChannelZapModel.cs:167-170</c>), and a zero after other digits is a nought,
  /// which is how 101 is spelled. The reference's own digit zap never actually worked - its timer cleared the
  /// number before parsing it - so these expectations are written from what it meant, not from what it did.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestTvPlayback
  {
    #region The scripted backend

    /// <summary>
    /// Three bouquets: the television household's two and one that says radio. The television guide is not
    /// supposed to offer the last one and the radio screen is not supposed to open on the first, which is the
    /// same rule the guide follows and for the same reason - a TvHeadend keeps one list of bouquets for both
    /// halves and only the names tell them apart.
    /// </summary>
    private const string BOUQUETS_JSON = "{\"total\":3,\"entries\":["
        + "{\"uuid\":\"all-tv\",\"name\":\"All headends\",\"services\":[\"s1\",\"s2\",\"s3\"]},"
        + "{\"uuid\":\"family\",\"name\":\"Family\",\"services\":[\"s1\",\"s3\",\"s5\"]},"
        + "{\"uuid\":\"radio-all\",\"name\":\"Radio\",\"services\":[\"s4\"]}]}";

    /// <summary>
    /// Five channels, four of them watchable. "Silent" has no <c>streamurl</c> and no <c>number</c> either, which
    /// is the only way a channel really is unwatchable: TVHeadend answers a numbered channel without a stream URL
    /// with an address built from the number, because a mux with a number is nearly always streaming
    /// (<c>Api/TvHeadendApiClient.cs:291-332</c>). A channel with neither is what a half-configured backend hands
    /// out, and it is the case the reference left the viewer standing in front of a black screen for.
    /// </summary>
    private const string CHANNELS_JSON = "{\"total\":5,\"entries\":["
        + "{\"number\":3,\"name\":\"Das Erste\",\"epgchannelid\":\"epg-ard\","
        + "\"streamurl\":\"//stream/channelid/3\",\"tags\":[\"all-tv\",\"family\"],\"services\":[\"s1\"]},"
        + "{\"number\":12,\"name\":\"ZDF\",\"epgchannelid\":\"epg-zdf\","
        + "\"streamurl\":\"//stream/channelid/12\",\"tags\":[\"all-tv\"],\"services\":[\"s2\"]},"
        + "{\"number\":44,\"name\":\"Sparten\",\"epgchannelid\":\"epg-sparten\","
        + "\"streamurl\":\"//stream/channelid/44\",\"tags\":[\"all-tv\",\"family\"],\"services\":[\"s3\"]},"
        + "{\"name\":\"Silent\",\"epgchannelid\":\"epg-silent\","
        + "\"tags\":[\"family\"],\"services\":[\"s5\"]},"
        + "{\"number\":101,\"name\":\"Antenne\",\"epgchannelid\":\"epg-ant\","
        + "\"streamurl\":\"//stream/channelid/101\",\"tags\":[\"radio-all\"],\"services\":[\"s4\"]}]}";

    /// <summary>The one service of the four that is an audio service, which is what makes a channel radio.</summary>
    private const string SERVICES_JSON = "{\"total\":5,\"entries\":["
        + "{\"uuid\":\"s1\",\"type\":1},{\"uuid\":\"s2\",\"type\":1},{\"uuid\":\"s3\",\"type\":1},"
        + "{\"uuid\":\"s5\",\"type\":1},{\"uuid\":\"s4\",\"type\":2}]}";

    /// <summary>
    /// Now, snapped down to the minute. The strip's end-of-programme time is printed to the minute, and a fixture
    /// anchored to a clock that moves between the answer and the assertion would put the expectation a minute off
    /// whenever a test happened to run across the boundary.
    /// </summary>
    private static long Anchor()
    {
      long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      return now - now % 60;
    }

    /// <summary>
    /// The evening, anchored to the running clock: a bulletin that began ten minutes ago and has fifty minutes
    /// left, the film after it, a sports programme running on a third channel, and nothing at all on ZDF - which
    /// is the case the reference answered with the words "No program data." rather than with a blank cell
    /// (<c>Models/SlimTvClientModel.cs:1151-1171</c>, <c>Language/strings_en.xml:28</c>).
    /// </summary>
    private static string EventsFor(string query)
    {
      long now = Anchor();

      if (query.Contains("channel=epg-ard", StringComparison.Ordinal))
      {
        return "{\"total\":2,\"entries\":["
            + "{\"event_id\":8001,\"start\":" + (now - 600) + ",\"stop\":" + (now + 3000)
            + ",\"title\":\"Tagesschau\",\"contentLabel\":\"News\",\"channel\":\"epg-ard\","
            + "\"channelname\":\"Das Erste\"},"
            + "{\"event_id\":8002,\"start\":" + (now + 3000) + ",\"stop\":" + (now + 8400)
            + ",\"title\":\"Tatort\",\"contentLabel\":\"Movie\",\"channel\":\"epg-ard\","
            + "\"channelname\":\"Das Erste\"}]}";
      }

      if (query.Contains("channel=epg-zdf", StringComparison.Ordinal))
        return "{\"total\":0,\"entries\":[]}";

      return "{\"total\":3,\"entries\":["
          + "{\"event_id\":8001,\"start\":" + (now - 600) + ",\"stop\":" + (now + 3000)
          + ",\"title\":\"Tagesschau\",\"contentLabel\":\"News\",\"channel\":\"epg-ard\","
          + "\"channelname\":\"Das Erste\"},"
          + "{\"event_id\":8002,\"start\":" + (now + 3000) + ",\"stop\":" + (now + 8400)
          + ",\"title\":\"Tatort\",\"contentLabel\":\"Movie\",\"channel\":\"epg-ard\","
          + "\"channelname\":\"Das Erste\"},"
          + "{\"event_id\":8003,\"start\":" + (now - 120) + ",\"stop\":" + (now + 1680)
          + ",\"title\":\"Fußball\",\"contentLabel\":\"Sport\",\"channel\":\"epg-sparten\","
          + "\"channelname\":\"Sparten\"}]}";
    }

    private class ScriptedTuner : HttpMessageHandler
    {
      internal readonly List<string> AskedPaths = new List<string>();
      internal readonly List<string> AskedQueries = new List<string>();

      /// <summary>A status to answer everything with, for a backend that will not talk at all.</summary>
      internal HttpStatusCode? RefuseEverything;

      /// <summary>
      /// Whether the backend refuses to talk about one channel's programmes while still answering the whole
      /// bouquet. This is the ordinary state of a machine whose EPG is half importable, and the strip under the
      /// picture has to survive it.
      /// </summary>
      internal bool RefusesOneChannelGuide;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        AskedPaths.Add(path);
        AskedQueries.Add(request.RequestUri.Query);

        if (RefuseEverything.HasValue)
          return Answer(request, RefuseEverything.Value, "");

        if (RefusesOneChannelGuide && path.EndsWith("/api/epg/events", StringComparison.Ordinal)
            && request.RequestUri.Query.Contains("channel=", StringComparison.Ordinal))
        {
          return Answer(request, HttpStatusCode.NotFound, "");
        }

        string body = path.EndsWith("/api/channeltag/list", StringComparison.Ordinal) ? BOUQUETS_JSON
            : path.EndsWith("/api/channel/grid", StringComparison.Ordinal) ? CHANNELS_JSON
            : path.EndsWith("/api/service/grid", StringComparison.Ordinal) ? SERVICES_JSON
            : path.EndsWith("/api/epg/events", StringComparison.Ordinal) ? EventsFor(request.RequestUri.Query)
            : "{\"total\":0,\"entries\":[]}";

        return Answer(request, HttpStatusCode.OK, body);
      }

      private static Task<HttpResponseMessage> Answer(HttpRequestMessage request, HttpStatusCode code, string body)
      {
        return Task.FromResult(new HttpResponseMessage(code)
        {
          RequestMessage = request,
          Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
      }
    }

    private static TvTunerViewModel TunerWith(ScriptedTuner backend, TvMediaType kind)
    {
      TvBackendSettings settings = new TvBackendSettings();
      settings.Host = "tvbox";
      settings.Port = 9981;
      return new TvTunerViewModel(kind, delegate { return settings; },
          delegate (TvBackendSettings s) { return new TvHeadendApiClient(s, new HttpClient(backend)); });
    }

    /// <summary>Opens the watching screen, which means waiting until the bouquet is actually there.</summary>
    private static TvTunerViewModel Opened(ScriptedTuner backend, TvMediaType kind = TvMediaType.Television)
    {
      TvTunerViewModel tuner = TunerWith(backend, kind);
      tuner.ReloadAsync().GetAwaiter().GetResult();
      return tuner;
    }

    /// <summary>Waits for the question a button or a digit started, which no caller can hand back itself.</summary>
    private static void Settle(TvTunerViewModel tuner)
    {
      tuner.InFlight.GetAwaiter().GetResult();
    }

    /// <summary>
    /// A tuning that records what the screen asked for. Nothing in the front end's test reach plays a real
    /// channel, so the arrangement between the tuner and the application is what is checked here.
    /// </summary>
    private sealed class AsksForPicture
    {
      internal readonly List<TvChannel> Channels = new List<TvChannel>();
      internal readonly List<MediaLocation> Locations = new List<MediaLocation>();
      internal int Stops;

      internal void Attach(TvTunerViewModel tuner)
      {
        tuner.PlayRequested += delegate (TvChannel channel, MediaLocation location)
        {
          Channels.Add(channel);
          Locations.Add(location);
        };
        tuner.StopRequested += delegate { Stops++; };
      }

      internal MediaLocation Only()
      {
        Assert.That(Locations, Has.Count.EqualTo(1), "expected exactly one channel on the screen, found "
            + Locations.Count);
        return Locations[0];
      }
    }

    private static TvChannel Channel(TvTunerViewModel tuner, string name)
    {
      TvChannel? found = tuner.Channels.FirstOrDefault(c => c.Name == name);
      Assert.That(found, Is.Not.Null, "no '" + name + "' to zap to; the screen has "
          + string.Join(", ", tuner.Channels.Select(c => c.Name)));
      return found!;
    }

    #endregion

    #region Asking for a channel

    [Test]
    public void AScreenWithNoBackendNamedAsksNothingAndSaysWhereToSayIt()
    {
      TvTunerViewModel tuner = new TvTunerViewModel(TvMediaType.Television,
          delegate { return new TvBackendSettings(); });
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(tuner.Channels, Is.Empty);
      Assert.That(tuner.IsTuned, Is.False);
      Assert.That(tuner.Status, Does.Contain("settings"),
          "a watching screen with nothing behind it has to say where the address belongs");
      Assert.That(player.Channels, Is.Empty);
    }

    [Test]
    public void TheAddressTheBackendGaveTheChannelIsWhatReachesThePlayer()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.Tune(Channel(tuner, "ZDF")), Is.True);

      MediaLocation asked = player.Only();
      Assert.That(asked.IsFile, Is.False, "a channel is never a file on this machine");
      Assert.That(asked.EngineString, Is.EqualTo("http://tvbox:9981/stream/channelid/12"),
          "the backend's own streaming address, untouched - it knows which profile a mux wants, and guessing at "
          + "one here is how a channel arrives without sound");
      Assert.That(player.Channels[0].Name, Is.EqualTo("ZDF"),
          "the channel travels with its address, because the player has no idea what a channel is");
    }

    [Test]
    public void AChannelWithNoAddressSaysSoAndAsksForNothing()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      // The bouquet has to be walked to the one family channel that has no stream, because a channel the
      // television bouquet does not name is not on the zap list at all.
      tuner.SelectedGroup = tuner.Groups.First(g => g.Name == "Family");
      Settle(tuner);

      Assert.That(tuner.Tune(Channel(tuner, "Silent")), Is.False);
      Assert.That(player.Channels, Is.Empty, "nothing was asked of the player, because there was nothing to play");
      Assert.That(tuner.Status, Does.Contain("no stream address"),
          "the reference said nothing at all here and left a black screen; a machine that says which channel is "
          + "broken is a machine that does not get unplugged");
      Assert.That(tuner.Status, Does.Contain("tvbox"),
          "and it says which machine said it, since a household can have more than one");
    }

    [Test]
    public void TheSameChannelTwiceAsksForNothingAndSaysWhy()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.Tune(Channel(tuner, "ZDF")), Is.True);
      Settle(tuner);

      Assert.That(tuner.Tune(Channel(tuner, "ZDF")), Is.False);
      Assert.That(player.Channels, Has.Count.EqualTo(1),
          "pressing the channel somebody is already watching must not stop and restart its picture");
      Assert.That(tuner.Status, Does.Contain("Already on"));
    }

    [Test]
    public void ZappingWalksTheBouquetAndWrapsAtBothEnds()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.Channels.Select(c => c.Name), Is.EqualTo(new[] { "Das Erste", "ZDF", "Sparten" }),
          "the zap list is the bouquet's own channels in the order the backend numbered them");

      tuner.Tune(Channel(tuner, "Sparten"));
      Settle(tuner);

      tuner.ZapNext();
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Das Erste"),
          "the channel up key at the end of the bouquet comes back round to the beginning, as the reference's "
          + "ZapNextChannel did (Models/SlimTvClientModel.cs:598-617)");

      tuner.ZapPrevious();
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Sparten"), "and the channel down key at the beginning wraps back");
    }

    [Test]
    public void GoingBackIsTheChannelBeforeTheOneOnTheScreen()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.ZapBack(), Is.False, "with nothing tuned there is nothing to go back to, and it says so");
      Assert.That(tuner.Status, Does.Contain("no channel before"));

      tuner.Tune(Channel(tuner, "Das Erste"));
      Settle(tuner);
      tuner.Tune(Channel(tuner, "Sparten"));
      Settle(tuner);

      Assert.That(tuner.ZapBack(), Is.True);
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Das Erste"),
          "the zero on a remote goes back to where the viewer was, which is what they press it for");
      Assert.That(player.Channels, Has.Count.EqualTo(3));
    }

    [Test]
    public void StoppingAsksForThePlayerAndForgetsTheChannel()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.Tune(Channel(tuner, "ZDF"));
      Settle(tuner);

      tuner.Stop();
      Assert.That(player.Stops, Is.EqualTo(1), "the tuner asked for the stop rather than doing it itself");
      Assert.That(tuner.IsTuned, Is.False);
      Assert.That(tuner.NowTitle, Does.Contain("No programme data"),
          "a stopped screen says nothing about a programme it is no longer showing");
    }

    [Test]
    public void ClosingTheScreenAsksForTheStopAndTakingThePictureAwayDoesNot()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel television = Opened(backend);
      TvTunerViewModel radio = Opened(backend, TvMediaType.Radio);
      AsksForPicture player = new AsksForPicture();
      player.Attach(television);
      player.Attach(radio);

      television.Tune(Channel(television, "ZDF"));
      Settle(television);

      // The reference kept two players and stopped one to start the other (TVHandler/SlimTvHandler.cs:213-221).
      // Here one player takes the new media and the old channel has simply gone away, so the other tuner must not
      // ask for a stop - that would fall on the picture that has just arrived.
      radio.Tune(Channel(radio, "Antenne"));
      Settle(radio);
      radio.TakenOver();

      Assert.That(television.IsTuned, Is.True, "the half that never stopped was not touched");
      Assert.That(radio.IsTuned, Is.False, "and the half that gave the picture up does not claim it");
      Assert.That(player.Stops, Is.EqualTo(0));

      television.Dispose();
      Assert.That(player.Stops, Is.EqualTo(1),
          "leaving a watching screen closes its channel; the reference let the sound carry on behind the rest of "
          + "the media centre, which is a habit for a box whose only job is being a television");
    }

    #endregion

    #region The digits

    [Test]
    public void TheDigitsAccumulateIntoOneChannelNumber()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.PressDigit(1);
      Assert.That(tuner.Digits, Is.EqualTo("1"));
      Assert.That(tuner.IsTypingANumber, Is.True);

      tuner.PressDigit(1);
      Assert.That(tuner.Digits, Is.EqualTo("11"),
          "a second digit is somebody typing eleven, not somebody who has chosen one channel and then another");

      // Eleven of three channels is nothing, and the reference's silence here (<c>ChannelZapModel.cs:141-181</c>)
      // is exactly the kind of quiet that makes a viewer press the remote again and again.
      Assert.That(tuner.CommitDigits(), Is.False);
      Assert.That(tuner.Digits, Is.Empty, "a finished number comes off the screen whether or not it matched");
      Assert.That(tuner.Status, Does.Contain("no channel number 11"));
      Assert.That(tuner.Status, Does.Contain("3"), "and it says how many there were to choose from");
      Assert.That(player.Channels, Is.Empty);
    }

    [Test]
    public void AFinishedNumberPicksTheChannelAtThatPlaceInTheBouquet()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.PressDigit(2);
      Assert.That(tuner.CommitDigits(), Is.True);
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("ZDF"),
          "the second channel, counted from one - the reference's ZapByChannelIndex was on by default, because "
          + "that is what the buttons on a remote mean to the person pressing them");

      tuner.PressDigit(3);
      Assert.That(tuner.CommitDigits(), Is.True);
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Sparten"));

      Assert.That(tuner.Digits, Is.Empty, "the number comes off the screen once it has been taken");
      Assert.That(player.Channels, Has.Count.EqualTo(2));
    }

    [Test]
    public void AZeroOnItsOwnGoesBackAndANoughtAfterDigitsIsANought()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.Tune(Channel(tuner, "Das Erste"));
      Settle(tuner);
      tuner.Tune(Channel(tuner, "ZDF"));
      Settle(tuner);

      tuner.PressDigit(0);
      Assert.That(tuner.Digits, Is.EqualTo("0"), "the zero is shown while it is typed, like every other digit");
      Assert.That(tuner.CommitDigits(), Is.True);
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Das Erste"),
          "a lone zero is the way back, which the reference made it for the same reason (<c>ChannelZapModel"
          + ".cs:167-170</c>)");

      // And a nought in the middle of a number is a nought. With three channels "10" cannot match, so the way to
      // see which reading was taken is the sentence that comes back.
      tuner.PressDigit(1);
      tuner.PressDigit(0);
      Assert.That(tuner.Digits, Is.EqualTo("10"));
      Assert.That(tuner.CommitDigits(), Is.False);
      Assert.That(tuner.Status, Does.Contain("no channel number 10"),
          "typed after other digits a zero is part of the number, which is how a hundred and one is spelled");
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Das Erste"), "and the channel on the screen did not move");
    }

    [Test]
    public void ClearingTheDigitsTunesNothing()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.PressDigit(7);
      tuner.ClearDigits();

      Assert.That(tuner.IsTypingANumber, Is.False);
      Assert.That(tuner.CommitDigits(), Is.False);
      Assert.That(player.Channels, Is.Empty, "Back on a remote takes the number off without choosing anything");
    }

    [Test]
    public void ANumberTypedWhileNothingIsOnTheScreenStillPicksAChannel()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.IsTuned, Is.False, "the screen opens on its bouquet and waits, as the reference's did");
      tuner.PressDigit(3);
      Assert.That(tuner.CommitDigits(), Is.True);
      Assert.That(tuner.Tuned!.Name, Is.EqualTo("Sparten"));
    }

    #endregion

    #region What the screen says about the evening

    [Test]
    public void TheListOverThePictureIsOneQuestionAboutTheBouquet()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);

      Assert.That(tuner.WatchRows.Select(r => r.Channel.Name),
          Is.EqualTo(new[] { "Das Erste", "ZDF", "Sparten" }),
          "the list over the picture is the zap list, because it is the same question about the same bouquet");

      Assert.That(backend.AskedPaths.Count(p => p.EndsWith("/api/epg/events", StringComparison.Ordinal)),
          Is.EqualTo(1),
          "one question about the bouquet, not one per channel: the reference's own miniguide was filled from "
          + "the guide data for exactly this reason");

      TvWatchRow first = tuner.WatchRows[0];
      Assert.That(first.NowTitle, Is.EqualTo("Tagesschau"), "what is running on it");
      Assert.That(first.NextTitle, Is.EqualTo("Tatort"), "and what comes after");
      Assert.That(first.HasProgramme, Is.True);
      Assert.That(first.Progress, Is.GreaterThan(0).And.LessThan(1));
      Assert.That(first.Number, Is.EqualTo("3"));

      TvWatchRow second = tuner.WatchRows[1];
      Assert.That(second.NowTitle, Is.EqualTo("No programme data."),
          "a channel the backend says nothing about gets the reference's own words rather than a blank cell");
      Assert.That(second.HasProgramme, Is.False);
      Assert.That(second.Progress, Is.EqualTo(0));
    }

    [Test]
    public void TheStripUnderThePictureFollowsTheRunningProgramme()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      tuner.Tune(Channel(tuner, "Das Erste"));
      Settle(tuner);

      Assert.That(tuner.ChannelSays, Is.EqualTo("3  Das Erste"));
      Assert.That(tuner.NowTitle, Is.EqualTo("Tagesschau"));
      Assert.That(tuner.NextTitle, Is.EqualTo("next:  Tatort"),
          "the one after it is led by the word that says which of the two it is; the reference had room for two "
          + "blocks and no labels, so the words carry the difference");
      Assert.That(tuner.HasProgramme, Is.True);
      Assert.That(tuner.Progress, Is.GreaterThan(0.1).And.LessThan(0.35),
          "ten minutes into an hour, which is what the bar stands at");

      // The reference put the end of the programme at the right end of its bar and commented the start out
      // (screens/FullScreenContentTv.xaml), because where a programme started is a fact nobody asks for while
      // watching one.
      DateTimeOffset stop = DateTimeOffset.FromUnixTimeSeconds(Anchor() + 3000);
      Assert.That(tuner.EndsAt, Is.EqualTo(stop.LocalDateTime.ToString("t", CultureInfo.CurrentCulture)));
    }

    [Test]
    public void ARefusedNowAndNextLeavesThePictureAloneAndSaysThereIsNoProgrammeData()
    {
      ScriptedTuner backend = new ScriptedTuner { RefusesOneChannelGuide = true };
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);

      Assert.That(tuner.Tune(Channel(tuner, "ZDF")), Is.True,
          "a channel is watchable whether or not its broadcaster has told anybody what is on it");
      Settle(tuner);

      Assert.That(player.Channels, Has.Count.EqualTo(1), "the picture was asked for all the same");
      Assert.That(tuner.HasProgramme, Is.False);
      Assert.That(tuner.NowTitle, Is.EqualTo("No programme data."));
      Assert.That(tuner.NextTitle, Is.Empty);
      Assert.That(tuner.Status, Does.Contain("no programme data"),
          "and the screen says which of the two it has, rather than showing an empty space and letting the "
          + "viewer wonder whether the remote is broken");
    }

    [Test]
    public void ABackendThatWillNotTalkAtAllLeavesAnEmptyBouquetAndItsOwnWords()
    {
      ScriptedTuner backend = new ScriptedTuner { RefuseEverything = HttpStatusCode.Unauthorized };
      TvTunerViewModel tuner = TunerWith(backend, TvMediaType.Television);

      tuner.ReloadAsync().GetAwaiter().GetResult();

      Assert.That(tuner.Channels, Is.Empty);
      Assert.That(tuner.WatchRows, Is.Empty);
      Assert.That(tuner.Status, Does.Contain("not accepted"),
          "the backend's own reason is what goes up on the screen, because it is the reason the settings screen "
          + "has to be opened for");
    }

    [Test]
    public void TheRadioHalfHasNoPictureAndOpensOnTheBouquetThatSaysRadio()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend, TvMediaType.Radio);

      Assert.That(tuner.HasPicture, Is.False,
          "the reference replaced the video surface with a still photograph for radio "
          + "(screens/FullScreenContentRadio.xaml); a black rectangle promising a picture that never comes is "
          + "worse than no rectangle");
      Assert.That(tuner.Groups.Select(g => g.Name), Is.EqualTo(new[] { "All headends", "Family", "Radio" }),
          "only the bouquets that name television are taken off a radio's list; deciding that a bouquet called "
          + "\"Family\" is television would take channels off somebody's radio");
      Assert.That(tuner.SelectedGroup!.Name, Is.EqualTo("Radio"),
          "and it opens on the one that says radio, because a radio that opens on a television bouquet opens on "
          + "channels nobody hears on the radio");
      Assert.That(tuner.Channels.Select(c => c.Name), Is.EqualTo(new[] { "Antenne" }));

      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);
      Assert.That(tuner.Tune(Channel(tuner, "Antenne")), Is.True,
          "a radio is played by the same player as a television, which is the only thing the two have in common "
          + "with the reference, whose two players were two objects");
      Assert.That(player.Only().EngineString, Is.EqualTo("http://tvbox:9981/stream/channelid/101"));
    }

    #endregion

    #region What the screen draws

    private static Window ShowOnScreen(TvTunerViewModel tuner)
    {
      Window window = new Window
      {
        Content = new TvWatchView(tuner),
        Width = 1280,
        Height = 720
      };
      window.Show();
      Pump();
      return window;
    }

    private static void Pump()
    {
      Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
      Dispatcher.UIThread.RunJobs();
    }

    private static List<Button> DrawnRows(Window window)
    {
      return window.GetVisualDescendants().OfType<Button>().Where(b => b.Tag is TvWatchRow).ToList();
    }

    [Test]
    [AvaloniaTest]
    public void TheTypedListOfDigitsStandsOnThePictureAndGoesAwayWhenItIsTaken()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      Window window = ShowOnScreen(tuner);

      Assert.That(WrittenOn(window), Has.None.EqualTo("7"),
          "nothing is typed, so the middle of the picture is the programme's");

      tuner.PressDigit(7);
      Pump();
      Assert.That(WrittenOn(window), Does.Contain("7"),
          "the digits are all the reference showed while zapping - a box with the number in it and no list of "
          + "channels (superlayers/ChannelZapOSD.xaml:11-13)");

      tuner.ClearDigits();
      Pump();
      Assert.That(WrittenOn(window), Has.None.EqualTo("7"));
    }

    [Test]
    [AvaloniaTest]
    public void TheListOverThePictureHasOneRowPerChannelAndWatchesTheOneItIsPressedOn()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);
      TvWatchView view = new TvWatchView(tuner);
      Window window = new Window { Content = view, Width = 1280, Height = 720 };
      window.Show();
      Pump();

      Assert.That(DrawnRows(window), Is.Empty, "the list stays away until somebody asks for it");

      view.ShowList(true);
      Pump();

      List<Button> rows = DrawnRows(window);
      Assert.That(rows, Has.Count.EqualTo(tuner.WatchRows.Count), "one row per channel of the bouquet");
      foreach (Button row in rows)
      {
        TvWatchRow model = (TvWatchRow)row.Tag;
        Assert.That(model.Channel.Name, Is.Not.Empty);
        string[] words = row.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
        Assert.That(words, Does.Contain(model.NowTitle), "the row says what is running on its channel");
        if (model.NextTitle.Length > 0)
        {
          Assert.That(words, Does.Contain(model.NextTitle), "and what comes after, when the backend said");
        }
      }

      rows[1].RaiseEvent(new RoutedEventArgs(Button.ClickEvent, rows[1]));
      Pump();

      Assert.That(tuner.Tuned!.Name, Is.EqualTo("ZDF"),
          "a press on a row watches that channel; the reference's miniguide was a way of choosing what to watch, "
          + "and recording was a different key altogether");
      Assert.That(view.IsListShowing, Is.False, "and the list goes out of the way to show the picture it chose");
      Assert.That(player.Channels, Has.Count.EqualTo(1));
    }

    [Test]
    [AvaloniaTest]
    public void TheRadioScreenDrawsNoPictureAndSaysItsChannelWhereThePictureWouldBe()
    {
      ScriptedTuner backend = new ScriptedTuner();
      TvTunerViewModel tuner = Opened(backend, TvMediaType.Radio);
      AsksForPicture player = new AsksForPicture();
      player.Attach(tuner);
      Window window = ShowOnScreen(tuner);

      Assert.That(window.GetVisualDescendants().OfType<VideoSurface>().Any(s => s.IsVisible), Is.False,
          "there is no picture on the radio, and no rectangle pretending there is one");

      tuner.Tune(Channel(tuner, "Antenne"));
      Settle(tuner);
      Pump();

      Assert.That(WrittenOn(window), Does.Contain("101  Antenne"),
          "the channel is said big where a television would show its picture, which is as close as this gets to "
          + "the reference's photograph and its big station name");
    }

    /// <summary>
    /// The words a person looking at the window would read. A control inside a box that has been hidden still
    /// calls itself visible in Avalonia, so what counts is the whole chain of parents above it - otherwise a test
    /// about the digits would find them whether or not anybody had typed any.
    /// </summary>
    private static List<string> WrittenOn(Window window)
    {
      return window.GetVisualDescendants().OfType<TextBlock>()
          .Where(t => t.GetVisualAncestors().All(a => a.IsVisible))
          .Select(t => t.Text ?? "")
          .ToList();
    }

    #endregion
  }
}
