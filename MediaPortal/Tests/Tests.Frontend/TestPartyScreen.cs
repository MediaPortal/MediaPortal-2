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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The party screen: the shuffle it builds from the server's music shelf, the songs it asks the application
  /// to play, and what it does when songs end, are skipped, or refuse to open.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The party is the one screen in this front end that is only half in charge: it chooses, the application
  /// plays, and the application reports back. These tests stand in for the application - they take the
  /// <see cref="PartyViewModel.PlayRequested"/> events and they press the two ears,
  /// <see cref="PartyViewModel.TrackFinished"/> and <see cref="PartyViewModel.TrackRefused"/> - because
  /// "the evening does not stop at song six" and "one broken file is not a silent screen" cannot be proven
  /// by drawing anything.
  /// </para>
  /// <para>
  /// The shuffle is random in the application and seeded in the tests: the seed is a constructor argument
  /// exactly so that a test can watch a queue run out and refill without praying that the random number
  /// generator repeats itself.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestPartyScreen
  {
    private static readonly Uri SERVER_URL = new Uri("http://127.0.0.1:9/MediaPortal/Server/");

    /// <summary>The provider id a local share's paths carry; the file rules do not read it.</summary>
    private static readonly Guid LOCAL_PROVIDER = new Guid("44444444-4444-4444-4444-444444444444");

    private static string SongPath(string name)
    {
      return LOCAL_PROVIDER.ToString("B") + "://"
          + Path.Combine(Path.GetTempPath(), "mp2-party-tests", name + ".mp3");
    }

    private sealed record Song(Guid Id, string Name, string? Path);

    private static Song SongAt(int index)
    {
      return new Song(new Guid(string.Format("00000001-0002-0003-0004-{0:D12}", index + 1)),
          "Song " + (index + 1).ToString(CultureInfo.InvariantCulture), SongPath("song" + index));
    }

    private static void Pump()
    {
      Avalonia.Threading.Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Pump();
        if (condition())
          return;
      }

      Assert.Fail("Timed out after 15 seconds waiting for " + because);
    }

    /// <summary>
    /// A party screen over a scripted music shelf, with the test standing in for the application: every song
    /// the screen asks for is collected in <see cref="AskedFor"/>.
    /// </summary>
    private sealed class PartyOn : IDisposable
    {
      public PartyOn(ShelfServer server, int? seed)
      {
        Client = new ServerApiClient(SERVER_URL, new HttpClient(server));
        Screen = new PartyViewModel(Client, seed);
        Screen.PlayRequested += delegate (object? sender, TitleViewModel song)
        {
          AskedFor.Add(song);
        };
      }

      public readonly PartyViewModel Screen;
      public readonly ServerApiClient Client;
      public readonly List<TitleViewModel> AskedFor = new List<TitleViewModel>();

      public void Dispose()
      {
        Screen.Dispose();
        Client.Dispose();
      }
    }

    /// <summary>
    /// A server with a music shelf: <c>GET api/media/titles?aspect=AudioItem</c> answered in the server's own
    /// paging shape, plus the two ways a shelf can fail to be one - it is not there, or it is empty.
    /// </summary>
    private sealed class ShelfServer : HttpMessageHandler
    {
      internal readonly List<Song> Songs = new List<Song>();

      /// <summary>Answer 404 like a server that has no aspect called AudioItem at all.</summary>
      internal bool NoShelf;

      internal int Reads;

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string path = request.RequestUri!.AbsolutePath;
        if (!path.Contains("api/media/titles", StringComparison.Ordinal))
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
        }

        Reads++;
        if (NoShelf)
        {
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
          {
            RequestMessage = request,
            Content = new StringContent("this server has no title aspect named 'AudioItem'", Encoding.UTF8,
                "text/plain")
          });
        }

        int offset = ParameterIn(request.RequestUri.Query, "offset") ?? 0;
        int limit = ParameterIn(request.RequestUri.Query, "limit") ?? Songs.Count;

        StringBuilder body = new StringBuilder("{\"aspect\":\"AudioItem\",\"total\":"
            + Songs.Count.ToString(CultureInfo.InvariantCulture) + ",\"offset\":"
            + offset.ToString(CultureInfo.InvariantCulture) + ",\"limit\":"
            + limit.ToString(CultureInfo.InvariantCulture) + ",\"returned\":"
            + Math.Min(limit, Songs.Count - offset).ToString(CultureInfo.InvariantCulture)
            + ",\"sort\":\"title\",\"titles\":[");
        int returned = 0;
        for (int at = offset; at < Songs.Count && returned < limit; at++, returned++)
        {
          Song song = Songs[at];
          if (returned > 0)
            body.Append(',');
          body.Append("{\"id\":\"").Append(song.Id.ToString("D")).Append("\",\"title\":\"").Append(song.Name)
              .Append('"');
          if (song.Path != null)
            body.Append(",\"path\":\"").Append(song.Path.Replace('\\', '/')).Append('"');
          body.Append('}');
        }

        body.Append("]}");
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
          RequestMessage = request,
          Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
        });
      }

      private static int? ParameterIn(string query, string name)
      {
        foreach (string part in query.TrimStart('?').Split('&'))
        {
          int at = part.IndexOf('=');
          if (at > 0 && part.Substring(0, at) == name
              && int.TryParse(part.Substring(at + 1), NumberStyles.Integer, CultureInfo.InvariantCulture,
                  out int value))
            return value;
        }

        return null;
      }
    }

    private static PartyOn ScreenWith(int songs, int? seed = null)
    {
      ShelfServer server = new ShelfServer();
      for (int at = 0; at < songs; at++)
        server.Songs.Add(SongAt(at));
      return new PartyOn(server, seed);
    }

    [AvaloniaTest]
    public void PressingStartReadsTheShelfAndAsksForTheFirstSongOfTheShuffle()
    {
      using PartyOn party = ScreenWith(6, 1);

      party.Screen.StartCommand.Execute(null);
      WaitUntil("six songs to be queued and the first to be asked for",
          delegate { return party.Screen.Upcoming.Count == 6 && party.AskedFor.Count == 1; });

      Assert.That(party.AskedFor[0], Is.SameAs(party.Screen.Upcoming[0]),
          "the song the screen says it asked for and the head of the queue are one song, or the list on the "
          + "screen is scenery");
      Assert.That(party.Screen.Upcoming.Select(row => row.Name),
          Is.EquivalentTo(Enumerable.Range(0, 6).Select(at => "Song " + (at + 1))),
          "everything the shelf answered is queued - the shuffle reorders the shelf, it does not edit it");
      Assert.That(party.Screen.Status, Does.Contain("The party is on").And.Contain("5 more to go"),
          "the screen says what it is doing: playing this one, and knowing how many more it has");
    }

    [AvaloniaTest]
    public void ASeededShuffleIsTheSamePartyTwice()
    {
      using PartyOn first = ScreenWith(6, 7);
      using PartyOn second = ScreenWith(6, 7);

      first.Screen.StartCommand.Execute(null);
      WaitUntil("the first party to start", delegate { return first.AskedFor.Count == 1; });
      second.Screen.StartCommand.Execute(null);
      WaitUntil("the second party to start", delegate { return second.AskedFor.Count == 1; });

      Assert.That(second.Screen.Upcoming.Select(row => row.Name),
          Is.EqualTo(first.Screen.Upcoming.Select(row => row.Name)),
          "the same seed over the same shelf must produce the same queue, or the tests are guessing");
    }

    [AvaloniaTest]
    public void FinishingASongPutsTheNextOneOnAndNamesThemBoth()
    {
      using PartyOn party = ScreenWith(6, 2);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      Guid first = party.AskedFor[0].Title.Id;
      party.Screen.TrackFinished(first);
      WaitUntil("the next song to be asked for", delegate { return party.AskedFor.Count == 2; });

      Assert.That(party.AskedFor[1], Is.SameAs(party.Screen.Upcoming[0]));
      Assert.That(party.AskedFor[1].Title.Id, Is.Not.EqualTo(first), "the party plays the next song, not this one again");
      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(5));
      Assert.That(party.Screen.Status, Does.Contain("played - next up"));

      party.Screen.TrackFinished(first);
      Pump();
      Assert.That(party.AskedFor.Count, Is.EqualTo(2),
          "a second report about a song the queue has let go is history, not a second ending - advancing on it "
          + "would skip a song for an ending that already happened");
    }

    [AvaloniaTest]
    public void TwoRoundsInARowNeverOpenOnTheSameSong()
    {
      using PartyOn party = ScreenWith(3, 3);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      for (int finished = 0; finished < 3; finished++)
      {
        Guid current = party.Screen.Upcoming[0].Title.Id;
        party.Screen.TrackFinished(current);
      }

      WaitUntil("the refill to ask for the next song", delegate { return party.AskedFor.Count == 4; });
      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(3),
          "a party that ran out of songs is a party that misread its job - the shelf starts again");
      Assert.That(party.AskedFor[3].Name, Is.Not.EqualTo(party.AskedFor[2].Name),
          "the song that just ended moves to the back of the new round rather than opening it again");
      Assert.That(party.Screen.Status, Does.Contain("starting again, shuffled"));
    }

    [AvaloniaTest]
    public void SkippingASongSaysWhichOneItWasAndPutsTheNextOneOn()
    {
      using PartyOn party = ScreenWith(6, 4);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      string first = party.AskedFor[0].Name;
      party.Screen.SkipCommand.Execute(null);
      WaitUntil("the skip to ask for the next song", delegate { return party.AskedFor.Count == 2; });

      Assert.That(party.AskedFor[1].Name, Is.Not.EqualTo(first));
      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(5), "the skipped song is done with, not waiting at the back");
      Assert.That(party.Screen.Status, Does.Contain(first).And.Contain("skipped"));
    }

    [AvaloniaTest]
    public void ARefusedSongLeavesTheQueueAndThePartyWalksOn()
    {
      using PartyOn party = ScreenWith(6, 5);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      Guid first = party.AskedFor[0].Title.Id;
      party.Screen.TrackRefused(first, "the file was gone");
      WaitUntil("the next song to be asked for", delegate { return party.AskedFor.Count == 2; });

      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(5), "a song that would not open is not waiting in the queue");
      Assert.That(party.Screen.Status, Does.Contain("the file was gone").And.Contain("the party walks on"),
          "the refusal is said with the player's own words rather than swallowed, and the evening continues");
    }

    [AvaloniaTest]
    public void ThreeRefusalsInARowStopThePartyAndSaySo()
    {
      using PartyOn party = ScreenWith(6, 6);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      for (int refused = 0; refused < 3; refused++)
      {
        Guid current = party.Screen.Upcoming[0].Title.Id;
        party.Screen.TrackRefused(current, "the share went away");
      }

      Pump();
      Assert.That(party.Screen.IsOn, Is.False,
          "one broken file is an evening saved by walking on; three in a row is a fact about the shelf, and a "
          + "machine that walks on forever is failing in a shuffle");
      Assert.That(party.Screen.Status, Does.Contain("would not open, so the party stops here"));
    }

    [AvaloniaTest]
    public void AServerWithoutTheMusicShelfIsAnsweredWithItsOwnWords()
    {
      ShelfServer server = new ShelfServer { NoShelf = true };
      using PartyOn party = new PartyOn(server, 1);

      party.Screen.StartCommand.Execute(null);
      WaitUntil("the refusal to come back", delegate { return party.Screen.Status.Contains("refused"); });

      Assert.That(party.Screen.Status, Does.Contain("the music shelf refused").And.Contain("AudioItem"));
      Assert.That(party.Screen.Upcoming, Is.Empty);
      Assert.That(party.AskedFor, Is.Empty);
      Assert.That(party.Screen.IsOn, Is.False,
          "a party that has nothing to play is not on, and its start button stays truthful");
    }

    [AvaloniaTest]
    public void TitlesThatCannotBePlayedFromHereStayOutOfTheQueue()
    {
      ShelfServer server = new ShelfServer();
      for (int at = 0; at < 4; at++)
        server.Songs.Add(SongAt(at));
      server.Songs[2] = server.Songs[2] with { Path = null };
      using PartyOn party = new PartyOn(server, 8);

      party.Screen.StartCommand.Execute(null);
      WaitUntil("the start line to be printed", delegate { return party.AskedFor.Count == 1; });

      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(3),
          "a title with no file is not a song waiting to happen, and queueing it would be queueing a refusal");
      Assert.That(party.Screen.Upcoming.Any(row => row.Title.Title == "Song 3"), Is.False);
      Assert.That(party.Screen.Status, Does.Contain("1 title the shelf named is not playable from here"),
          "the shelf really did hold four things, and the line says which one this machine cannot play");
    }

    [AvaloniaTest]
    public void AStoppedPartyLetsTheCurrentSongFinishAndAsksForNothingAfterIt()
    {
      using PartyOn party = ScreenWith(6, 9);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      party.Screen.StopCommand.Execute(null);
      Guid current = party.AskedFor[0].Title.Id;
      party.Screen.TrackFinished(current);
      Pump();

      Assert.That(party.AskedFor.Count, Is.EqualTo(1),
          "stop said no more songs after this one, and a screen that queues the next song anyway did not listen");
      Assert.That(party.Screen.Upcoming.Count, Is.EqualTo(5),
          "the song that finished still leaves the queue - the list shows what is coming, not what happened");
      Assert.That(party.Screen.Status, Does.Contain("the party is stopped"));
    }

    [AvaloniaTest]
    public void PressingOnARowPlaysThatSongNowAndKeepsTheRestInOrder()
    {
      using PartyOn party = ScreenWith(6, 10);
      party.Screen.StartCommand.Execute(null);
      WaitUntil("the first song to be asked for", delegate { return party.AskedFor.Count == 1; });

      List<string> before = party.Screen.Upcoming.Select(row => row.Name).ToList();
      TitleViewModel picked = party.Screen.Upcoming[3];
      party.Screen.Selected = picked;
      party.Screen.PlaySelectedCommand.Execute(null);
      WaitUntil("the picked song to be asked for", delegate { return party.AskedFor.Count == 2; });

      Assert.That(party.AskedFor[1], Is.SameAs(picked));
      Assert.That(party.Screen.Upcoming[0], Is.SameAs(picked),
          "the queue on the screen leads with the song that is playing, or it is describing last minute");
      Assert.That(party.Screen.Upcoming.Select(row => row.Name),
          Is.EqualTo(new[] { before[3], before[1], before[2], before[4], before[5] }),
          "the songs behind the one that was jumped to keep their order, and the song that was playing leaves "
          + "the queue like the skip it is");
      Assert.That(party.Screen.Status, Does.Contain("on your ask"));
    }

    [AvaloniaTest]
    public void ThePartyTileIsOpenableWithoutAServerAndSaysSoOnItsOwnScreen()
    {
      PartyViewModel withoutServer = new PartyViewModel();

      Assert.That(withoutServer.Status, Is.EqualTo("No server to ask for music"));
      Assert.That(withoutServer.StartCommand.CanExecute(null), Is.False,
          "a start button that can only fail is not offered - the tile opens, the screen says why it cannot dance");
    }
  }
}
