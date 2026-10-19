#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;
using MediaPortal.Frontend.Views;
using MediaPortal.Server.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using UPnP.Infrastructure.Http;

namespace Tests.Frontend
{
  /// <summary>
  /// The pictures pillar: the server's answer about what a folder holds, the client that reads it, and the screen
  /// a person walks through.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Three layers, because the boundary is the interesting part. The controller's rule is about refusal - a folder
  /// that is not inside a share is not talked about - and that is tested by handing it a guard whose answer is
  /// known rather than by building a disk. The client is tested over a real socket against a scripted server, so
  /// what the two ends of the wire agree about is a fact and not an intention. The screen is tested against the
  /// same scripted server, because "the folder was read" and "the folder is on the television" are different
  /// claims.
  /// </para>
  /// <para>
  /// What a real directory walk on a real disk looks like is proved for the same plumbing in
  /// Tests.Server/Api/TestResourceAccessModuleOverHttp; nothing here pretends to repeat it.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestPicturesScreen
  {
    // Real serialized resource paths, built by the same call that builds one for a file on this disk, over a
    // folder in this machine's temp directory. Spelled out by hand they would be a guess about the format - the
    // "{local}://..." spelling is a shorthand from the documentation, while what ProviderPathSegment.Serialize()
    // produces is a provider GUID - and the whole point of the client tests is that the two ends of the wire
    // agree about the real thing.
    private static readonly string PHOTOS_DIR =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MediaPortal.Pictures.Test");
    private static readonly string TRIP_DIR = System.IO.Path.Combine(PHOTOS_DIR, "2019");
    private static readonly string PHOTOS =
        LocalFsResourceProviderBase.ToResourcePath(PHOTOS_DIR).Serialize();
    private static readonly string TRIP =
        LocalFsResourceProviderBase.ToResourcePath(TRIP_DIR).Serialize();

    private readonly List<IDisposable> _toDispose = new List<IDisposable>();
    private readonly List<Window> _windows = new List<Window>();
    private List<string> _askedFor;
    private string _scriptedListing;

    /// <summary>
    /// How long the scripted server takes to answer a folder. Zero, as usual; a test that wants to know what the
    /// screen does with an answer that came too late sets it.
    /// </summary>
    private volatile int _browseDelayMs;

    [SetUp]
    public void Prepare()
    {
      _askedFor = new List<string>();
      _scriptedListing = Listing(TRIP, "2019", 1, 1, 3, false);
      _browseDelayMs = 0;
      ServiceRegistration.Remove<IResourceAccessGuard>();
    }

    [TearDown]
    public void DisposeOfEverything()
    {
      // Screens go before servers: a window still on screen would keep asking a view model whose test has
      // finished, and the answer it got would belong to no test at all.
      foreach (Window window in _windows)
      {
        try
        {
          window.Close();
        }
        catch (Exception)
        {
          // A screen that would not close at the end of a test says nothing about the keys it answered.
        }
      }
      _windows.Clear();

      foreach (IDisposable thing in _toDispose)
      {
        IDisposable closing = thing;
        // Closing the listener from the display thread deadlocks - the hazard TestHomeWindow measured and wrote
        // down - so it is closed where no display thread is involved, and this thread waits for that one, which it
        // may do because nothing on the pool thread needs anything back from here.
        //
        // Waiting is not tidiness either: a port goes back in the pool of free ports when the listener closes,
        // which is at the start of the shutdown rather than the end, so a test asking for a port while the
        // previous test's server is still saying goodbye can be handed the same number and join a listener on its
        // way out. Its requests are answered until the goodbye finishes and hang after that, which looks exactly
        // like a screen that stopped listening to its server.
        if (!Task.Run(delegate
        {
          try
          {
            closing.Dispose();
          }
          catch (Exception)
          {
            // A server that could not close itself down cleanly at the end of a test says nothing about the
            // screen the test was looking at.
          }
        }).Wait(TimeSpan.FromSeconds(10)))
        {
          TestContext.Progress.WriteLine("a scripted server of " + TestContext.CurrentContext.Test.Name
              + " was still closing after ten seconds");
        }
      }
      _toDispose.Clear();
      ServiceRegistration.Remove<IResourceAccessGuard>();
    }

    // The controller's rule

    [Test]
    public void AFolderOutsideEveryShareIsRefusedWithoutExplainingItself()
    {
      Register(delegate (ResourcePath path) { return false; });

      IActionResult answer = new BrowseController().Browse(PHOTOS);

      Assert.That(answer, Is.TypeOf<StatusCodeResult>(),
          "a refusal with a body is a description of what the server does have");
      Assert.That(((StatusCodeResult) answer).StatusCode, Is.EqualTo(403));
    }

    [Test]
    public void AServerThatNeverSaidWhatItMayShowShowsNothing()
    {
      // No guard at all. The file server fails closed in the same case and for the same reason: the missing piece
      // is a decision, and guessing one is not the safer answer.
      IActionResult answer = new BrowseController().Browse(PHOTOS);

      Assert.That(((StatusCodeResult) answer).StatusCode, Is.EqualTo(403),
          "the same refusal as \"this is not one of my shares\", so a stranger cannot tell the two apart");
    }

    [Test]
    public void ARequestThatNamesNoFolderIsRefused()
    {
      Assert.That(new BrowseController().Browse(null), Is.TypeOf<BadRequestObjectResult>());
      Assert.That(new BrowseController().Browse(string.Empty), Is.TypeOf<BadRequestObjectResult>());
    }

    [Test]
    public void TheShareRootItselfIsBrowsableEvenThoughItCannotBeStreamed()
    {
      // The whole reason the guard asks two questions. A folder cannot be handed over as bytes, but browsing has
      // to start somewhere, and that somewhere is the folder somebody registered.
      bool askedTheWeakQuestion = false;
      Register(delegate (ResourcePath path)
      {
        askedTheWeakQuestion = true;
        return true;
      });

      // What is under test is which question the controller asks, not the walk itself - the walk over a real disk
      // is proved in Tests.Server, and this process has no resource providers registered at all.
      try
      {
        new BrowseController().Browse(PHOTOS);
      }
      catch (Exception)
      {
      }

      Assert.That(askedTheWeakQuestion, Is.True,
          "if this asked the strong question instead, the first folder a viewer ever opens would be refused");
    }

    [Test]
    public void APictureIsKnownByItsExtensionAndNothingElse()
    {
      Assert.That(BrowseController.IsPicture("Beach.JPG"), Is.True, "case is not a second format");
      Assert.That(BrowseController.IsPicture("scan.jpeg"), Is.True);
      Assert.That(BrowseController.IsPicture("old.jfif"), Is.True, "cameras and scanners wrote that for years");
      Assert.That(BrowseController.IsPicture("clip.mkv"), Is.False);
      Assert.That(BrowseController.IsPicture("notes.nfo"), Is.False);
      Assert.That(BrowseController.IsPicture("README"), Is.False, "no extension is not a picture");
      Assert.That(BrowseController.IsPicture(null), Is.False);
    }

    // The client, over a socket

    [Test]
    public async Task TheFolderIsOneEscapedArgumentAndItsContentsArriveAsTiles()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      {
        FolderListing listing = await client.GetFolderAsync(PHOTOS);

        string asked = _askedFor[0];
        Assert.That(asked, Does.Contain("path=%7B").And.Contain("%3A").And.Contain("%2F"),
            "a serialized resource path is full of ':' and '/' and sometimes a space; it travels as one escaped "
            + "argument or it travels as three broken ones");
        Assert.That(listing.Name, Is.EqualTo("2019"));
        Assert.That(listing.Folders.Count, Is.EqualTo(1));
        Assert.That(listing.Pictures.Count, Is.EqualTo(1));
        Assert.That(listing.Pictures[0].Name, Is.EqualTo("beach"),
            "the extension is the file manager's business, not something to print on a television");
        Assert.That(listing.OtherFileCount, Is.EqualTo(3),
            "the files that are not pictures are counted rather than hidden");
      }
    }

    [Test]
    public async Task APictureTheServerWontShowIsNoPictureRatherThanAFault()
    {
      string baseUrl = Start(delegate (HttpContext context)
      {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
      });

      using (var client = new ServerApiClient(new Uri(baseUrl)))
      {
        Assert.That(await client.GetResourceBytesAsync(PHOTOS + "/gone.jpg"), Is.Null,
            "a file outside every share and a file that is gone look the same to a picture screen, and neither is "
            + "a reason to tell the viewer the server is broken");
      }
    }

    [Test]
    public async Task AServerThatBreaksIsStillReportedAsBreaking()
    {
      string baseUrl = Start(delegate (HttpContext context)
      {
        context.Response.StatusCode = 500;
        return Task.CompletedTask;
      });

      using (var client = new ServerApiClient(new Uri(baseUrl)))
      {
        ServerApiException failure = null;
        try
        {
          await client.GetResourceBytesAsync(PHOTOS + "/one.jpg");
        }
        catch (ServerApiException e)
        {
          failure = e;
        }

        Assert.That(failure, Is.Not.Null,
            "a server that broke is not the same answer as a folder with nothing in it, and a screen that could "
            + "not tell them apart would empty the shelf instead of saying what happened");
        Assert.That(failure.StatusCode, Is.EqualTo(500));
      }
    }

    // The screen

    [AvaloniaTest]
    public void TheScreenOpensOnTheFoldersTheServerWasToldAbout()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        pictures.RefreshCommand.Execute(null);
        WaitFor(delegate { return pictures.Status != "Not read yet."; }, "the shares to be read");

        Assert.That(pictures.Tiles.Count, Is.EqualTo(1), "the shares are the root, because that is all the server "
            + "will look inside of");
        Assert.That(pictures.Tiles[0].Name, Is.EqualTo("Photos"));
        Assert.That(pictures.CanGoUp, Is.False, "there is nothing above the list of shares");
      }
    }

    [AvaloniaTest]
    public void WalkingIntoAFolderRemembersWhereItCameFromAndBackGoesThere()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        pictures.RefreshCommand.Execute(null);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the shares to be read");

        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Heading == "2019"; }, "the folder to be read");

        Assert.That(pictures.CanGoUp, Is.True);
        Assert.That(pictures.Tiles.Count, Is.EqualTo(2), "one subfolder and one picture");

        pictures.GoUpCommand.Execute(null);
        WaitFor(delegate { return pictures.Heading == "Pictures"; }, "the list of shares to come back");

        Assert.That(pictures.CanGoUp, Is.False);
      }
    }

    [AvaloniaTest]
    public void OnlyTheFirstTilesOfAFolderAreFetchedAsPictures()
    {
      _scriptedListing = Listing(TRIP, "2019", 0, 40, 0, false);
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        _ = pictures.OpenAsync(new PictureTile(new FolderEntry { Name = "2019", Path = TRIP }));
        WaitFor(delegate { return pictures.Tiles.Count == 40; }, "the folder to be read");

        WaitFor(delegate
        {
          return Asked("/GetResource") >= PicturesViewModel.TILES_WITH_A_PICTURE;
        }, "the first tiles to have their picture");

        // And then it stops. Pumping for another half second gives any tile beyond the limit the time it would
        // need to fetch its picture, so "still the same number" means "nobody asked", not "nobody got round to it".
        for (int i = 0; i < 100; i++)
          Pump();

        Assert.That(Asked("/GetResource"), Is.EqualTo(PicturesViewModel.TILES_WITH_A_PICTURE),
            "there is no way to ask the server for a smaller picture, so a preview costs the whole photograph - "
            + "worth it for the tiles a viewer can see and not for four thousand");
        Assert.That(pictures.Tiles[PicturesViewModel.TILES_WITH_A_PICTURE].PictureNote, Is.Not.Empty,
            "and a tile without one says so instead of showing a grey box");
      }
    }

    [AvaloniaTest]
    public void AFolderTheServerWontShowSaysBothThingsItCouldMean()
    {
      string baseUrl = Start(delegate (HttpContext context)
      {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
      });

      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        _ = pictures.OpenAsync(new PictureTile(new FolderEntry { Name = "Photos", Path = PHOTOS }));
        WaitFor(delegate { return pictures.Status != "Not read yet."; }, "the refusal to be reported");

        Assert.That(pictures.Status, Does.Contain("sign in").Or.Contain("share"),
            "a 403 here means either \"not one of my folders\" or \"I do not know who you are\", and a viewer "
            + "cannot tell those apart from here");
      }
    }

    // The wording of a folder

    [Test]
    public void ACountThatStoppedCountingSaysSo()
    {
      FolderListing listing = ReadListing(Listing(TRIP, "2019", 0, 500, 0, true));

      Assert.That(listing.Summary, Does.Contain("500+"),
        "a folder whose count stopped at the limit is not a folder with exactly five hundred pictures");
    }

    [Test]
    public void AFolderOfNothingButOtherFilesSaysWhatItIs()
    {
      Assert.That(ReadListing(Listing(TRIP, "2019", 0, 0, 12, false)).Summary,
          Does.Contain("no pictures").And.Contain("12 files"));
    }

    [Test]
    public void AFolderOnADiskIsSpelledTheWayAPersonSpellsIt()
    {
      FolderListing listing = ReadListing(Listing(TRIP, "2019", 0, 1, 0, false));

      Assert.That(listing.LocalPath, Is.EqualTo(TRIP_DIR),
          "the provider spelling - a GUID, a '://' and a slash in front of the drive - is what the server wants "
          + "and the wrong thing to put under a folder name on a television");
      Assert.That(listing.LocalPath, Does.Not.Contain("://"));
    }

    // The keys

    [AvaloniaTest]
    public void TheNextArrowWalksTheCursorOntoTheNextTileWithNoMouseAnywhere()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Tiles.Count == 2; }, "the folder to be read");

        ListBox tiles = TilesOn(window);
        Assert.That(tiles.Focus(), Is.True,
            "the grid has to be able to hold the focus, because that is where a remote leaves it");

        pictures.Selected = pictures.Tiles[0];
        Pump();
        ListBoxItem underTheCursor = ContainerFor(window, pictures.Tiles[0]);
        Assert.That(underTheCursor.Focus(), Is.True,
            "the tile the viewer is standing on has to be able to hold the focus");

        // Through the headless platform's input pipeline rather than raised on the grid as the other keys here
        // are: walking a selection along a grid is Avalonia's own code, and that code follows the container that
        // holds the focus, so a key raised on the grid alone moves nothing.
        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.None, null);
        Pump();
        WaitFor(delegate { return pictures.Tiles.IndexOf(pictures.Selected) == 1; },
            "the cursor to reach the next tile");

        Assert.That(tiles.SelectedIndex, Is.EqualTo(1),
            "what the grid has selected and what the screen thinks is selected cannot be two facts");
        Assert.That(pictures.Tiles[1].Name, Is.EqualTo(pictures.Selected.Name),
            "the arrow that means \"the next one\" has to land on the next tile, not somewhere else");
      }
    }

    [AvaloniaTest]
    public void EnterOnAFolderTileWalksIntoItAndTheArrowsThenBelongToTheNewFolder()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Tiles.Count == 2; }, "the folder to be read");

        pictures.Selected = pictures.Tiles[0];
        Pump();
        Assert.That(pictures.Selected.IsFolder, Is.True, "the test wants a folder under the cursor");

        // What the folder the test is about to walk into holds, said differently, so that walking into it can be
        // told apart from still standing where the test started.
        _scriptedListing = Listing(TRIP, "The trip", 0, 2, 0, false);

        Assert.That(PressKey(window.Screen!, Key.Enter), Is.True, "the screen has to claim the key");
        WaitFor(delegate { return pictures.Heading == "The trip"; }, "the folder to be read");

        Assert.That(pictures.Tiles.Count, Is.EqualTo(2), "the tiles on screen are the new folder's, not the old "
            + "one's with a new title over them");
      }
    }

    [AvaloniaTest]
    public void EnterOnAPhotographRaisesItRatherThanWalkingIntoIt()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");
        _scriptedListing = Listing(TRIP, "2019", 0, 3, 0, false);
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Tiles.Count == 3; }, "the folder to be read");

        pictures.Selected = pictures.Tiles[0];
        Pump();

        PressKey(window.Screen!, Key.Enter);
        WaitFor(delegate { return pictures.IsViewing; }, "the photograph to be raised");

        Assert.That(pictures.Viewing, Is.SameAs(pictures.Tiles[0]));
        Assert.That(pictures.CanGoUp, Is.True, "raising a photograph does not leave the folder");
        WaitFor(delegate { return pictures.ViewingBytes != null; }, "the bytes of the photograph");
        Assert.That(pictures.ViewingBytes!.Length, Is.EqualTo(4), "the screen shows the picture the server handed "
            + "over, not a placeholder drawn in its place");
      }
    }

    [AvaloniaTest]
    public void BackStepsOutOfAFolderAndOnlyLeavesTheScreenFromTheListOfShares()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Heading == "2019"; }, "the folder to be read");

        PressKey(window.Screen!, Key.Back);
        WaitFor(delegate { return pictures.Heading == "Pictures"; }, "to be back at the list of shares");

        Assert.That(window.IsVisible, Is.True,
            "pressing back in the middle of a folder means out of the folder, not away from pictures");
        Assert.That(pictures.CanGoUp, Is.False);

        PressKey(window.Screen!, Key.Back);
        Pump();
        Assert.That(window.IsVisible, Is.False, "from the one place with nowhere up to go, the key leaves");
      }
    }

    /// <summary>
    /// A folder whose answer arrives after the viewer walked back out of the walk must not be painted over the
    /// screen they are standing in front of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Walking into a folder and pressing back are two key presses, and on a slow server they can finish in either
    /// order. The screen the viewer is looking at at the end is the list of shares, and a folder arriving late is
    /// not a reason to move them somewhere they just left.
    /// </para>
    /// <para>
    /// This is the shape the first version of the screen got wrong, and the suite found it rather than anybody:
    /// there the folder was slow only because a screenful of other tests had the machine busy, which is a way for
    /// a bug to hide in plain sight. The delay below is what turns that timing into a fact of the test instead of
    /// a matter of luck.
    /// </para>
    /// </remarks>
    [AvaloniaTest]
    public void AFolderThatArrivesLateDoesNotPaintOverTheFolderTheViewerLeft()
    {
      _browseDelayMs = 1500;
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");

        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.CanGoUp; }, "the walk into the folder to have started");

        PressKey(window.Screen!, Key.Back);
        WaitFor(delegate { return pictures.Heading == "Pictures"; }, "to be back at the list of shares");

        // Now let the folder turn up. It was asked for while the viewer was walking in, and it has nowhere to go.
        Thread.Sleep(2500);
        Pump();

        Assert.That(pictures.Heading, Is.EqualTo("Pictures"),
            "the folder the viewer walked out of must not be put back over the list of shares");
        Assert.That(pictures.Tiles.Count, Is.EqualTo(1), "and its tiles must not be on the screen either");
        Assert.That(window.IsVisible, Is.True);
      }
    }

    [AvaloniaTest]
    public void WhileAPhotographIsRaisedTheLeftAndRightKeysBelongToThePhotograph()
    {
      string baseUrl = Start();
      using (var client = new ServerApiClient(new Uri(baseUrl)))
      using (var pictures = new PicturesViewModel(client))
      {
        ShellWindow window = Show(pictures);
        WaitFor(delegate { return pictures.Tiles.Count == 1; }, "the list of shares");
        _scriptedListing = Listing(TRIP, "2019", 0, 3, 0, false);
        _ = pictures.OpenAsync(pictures.Tiles[0]);
        WaitFor(delegate { return pictures.Tiles.Count == 3; }, "the folder to be read");

        pictures.Selected = pictures.Tiles[0];
        Pump();
        PressKey(window.Screen!, Key.Enter);
        WaitFor(delegate { return pictures.IsViewing; }, "the photograph to be raised");
        int selectedBefore = pictures.Tiles.IndexOf(pictures.Selected);

        PressKey(window.Screen!, Key.Right);
        Assert.That(pictures.Caption, Does.Contain("2 of 3"), "the key the viewer is holding moves the photograph");
        Assert.That(pictures.Tiles.IndexOf(pictures.Selected), Is.EqualTo(selectedBefore),
            "and does not move the cursor behind the photograph out of sight");

        PressKey(window.Screen!, Key.Escape);
        Assert.That(pictures.IsViewing, Is.False);
        Assert.That(window.IsVisible, Is.True, "putting one photograph down is not leaving the pictures");
      }
    }

    // The plumbing of the fixture

    private static FolderListing ReadListing(string json)
    {
      return System.Text.Json.JsonSerializer.Deserialize<FolderListing>(json,
          new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
    }

    private static string Listing(string path, string name, int folders, int pictures, int others,
        bool picturesTruncated)
    {
      List<string> folderEntries = new List<string>();
      for (int i = 0; i < folders; i++)
        folderEntries.Add(string.Format("{{\"name\":\"sub{0}\",\"path\":\"{1}/sub{0}\"}}", i, path));
      List<string> pictureEntries = new List<string>();
      for (int i = 0; i < pictures; i++)
      {
        // The first picture of a folder gets a name a person would recognise; the rest only have to be distinct.
        string fileName = i == 0 ? "beach" : "p" + i.ToString();
        pictureEntries.Add(string.Format(
            "{{\"name\":\"{2}\",\"path\":\"{1}/{2}.jpg\",\"size\":120,\"lastChanged\":\"2019-06-01T10:00:00\"}}",
            i, path, fileName));
      }

      return string.Format(
          "{{\"path\":\"{0}\",\"name\":\"{1}\",\"folders\":[{2}],\"pictures\":[{3}],\"foldersTruncated\":false,"
          + "\"picturesTruncated\":{4},\"otherFileCount\":{5}}}",
          path, name, string.Join(",", folderEntries.ToArray()),
          string.Join(",", pictureEntries.ToArray()), picturesTruncated ? "true" : "false", others);
    }

    private void Register(Func<ResourcePath, bool> rule)
    {
      ServiceRegistration.Set<IResourceAccessGuard>(new ScriptedGuard(rule));
    }

    private int Asked(string suffix)
    {
      lock (_askedFor)
        return _askedFor.FindAll(asked => asked.Contains(suffix)).Count;
    }

    /// <summary>
    /// A server that answers the three addresses the pictures screen uses, and remembers what it was asked.
    /// </summary>
    /// <remarks>
    /// The pipeline is booted on another thread, and <see cref="DisposeOfEverything"/> closes it on one. Both
    /// directions of <see cref="SharedHttpPortHost"/> wait synchronously for their own startup and shutdown, and
    /// ASP.NET Core hands part of both to the context it was asked from; asked from a thread that owns a user
    /// interface dispatcher, that wait is a wait for that thread, which is how a test body that starts a server
    /// hangs instead of failing. On an ordinary thread none of this shows, which is why the three client tests
    /// here never noticed. What the server answers is unaffected by which thread decided to start it.
    /// </remarks>
    private string Start(Func<HttpContext, Task>? instead = null)
    {
      int port = InProcessApi.GetFreePort();
      string prefix = string.Format("http://127.0.0.1:{0}/MediaPortal/Server", port);
      string baseUrl = prefix + "/";

      IDisposable host = null;
      Exception failure = null;
      bool started = Task.Run(delegate
      {
        try
        {
          host = SharedHttpPortHost.Start(
              new[] { prefix },
              services => { },
              app => app.Run(async context =>
              {
                lock (_askedFor)
                  _askedFor.Add(context.Request.Path + context.Request.QueryString);

                if (instead != null)
                {
                  await instead(context);
                  return;
                }

                string body;
                if (context.Request.Path.ToString().EndsWith("/api/media/shares", StringComparison.Ordinal))
                  body = "{\"count\":1,\"shares\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Photos\","
                      + "\"path\":\"" + Json(PHOTOS_DIR) + "\",\"resourcePath\":\"" + PHOTOS + "\","
                      + "\"systemId\":\"host\",\"local\":true,"
                      + "\"mediaCategories\":[\"Image\"],\"useShareWatcher\":true}],\"mediaCategories\":[\"Image\"]}";
                else if (context.Request.Path.ToString().EndsWith("/api/media/browse", StringComparison.Ordinal))
                {
                  if (_browseDelayMs > 0)
                    await Task.Delay(_browseDelayMs, context.RequestAborted);
                  body = _scriptedListing;
                }
                else
                {
                  // A picture's bytes. Any bytes will do - nothing here decodes them.
                  context.Response.ContentType = "image/jpeg";
                  byte[] bytes = new byte[] { 1, 2, 3, 4 };
                  await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                  return;
                }

                context.Response.ContentType = "application/json";
                byte[] json = System.Text.Encoding.UTF8.GetBytes(body);
                await context.Response.Body.WriteAsync(json, 0, json.Length);
              }));
        }
        catch (Exception e)
        {
          failure = e;
        }
      }).Wait(TimeSpan.FromSeconds(30));

      if (failure != null)
        Assert.Fail("the scripted server could not start: " + failure.GetBaseException().Message);
      if (!started || host == null)
        Assert.Fail("the scripted server did not come up within 30 seconds.");

      _toDispose.Add(host);
      return baseUrl;
    }

    /// <summary>
    /// A path as it is written inside a JSON string, since a folder name on Windows is full of backslashes.
    /// </summary>
    private static string Json(string text)
    {
      return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Puts the screen on the (headless) display and takes responsibility for closing it again.
    /// </summary>
    private ShellWindow Show(PicturesViewModel pictures)
    {
      var window = new ShellWindow(new PicturesView(pictures));
      window.Show();
      Pump();
      _windows.Add(window);
      return window;
    }

    private static ListBox TilesOn(Window window)
    {
      return window.GetVisualDescendants().OfType<ListBox>().SingleOrDefault()
          ?? throw new AssertionException("the pictures screen has no grid of tiles");
    }

    /// <summary>
    /// The container the grid put a tile in, which is the thing that holds the focus while a viewer stands on that
    /// tile - the tile itself is only data.
    /// </summary>
    private static ListBoxItem ContainerFor(Window window, PictureTile tile)
    {
      return window.GetVisualDescendants().OfType<ListBoxItem>()
          .SingleOrDefault(item => ReferenceEquals(item.DataContext, tile))
          ?? throw new AssertionException("the tile \"" + tile.Name + "\" was never given a container to stand in");
    }

    /// <summary>
    /// Raises a key the way Avalonia does, and reports whether the screen claimed it.
    /// </summary>
    private static bool PressKey(Control on, Key key)
    {
      var args = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, Source = on };
      on.RaiseEvent(args);
      Pump();
      return args.Handled;
    }

    /// <summary>
    /// Runs the dispatcher and lets the network catch up, which is the only way this thread can watch a screen
    /// that never blocks on its server.
    /// </summary>
    private static void Pump()
    {
      Avalonia.Threading.Dispatcher.UIThread.RunJobs();
      Thread.Sleep(5);
    }

    /// <summary>
    /// Waits for the screen to settle into the state a test wants to look at.
    /// </summary>
    /// <remarks>
    /// A test here must not <c>await</c> a command or a client call. The screen picks up the answer where it
    /// asked - on the user interface thread - and that thread is the one running the test, so a test waiting for
    /// the answer would be waiting for itself. Waiting by pumping is what the rest of the front-end suite does,
    /// and the deadline is here so that a regression fails instead of hanging the runner.
    /// </remarks>
    private static void WaitFor(Func<bool> condition, string what)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (!condition())
      {
        if (watch.Elapsed.TotalSeconds > 15)
          Assert.Fail("timed out waiting for " + what);
        Pump();
      }
    }

    private class ScriptedGuard : IResourceAccessGuard
    {
      private readonly Func<ResourcePath, bool> _rule;

      internal ScriptedGuard(Func<ResourcePath, bool> rule)
      {
        _rule = rule;
      }

      public bool IsAllowed(ResourcePath resourcePath)
      {
        return _rule(resourcePath);
      }

      public bool IsInsideAShare(ResourcePath resourcePath)
      {
        return _rule(resourcePath);
      }
    }
  }
}