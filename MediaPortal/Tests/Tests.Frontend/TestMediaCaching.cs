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
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using MediaPortal.Frontend.Caching;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;

namespace Tests.Frontend
{
  /// <summary>
  /// What this machine keeps of the library's pictures. A backdrop is the picture lying next to the media -
  /// around 650 KB on an ordinary library and up to two megabytes on a big one - so a wall of tiles is tens of
  /// megabytes read over the network, and a machine that forgets at the end of the run pays for all of it again
  /// at the next start. These tests hold the shelf to what the media settings screen promises: that a picture
  /// once fetched is not asked for twice, that "this title has none" is worth keeping too, that two servers and
  /// two kinds of picture never answer for each other, that the room the viewer named is the room the shelf
  /// stays inside, and that none of it - not a full disk, not a folder that went away - is ever the reason a
  /// picture fails to arrive.
  /// </summary>
  [TestFixture]
  public class TestMediaCaching
  {
    private static readonly Uri FIRST_SERVER = new Uri("http://192.168.178.31:55555/MediaPortal/Server");
    private static readonly Uri OTHER_SERVER = new Uri("http://10.0.0.9:55555/MediaPortal/Server");

    private string _folder = "";

    /// <summary>The folder the answers name, which is the only one the shelf may ever write into.</summary>
    private string _shelf = "";
    private string _file = "";

    [SetUp]
    public void AFolderOfItsOwn()
    {
      _folder = Path.Combine(Path.GetTempPath(), "mp2-pictures-" + Guid.NewGuid().ToString("N"));
      _file = Path.Combine(_folder, "media.json");
      _shelf = Path.Combine(_folder, "the shelf");
      Directory.CreateDirectory(_folder);
    }

    [TearDown]
    public void TakeTheFolderAway()
    {
      try
      {
        if (Directory.Exists(_folder))
          Directory.Delete(_folder, true);
      }
      catch (IOException)
      {
      }
      catch (UnauthorizedAccessException)
      {
      }
    }

    // ---------------------------------------------------------------- the answers

    [Test]
    public void TheShippedAnswersKeepPicturesAndSpendAQuarterOfAGigabyte()
    {
      MediaSettings settings = MediaSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Null, "a file that was never written is not a problem");
      Assert.That(settings.CachePictures, Is.True,
          "keeping what was fetched once helps every machine, so it is the answer that ships");
      Assert.That(settings.CacheSizeMb, Is.EqualTo(256));
      Assert.That(settings.RememberedPictures, Is.EqualTo(Artwork.MAX_REMEMBERED),
          "the memory answer ships at the figure the screens were built with");
      Assert.That(settings.PicturesAtOnce, Is.EqualTo(Artwork.MAX_AT_ONCE));
      Assert.That(settings.CacheFolder, Is.Empty, "empty means this machine's own folder");
      Assert.That(settings.CacheFolderPath, Is.Not.Empty);
    }

    [Test]
    public void AHandWrittenNumberMeansTheNearestAnswerThatDoesNotGoOverIt()
    {
      // A viewer who types 300 into the file said how much they are willing to spend, not how little.
      Write("{\"cacheSizeMb\": 300, \"rememberedPictures\": 300, \"picturesAtOnce\": 9}");

      MediaSettings settings = MediaSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Null);
      Assert.That(settings.CacheSizeMb, Is.EqualTo(256), "300 MB of promise must not become 512 MB of disk");
      Assert.That(settings.CacheSizeBytes, Is.EqualTo(256L * 1024L * 1024L));
      Assert.That(settings.RememberedPictures, Is.EqualTo(200));
      Assert.That(settings.PicturesAtOnce, Is.EqualTo(8));
    }

    [Test]
    public void ANumberBelowEveryAnswerMeansTheSmallestOne()
    {
      Write("{\"cacheSizeMb\": 3, \"rememberedPictures\": 7, \"picturesAtOnce\": 0}");

      MediaSettings settings = MediaSettings.Load(_file, out _);

      Assert.That(settings.CacheSizeMb, Is.EqualTo(64));
      Assert.That(settings.RememberedPictures, Is.EqualTo(50));
      Assert.That(settings.PicturesAtOnce, Is.EqualTo(1));
    }

    [Test]
    public void AFileThatIsNotThereForTheReaderIsSaidAndDoesNotStopTheScreen()
    {
      File.WriteAllText(_file, "not json at all {");

      MediaSettings settings = MediaSettings.Load(_file, out string? problem);

      Assert.That(problem, Is.Not.Null.And.Contains(_file));
      Assert.That(settings.CachePictures, Is.True, "an unreadable file leaves the shipped answers, not none");
      Assert.That(settings.CacheSizeMb, Is.EqualTo(256));
    }

    [Test]
    public void AnAnswerNamedByTheViewerIsTheFolderTheShelfLivesIn()
    {
      string named = Path.Combine(_folder, "pictures on the big disk");
      Write("{\"cacheFolder\": \"" + Quote(named) + "\"}");

      MediaSettings settings = MediaSettings.Load(_file, out _);

      Assert.That(settings.CacheFolderPath, Is.EqualTo(named));
    }

    // ---------------------------------------------------------------- the shelf

    [Test]
    public async Task APictureOnceFetchedIsNotAskedForAgain()
    {
      PictureCache shelf = Shelf();
      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);
      Guid title = Guid.NewGuid();

      int asked = 0;
      byte[]? first = await posters.LoadAsync(title, delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 1, 2, 3, 4 });
      });
      await shelf.FlushAsync();

      byte[]? second = await posters.LoadAsync(title, delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 9 });
      });

      Assert.That(first, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
      Assert.That(asked, Is.EqualTo(1), "the second question is answered from the shelf, not from the server");
      Assert.That(second, Is.EqualTo(first), "and it is answered with the same bytes, unchanged");
    }

    [Test]
    public async Task ATitleTheServerHasNoPictureForIsKeptAsHavingNone()
    {
      PictureCache shelf = Shelf();
      IPictureStore banners = shelf.Store("Banner", FIRST_SERVER);
      Guid title = Guid.NewGuid();

      int asked = 0;
      Func<Task<byte[]?>> none = delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(null);
      };

      Assert.That(await banners.LoadAsync(title, none), Is.Null);
      await shelf.FlushAsync();
      Assert.That(await banners.LoadAsync(title, none), Is.Null);

      // Listing the folder of a title is as expensive as reading the picture out of it, so the answer that a
      // title has no banner is worth the file it costs. Without it a library where half the titles have none
      // spends its bandwidth proving that over and over.
      Assert.That(asked, Is.EqualTo(1));
    }

    [Test]
    public async Task AKeptAbsenceAsksAgainOnceItHasAged()
    {
      PictureCache shelf = Shelf();
      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);
      Guid title = Guid.NewGuid();

      Func<Task<byte[]?>> none = delegate { return Task.FromResult<byte[]?>(null); };
      await posters.LoadAsync(title, none);
      await shelf.FlushAsync();

      // Laying the absence back past its age, the way a day of standing unused does it.
      foreach (string kept in Directory.GetFiles(_shelf, "*.none", SearchOption.AllDirectories))
        File.SetLastWriteTimeUtc(kept, DateTime.UtcNow.AddDays(-2));

      // The server has gained the picture since the absence was proven - a tag extractor ran, a fanart was
      // filled in by hand - and a shelf that never asked again would hide it for the life of the disk.
      int asked = 0;
      byte[]? nowThere = await posters.LoadAsync(title, delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 5, 6 });
      });

      Assert.That(asked, Is.EqualTo(1), "an absence past its age is a question again, not an answer");
      Assert.That(nowThere, Is.EqualTo(new byte[] { 5, 6 }));

      // And a second aged absence still answers for its day: the renewed stamp keeps the question from being
      // asked on every tile again.
      PictureCache again = Shelf();
      IPictureStore banners = again.Store("Banner", FIRST_SERVER);
      Guid other = Guid.NewGuid();
      int askedTwice = 0;
      Func<Task<byte[]?>> askedOnce = delegate
      {
        askedTwice++;
        return Task.FromResult<byte[]?>(null);
      };

      await banners.LoadAsync(other, askedOnce);
      await again.FlushAsync();
      foreach (string kept in Directory.GetFiles(_shelf, "*.none", SearchOption.AllDirectories))
        File.SetLastWriteTimeUtc(kept, DateTime.UtcNow.AddDays(-2));
      Assert.That(await banners.LoadAsync(other, askedOnce), Is.Null);
      await again.FlushAsync();
      Assert.That(askedTwice, Is.EqualTo(2), "the stale answer was answered, so its stamp is renewed");

      PictureCache third = Shelf();
      Assert.That(await third.Store("Banner", FIRST_SERVER).LoadAsync(other, askedOnce), Is.Null);
      Assert.That(askedTwice, Is.EqualTo(2), "and the fresh answer is kept like the old one was");
    }

    [Test]
    public async Task AKnownAnswerOfOneServerIsNotHandedOutForAnother()
    {
      PictureCache shelf = Shelf();
      Guid title = Guid.NewGuid();

      byte[]? fromFirst = await shelf.Store("Poster", FIRST_SERVER).LoadAsync(title,
          delegate { return Task.FromResult<byte[]?>(new byte[] { 7 }); });
      await shelf.FlushAsync();

      int askedToOther = 0;
      byte[]? fromOther = await shelf.Store("Poster", OTHER_SERVER).LoadAsync(title, delegate
      {
        askedToOther++;
        return Task.FromResult<byte[]?>(new byte[] { 8 });
      });

      Assert.That(fromFirst, Is.EqualTo(new byte[] { 7 }));
      Assert.That(askedToOther, Is.EqualTo(1), "the same id on two servers is not the same poster");
      Assert.That(fromOther, Is.EqualTo(new byte[] { 8 }));
    }

    [Test]
    public async Task APosterIsNeverHandedOutAsABackdrop()
    {
      PictureCache shelf = Shelf();
      Guid title = Guid.NewGuid();

      await shelf.Store("Poster", FIRST_SERVER).LoadAsync(title,
          delegate { return Task.FromResult<byte[]?>(new byte[] { 1 }); });
      await shelf.FlushAsync();

      int asked = 0;
      byte[]? backdrop = await shelf.Store("FanArt", FIRST_SERVER).LoadAsync(title, delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 2 });
      });

      Assert.That(asked, Is.EqualTo(1), "a kind the shelf has never seen is asked for, not borrowed");
      Assert.That(backdrop, Is.EqualTo(new byte[] { 2 }));
    }

    [Test]
    public async Task ASwitchedOffShelfNeitherReadsNorWrites()
    {
      Write("{\"cachePictures\": false, \"cacheFolder\": \"" + Quote(_shelf) + "\"}");
      PictureCache shelf = new PictureCache(_file);
      Assert.That(shelf.Enabled, Is.False);

      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);
      int asked = 0;
      Func<Task<byte[]?>> ask = delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 1 });
      };

      await posters.LoadAsync(Guid.NewGuid(), ask);
      await posters.LoadAsync(Guid.NewGuid(), ask);
      await shelf.FlushAsync();

      Assert.That(asked, Is.EqualTo(2), "off means the server is asked every time");
      Assert.That(PicturesOn(_shelf), Is.EqualTo(0), "and nothing is left behind on the disk");
    }

    [Test]
    public async Task TheRoomTheViewerNamedIsTheRoomTheShelfStaysInside()
    {
      PictureCache shelf = Shelf();
      shelf.RoomBytes = 300;
      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);

      // Three pictures of 120 bytes with room for two. The writes are a heartbeat apart because the shelf throws
      // out what it has not handed out for the longest, and a disk that cannot tell two writes apart cannot
      // answer which that was.
      Guid oldest = Guid.NewGuid();
      await posters.LoadAsync(oldest, delegate { return Task.FromResult<byte[]?>(Bytes(120)); });
      await shelf.FlushAsync();
      await Task.Delay(30);

      Guid second = Guid.NewGuid();
      await posters.LoadAsync(second, delegate { return Task.FromResult<byte[]?>(Bytes(120)); });
      await shelf.FlushAsync();
      await Task.Delay(30);

      Guid newest = Guid.NewGuid();
      await posters.LoadAsync(newest, delegate { return Task.FromResult<byte[]?>(Bytes(120)); });
      await shelf.FlushAsync();

      Assert.That(PicturesOn(_shelf), Is.EqualTo(2), "one of them went to keep the shelf inside its room");
      Assert.That(BytesOn(_shelf), Is.LessThanOrEqualTo(300));

      int asked = 0;
      Func<Task<byte[]?>> askedOnce = delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(Bytes(1));
      };

      await posters.LoadAsync(newest, askedOnce);
      await posters.LoadAsync(second, askedOnce);
      Assert.That(asked, Is.EqualTo(0), "the pictures nobody has looked at longest are the ones that went");

      await posters.LoadAsync(oldest, askedOnce);
      Assert.That(asked, Is.EqualTo(1), "and the one that went comes back from the server when it is asked for");
    }

    [Test]
    public async Task EmptyingTheShelfEmptiesTheFolderAndSaysSo()
    {
      PictureCache shelf = Shelf();
      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);
      await posters.LoadAsync(Guid.NewGuid(), delegate { return Task.FromResult<byte[]?>(Bytes(10)); });
      await posters.LoadAsync(Guid.NewGuid(), delegate { return Task.FromResult<byte[]?>(Bytes(10)); });
      await shelf.FlushAsync();
      Assert.That(PicturesOn(_shelf), Is.EqualTo(2));

      string says = await shelf.ClearAsync();

      Assert.That(says, Does.Contain("2"));
      Assert.That(PicturesOn(_shelf), Is.EqualTo(0), "what the viewer threw away is gone from the disk");
    }

    [Test]
    public async Task AnEmptiedShelfFillsAgain()
    {
      // Throwing the shelf away may not be the end of the shelf. The folders it lived in are gone with it, so
      // the next picture has to build them again - otherwise one press would quietly switch the keeping off for
      // the rest of this run, which is the opposite of what the row promises.
      PictureCache shelf = Shelf();
      IPictureStore posters = shelf.Store("Poster", FIRST_SERVER);
      await posters.LoadAsync(Guid.NewGuid(), delegate { return Task.FromResult<byte[]?>(Bytes(10)); });
      await shelf.FlushAsync();

      await shelf.ClearAsync();
      await shelf.FlushAsync();
      Assert.That(PicturesOn(_shelf), Is.EqualTo(0), "emptied");

      int asked = 0;
      byte[]? again = await posters.LoadAsync(Guid.NewGuid(), delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(new byte[] { 7, 8, 9 });
      });
      await shelf.FlushAsync();

      Assert.That(again, Is.EqualTo(new byte[] { 7, 8, 9 }), "the picture still arrives");
      Assert.That(asked, Is.EqualTo(1), "and it is asked for, because nothing was left to answer from");
      Assert.That(PicturesOn(_shelf), Is.EqualTo(1),
          "and it is kept again, so that the start after the emptying is only slower by the pictures it fetches");
    }

    [Test]
    public async Task AShelfThatCannotBeWrittenStillHandsThePictureOver()
    {
      // A folder that is a file: nothing can be created inside it, which is the shape of every failure a shelf
      // can have - a disk that filled up, a network drive that went away, a path somebody renamed.
      string blocked = Path.Combine(_folder, "a-file-not-a-folder");
      File.WriteAllText(blocked, "not a folder");
      Write("{\"cacheFolder\": \"" + Quote(blocked) + "\"}");

      PictureCache shelf = new PictureCache(_file);
      byte[]? picture = await shelf.Store("Poster", FIRST_SERVER).LoadAsync(Guid.NewGuid(),
          delegate { return Task.FromResult<byte[]?>(new byte[] { 5, 6 }); });
      await shelf.FlushAsync();

      Assert.That(picture, Is.EqualTo(new byte[] { 5, 6 }),
          "a shelf that cannot be used is a shelf that is not helping, not a broken library");
    }

    [Test]
    public async Task TurningTheShelfOffOnTheScreenTakesEffectWithoutARestart()
    {
      PictureCache shelf = Shelf();
      Guid title = Guid.NewGuid();
      await shelf.Store("Poster", FIRST_SERVER).LoadAsync(title,
          delegate { return Task.FromResult<byte[]?>(Bytes(4)); });
      await shelf.FlushAsync();

      Write("{\"cachePictures\": false, \"cacheFolder\": \"" + Quote(_shelf) + "\"}");
      shelf.Reload();

      int asked = 0;
      await shelf.Store("Poster", FIRST_SERVER).LoadAsync(title, delegate
      {
        asked++;
        return Task.FromResult<byte[]?>(Bytes(5));
      });

      Assert.That(shelf.Enabled, Is.False);
      Assert.That(asked, Is.EqualTo(1), "the row on the screen is the switch, not a note for the next start");
    }

    // ---------------------------------------------------------------- the screen

    [Test]
    public void TheScreenAsksTheFourQuestionsAndOffersTheDoing()
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file);

      Assert.That(screen.Heading, Is.EqualTo("Media settings"));
      Assert.That(screen.Options[0].Label, Is.EqualTo("Keep pictures on this machine"));
      Assert.That(screen.Options[0].IsToggle, Is.True, "a switch with two answers wears a checkbox");
      Assert.That(screen.Options[1].Label, Is.EqualTo("Room the kept pictures may take"));
      Assert.That(screen.Options[2].Label, Is.EqualTo("Pictures a screen keeps in mind"));
      Assert.That(screen.Options[3].Label, Is.EqualTo("Pictures on the wire at once"));
      Assert.That(screen.Options[4].Label, Is.EqualTo("Throw the kept pictures away"));
      Assert.That(screen.Options[4].IsToggle, Is.False, "the doing is a row you press, not a box you tick");
    }

    [Test]
    public void TheScreenSaysWhereThePicturesLie()
    {
      string named = Path.Combine(_folder, "auf der grossen platte");
      Write("{\"cacheFolder\": \"" + Quote(named) + "\"}");

      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file);

      Assert.That(screen.Subtitle, Does.Contain(named),
          "the screen says where the pictures lie, because that is the question a viewer with a small disk has");
    }

    [Test]
    public void PressingARowAnswersItSavesItAndSaysSo()
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file);
      SkinSettingOption room = screen.Options[1];

      Assert.That(room.ValueText, Is.EqualTo("256 MB"));
      room.Press();
      Assert.That(room.ValueText, Is.EqualTo("512 MB"));

      Assert.That(File.Exists(_file), Is.True, "a row saves in the same breath it is answered");
      Assert.That(MediaSettings.Load(_file, out _).CacheSizeMb, Is.EqualTo(512));
      Assert.That(screen.Status, Does.Contain("512 MB"));
    }

    [Test]
    public void AnAnswerKeepsWhatAnotherScreenOfTheSameKindAnswered()
    {
      // The trail keeps a screen the viewer walked back over, so two of this screen can be alive at once. The
      // one that answers next may only write its own answer: what the other one answered stays in the file.
      MediaSettingsViewModel kept = new MediaSettingsViewModel(null, _file);
      MediaSettingsViewModel remembered = new MediaSettingsViewModel(null, _file);

      kept.Options[0].IsChecked = false;
      remembered.Options[1].Press();

      MediaSettings saved = MediaSettings.Load(_file, out _);
      Assert.That(saved.CachePictures, Is.False, "the switch the first screen gave is still given");
      Assert.That(saved.CacheSizeMb, Is.EqualTo(512), "and so is the room the second one gave");

      MediaSettingsViewModel opened = new MediaSettingsViewModel(null, _file);
      Assert.That(opened.Options[0].IsChecked, Is.False, "a screen opened after both shows both answers");
      Assert.That(opened.Options[1].ValueText, Is.EqualTo("512 MB"));
    }

    [Test]
    public void TheMemoryAnswersWalkTheAnswersTheScreensWereBuiltFor()
    {
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file);

      Assert.That(screen.Options[2].ValueText, Is.EqualTo("200 pictures"));
      screen.Options[2].Press();
      Assert.That(screen.Options[2].ValueText, Is.EqualTo("400 pictures"));

      Assert.That(screen.Options[3].ValueText, Is.EqualTo("4 at a time"));
      screen.Options[3].Press();
      Assert.That(screen.Options[3].ValueText, Is.EqualTo("8 at a time"));
      screen.Options[3].Press();
      Assert.That(screen.Options[3].ValueText, Is.EqualTo("12 at a time"));
      screen.Options[3].Press();
      Assert.That(screen.Options[3].ValueText, Is.EqualTo("one at a time"),
          "and a slow link can be told one at a time, so that the tiles in sight win against the off-screen ones");
    }

    [Test]
    public async Task TheRowAboutTheShelfSaysWhatIsOnItAndPressingItEmptiesIt()
    {
      PictureCache shelf = Shelf();
      await shelf.Store("Poster", FIRST_SERVER).LoadAsync(Guid.NewGuid(),
          delegate { return Task.FromResult<byte[]?>(Bytes(4000)); });
      await shelf.FlushAsync();

      MediaSettingsViewModel screen = new MediaSettingsViewModel(shelf, _file);
      await screen.ReloadUsageAsync();
      SkinSettingOption keeping = screen.Options[4];

      Assert.That(keeping.ValueText, Does.Contain("1 pictures"));
      Assert.That(keeping.ValueText, Does.Contain("KB"), "a room is said in a size a person reads");

      keeping.Press();
      await screen.Clearing;
      await screen.ReloadUsageAsync();

      Assert.That(keeping.ValueText, Is.EqualTo("nothing yet"));
      Assert.That(screen.Status, Does.Contain("removed"));
      Assert.That(PicturesOn(_shelf), Is.EqualTo(0));
    }

    [Test]
    public void AScreenOpenedWithoutAShelfStillAnswersForTheRunThatHasOne()
    {
      // The settings hub can be reached on a machine with no server in sight. The answers are still this
      // machine's and are still written down for the run that has one.
      MediaSettingsViewModel screen = new MediaSettingsViewModel(null, _file);
      SkinSettingOption switchRow = screen.Options[0];

      Assert.That(switchRow.IsChecked, Is.True);
      switchRow.Press();

      Assert.That(switchRow.IsChecked, Is.False);
      Assert.That(MediaSettings.Load(_file, out _).CachePictures, Is.False);
    }

    [AvaloniaTest]
    public void TheScreenWearsTheRowsItAnswersWith()
    {
      // A settings screen is its controls: a viewer who finds no checkbox and no button has found a wall chart.
      // This is also the only proof that the screen's own markup loads, which no view model test can give.
      MediaSettingsViewModel model = new MediaSettingsViewModel(null, _file);
      ShellWindow window = new ShellWindow(new MediaSettingsView(model));
      window.Show();
      Pump();

      ListBox list = window.GetVisualDescendants().OfType<ListBox>().First();
      IList<Control> rows = list.GetVisualDescendants()
          .Where(delegate (Visual child) { return child is CheckBox || child is Button; })
          .OfType<Control>()
          .ToList();

      // Every tile wears both shapes of answer and shows the one that fits, so what matters is
      // how many are actually to be seen. A checkbox is a button as far as Avalonia is concerned,
      // so the switch is counted on its own and left out of the answer buttons.
      IList<CheckBox> switches = rows.OfType<CheckBox>()
          .Where(delegate (CheckBox box) { return box.IsVisible; })
          .ToList();
      IList<Button> answers = rows.OfType<Button>()
          .Where(delegate (Button button) { return !(button is CheckBox) && button.IsVisible; })
          .ToList();

      Assert.That(switches.Count, Is.EqualTo(1),
          "one switch with two answers, and it is the only switch to be seen on the screen");
      Assert.That(answers.Count, Is.EqualTo(5),
          "three questions with lists of answers, one doing and the server's own answer, each showing its "
          + "answer in a button");

      Button? room = answers.FirstOrDefault(delegate (Button button)
      {
        SkinSettingOption? row = button.DataContext as SkinSettingOption;
        return row != null && row.Label == "Room the kept pictures may take";
      });
      Assert.That(room, Is.Not.Null, "the room row shows its answer in a button of its own");
      Assert.That(room!.Content, Is.EqualTo("256 MB"),
          "and the button's face says what the machine is set to, not what the question was");

      room.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      Pump();

      Assert.That(model.Options[1].ValueText, Is.EqualTo("512 MB"),
          "the answer on the screen walks to the next size without anyone reopening the screen");
      Assert.That(MediaSettings.Load(_file, out _).CacheSizeMb, Is.EqualTo(512),
          "and the answer is in the file at once");

      window.Close();
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Letting the headless window draw what it was given, the way every other screen test does it.</summary>
    private static void Pump()
    {
      for (int round = 0; round < 20; round++)
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>A shelf that lives in this test's own folder, built from the file it wrote.</summary>
    private PictureCache Shelf()
    {
      Write("{\"cacheFolder\": \"" + Quote(_shelf) + "\"}");
      return new PictureCache(_file);
    }

    private void Write(string json)
    {
      File.WriteAllText(_file, json);
    }

    /// <summary>A Windows path inside a JSON string, with its backslashes escaped.</summary>
    private static string Quote(string path)
    {
      return path.Replace("\\", "\\\\");
    }

    /// <summary>A picture of a named length, so that a room can be counted in pictures.</summary>
    private static byte[] Bytes(int length)
    {
      byte[] bytes = new byte[length];
      for (int i = 0; i < length; i++)
        bytes[i] = (byte)(i % 251);
      return bytes;
    }

    /// <summary>How many pictures and absences lie in a folder tree, a half-written file aside.</summary>
    private static int PicturesOn(string folder)
    {
      if (!Directory.Exists(folder))
        return 0;

      return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
          .Count(delegate (string path) { return !path.EndsWith(".new", StringComparison.OrdinalIgnoreCase); });
    }

    /// <summary>How much room a folder tree takes.</summary>
    private static long BytesOn(string folder)
    {
      if (!Directory.Exists(folder))
        return 0;

      return Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
          .Sum(delegate (string path) { return new FileInfo(path).Length; });
    }
  }
}
