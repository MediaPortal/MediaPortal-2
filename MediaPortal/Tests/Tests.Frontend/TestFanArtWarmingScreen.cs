#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
    http://www.team-mediaportal.com

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

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// What the front end does with the server's memory of where its pictures lie: the row on the media answers
  /// screen that says how far the server got and starts a pass when it is pressed, and the folders screen telling
  /// the server to learn once an import it watched is through.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The server behind the screen tests is the real <see cref="MediaPortal.Server.Api.MediaController"/> hosted in
  /// this process, because the thing worth testing here is the sentence the row shows: it is made out of what the
  /// server's answer means, and a fake that answered "yes" would leave the wording - which is the entire feature -
  /// untested. Only the library is faked, with titles that have no files of their own, which the server answers
  /// with "nothing of that kind" and is exactly the answer a walk over a library whose pictures are not filed yet
  /// produces.
  /// </para>
  /// <para>
  /// The folders screen is tested against a recording transport rather than the hosted server, for the opposite
  /// reason: what is under test is that this screen <em>asked</em>, and on a hosted server the server starts the
  /// same pass by itself when an import is queued, so its state cannot tell the two callers apart.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFanArtWarmingScreen
  {
    private string _settingsFile;

    [SetUp]
    public void SetUp()
    {
      _settingsFile = Path.Combine(Path.GetTempPath(),
          "mp2-warming-" + Guid.NewGuid().ToString("N") + ".json");
    }

    [TearDown]
    public void TearDown()
    {
      TestWarmingServer.Clear();
      if (File.Exists(_settingsFile))
        File.Delete(_settingsFile);
    }

    #region The row on the media answers screen

    [Test]
    public void TheRowSaysWhatTheServerHasLearnedWithoutAnyonePressing()
    {
      using (TestWarmingServer server = TestWarmingServer.Start(2))
      {
        // A pass that ran on its own - after an import, which the server does by itself - has to be visible on the
        // screen without the viewer having to start a second one to find out about it.
        server.WarmAsync().GetAwaiter().GetResult();
        server.WaitUntilQuietAsync().GetAwaiter().GetResult();

        MediaSettingsViewModel screen = OpenScreen(server);

        Assert.That(server.ReadAsync().GetAwaiter().GetResult().Answers, Is.EqualTo(14),
            "the pass answered all seven kinds of both titles, found or not");
        AssertThat(LastRow(screen).ValueText, Does.Contain("went over 2 title(s)")
            .And.Contain("14 picture question(s)"));
      }
    }

    [Test]
    public void PressingTheRowSendsTheServerOverItsLibrary()
    {
      using (TestWarmingServer server = TestWarmingServer.Start(2))
      {
        MediaSettingsViewModel screen = OpenScreen(server);
        AssertThat(LastRow(screen).ValueText, Does.Contain("nothing learned yet"),
            "a server that never walked its library is said so, rather than the row going blank");

        LastRow(screen).Press();
        screen.WarmingRead.GetAwaiter().GetResult();

        FanArtWarming said = server.ReadAsync().GetAwaiter().GetResult();
        Assert.That(said.Stopped, Is.Null, "nothing interrupted the pass: " + said.Stopped);
        Assert.That(said.Titles, Is.EqualTo(2), "pressing the row really sent the server over its titles");
        AssertThat(LastRow(screen).ValueText, Does.Contain("went over 2 title(s)"),
            "and the row is left showing what the server ended up saying");
      }
    }

    [Test]
    public void AServerThatWillNotSayLeavesTheRowWithoutAnAnswer()
    {
      // Port 1 answers nothing. The other four rows on this screen are the viewer's own and stay usable, because a
      // server that will not talk about its memory still hands out its pictures.
      using (var client = new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"), null,
          TimeSpan.FromSeconds(2)))
      {
        MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _settingsFile, client);
        screen.WarmingRead.GetAwaiter().GetResult();

        AssertThat(LastRow(screen).ValueText, Is.EqualTo("the server would not say"));
        Assert.That(screen.Options, Has.Count.EqualTo(6),
            "the row is still there, and the screen is still the five answers of its own plus it");
      }
    }

    [Test]
    public void ARowOnAMachineWithNoServerSaysThatRatherThanGuessing()
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _settingsFile, null);
      screen.WarmingRead.GetAwaiter().GetResult();

      AssertThat(LastRow(screen).ValueText, Is.EqualTo("no server to ask"));
    }

    [Test]
    public void TheWordsAboutTheServersMemorySayWhatTheNumberMeans()
    {
      // An answer stands for a picture the server can name without listing a folder, whether that answer is a file
      // or "this title has none of that kind", and the row has to say it that way round.
      string learned = new FanArtWarming(false, 120, 0, 40, 840, null, null, null, null).Describe();
      AssertThat(learned, Does.Contain("went over 120 title(s)").And.Contain("840 picture question(s)"));

      string waiting = new FanArtWarming(true, 0, 0, 0, 0, "the importer still has 1 job", null, null, null).Describe();
      AssertThat(waiting, Does.Contain("waiting").And.Contain("1 job"));

      string stopped = new FanArtWarming(false, 0, 0, 0, 0, null, "gave up waiting for the importer",
          null, null).Describe();
      AssertThat(stopped, Does.Contain("stopped").And.Contain("gave up"));

      AssertThat(new FanArtWarming(false, 0, 0, 0, 0, null, null, null, null).Describe(),
          Does.Contain("nothing learned yet"));
    }

    #endregion

    #region The folders screen telling the server

    [Test]
    public async Task AFolderThatFinishedImportingIsFollowedByTellingTheServer()
    {
      RecordingTransport transport = new RecordingTransport();
      using (var client = new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
          new HttpClient(transport)))
      {
        FoldersViewModel folders = new FoldersViewModel(client) { PollDelayMilliseconds = 1 };
        await folders.RefreshAsync();
        folders.Selected = folders.Shares.Single();

        await folders.ReimportAsync();
        await folders.QueueWatch;

        Assert.That(folders.Status, Is.EqualTo("Import finished."));
        Assert.That(transport.AskedFor("api/media/fanart/warming", HttpMethod.Post), Is.True,
            "a folder that was just imported brought pictures with it, and the server is told to learn where "
            + "they lie instead of finding out one wall at a time");
      }
    }

    [Test]
    public async Task ATellTheServerRefusedLeavesTheFoldersScreenSayingWhatItSaid()
    {
      RecordingTransport transport = new RecordingTransport { RefusesWarming = true };
      using (var client = new ServerApiClient(new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
          new HttpClient(transport)))
      {
        FoldersViewModel folders = new FoldersViewModel(client) { PollDelayMilliseconds = 1 };
        await folders.RefreshAsync();
        folders.Selected = folders.Shares.Single();

        await folders.ReimportAsync();
        await folders.QueueWatch;

        Assert.That(folders.Status, Is.EqualTo("Import finished."),
            "a server that will not talk about its memory does not take the sentence about the folder away");
        Assert.That(folders.Queue, Is.EqualTo("nothing being imported"));
      }
    }

    #endregion

    #region Helpers

    private MediaSettingsViewModel OpenScreen(TestWarmingServer server)
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _settingsFile, server.Client);
      screen.WarmingRead.GetAwaiter().GetResult();
      return screen;
    }

    private static SkinSettingOption LastRow(MediaSettingsViewModel screen)
    {
      return screen.Options[screen.Options.Count - 1];
    }

    /// <summary>
    /// Asserting on a sentence the screen shows. Spelling the assertion out here keeps the message that names the
    /// row and what it should have said together, which is what a failing row needs.
    /// </summary>
    private static void AssertThat(string shown, Constraint wanted, string because = null)
    {
      Assert.That(shown, wanted, "{0} - the row says: {1}", because ?? "the row says the wrong thing", shown);
    }

    #endregion
  }
}
