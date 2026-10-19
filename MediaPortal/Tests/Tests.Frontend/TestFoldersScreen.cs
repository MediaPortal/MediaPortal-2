#region Copyright (C) 2007-2023 Team MediaPortal

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

#endregion

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.ViewModels;

namespace Tests.Frontend
{
  /// <summary>
  /// The folders screen: what a viewer sees of the library's source folders, and what the server is told when a
  /// button is pressed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The server behind these tests is the real <c>SharesController</c>, hosted in this process by
  /// <see cref="InProcessApi"/>, and the folders are real temporary directories. Both matter here more than
  /// elsewhere: this screen's job is to hand a path to a server and report what that server made of it, so a
  /// fixture that answered "yes" to everything would be testing a conversation nobody has.
  /// </para>
  /// <para>
  /// A library, an accessor and an importer are the fakes (see <see cref="SharesFakes"/>). What a real server does
  /// with a folder once it has it - walking it and filling the database - is the server's own gate's business, and
  /// that gate imports a film over HTTP and reads it back.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFoldersScreen
  {
    private string _scratch;
    private string _folder;
    private string _other;
    private SharesFakes _server;
    private InProcessApi _api;
    private ServerApiClient _client;

    [SetUp]
    public void SetUp()
    {
      string scratch = Path.Combine(Path.GetTempPath(), "mp2-folders-" + Guid.NewGuid().ToString("N"));
      // Two folders side by side, because one of them is already shared by the fake server: a test that adds the
      // folder which is already in the library proves that a duplicate was refused, not that a folder was added.
      _scratch = Directory.CreateDirectory(scratch).FullName;
      _folder = Directory.CreateDirectory(Path.Combine(scratch, "first")).FullName;
      _other = Directory.CreateDirectory(Path.Combine(scratch, "second")).FullName;
      _server = new SharesFakes(_folder);
      InProcessApi.RegisterMediaLibrary(_server.Library.Object);
      ServiceRegistration.Set<ISystemResolver>(_server.Resolver.Object);
      ServiceRegistration.Set<IMediaAccessor>(_server.Accessor.Object);
      ServiceRegistration.Set<IImporterWorker>(_server.Importer.Object);

      _api = InProcessApi.Start();
      _client = new ServerApiClient(_api.BaseUrl);
    }

    [TearDown]
    public void TearDown()
    {
      InProcessApi.RemoveMediaLibrary();
      if (ServiceRegistration.IsRegistered<ISystemResolver>())
        ServiceRegistration.Remove<ISystemResolver>();
      if (ServiceRegistration.IsRegistered<IMediaAccessor>())
        ServiceRegistration.Remove<IMediaAccessor>();
      if (ServiceRegistration.IsRegistered<IImporterWorker>())
        ServiceRegistration.Remove<IImporterWorker>();

      _client?.Dispose();
      _api?.Dispose();
      try
      {
        if (_scratch != null && Directory.Exists(_scratch))
          Directory.Delete(_scratch, true);
      }
      catch (IOException)
      {
        // A scratch directory left behind is not worth failing a test over
      }
    }

    /// <summary>
    /// A view model whose queue watcher does not make the suite wait a and-a-half seconds per read.
    /// </summary>
    private FoldersViewModel WatchedQuickly()
    {
      return new FoldersViewModel(_client) { PollDelayMilliseconds = 20 };
    }

    private FoldersViewModel Opened()
    {
      FoldersViewModel folders = WatchedQuickly();
      folders.RefreshAsync().GetAwaiter().GetResult();
      return folders;
    }

    private static void Tick(FoldersViewModel folders, params string[] categories)
    {
      foreach (CategoryChoice choice in folders.Categories)
        choice.IsOn = categories.Contains(choice.Name);
    }

    #region What the list shows

    [Test]
    public void TheFoldersTheServerHoldsAreListed()
    {
      FoldersViewModel folders = Opened();

      Assert.That(folders.Shares.Count, Is.EqualTo(2));
      Assert.That(folders.Heading, Does.Contain("2 folders"));
      Assert.That(folders.Shares.Select(row => row.Name), Is.EqualTo(new[] { "Alien Earth", "Films" }));
      Assert.That(folders.Shares[1].CategoriesText, Is.EqualTo("Movie"));
      Assert.That(folders.Shares[1].Path, Is.EqualTo(_folder));
      Assert.That(folders.Shares[1].Detail, Does.Contain("on this server"));
      Assert.That(folders.Status, Does.Contain("2 folders on"));
    }

    [Test]
    public void AFolderOnAnotherMachineSaysThatItCannotBeImportedHere()
    {
      FoldersViewModel folders = Opened();

      FolderShare remote = folders.Shares.Single(row => !row.Local);
      Assert.That(remote.Detail, Does.Contain("another system"));
      Assert.That(remote.Detail, Does.Contain("cannot import"));
      // Its path is a resource path rather than a Windows one, and it is printed as it is - the only true thing
      // that can be said about a folder this server has no filesystem for.
      Assert.That(remote.Path, Does.Contain("nas"));
    }

    [Test]
    public void TheServerOwnCategoriesAreTheOnesOffered()
    {
      FoldersViewModel folders = Opened();

      Assert.That(folders.Categories.Select(choice => choice.Name),
          Is.EqualTo(new[] { "Movie", "Series", "Video" }));
      Assert.That(folders.Categories.All(choice => !choice.IsOn), Is.True,
          "nothing is ticked before anybody said what is in the folder");
    }

    [Test]
    public void AFolderWithoutAMediaTypeSaysThatNothingComesOutOfIt()
    {
      // Built rather than read: the endpoint hands back what the library holds, and a share with no categories is
      // one a test would have to talk the library into. It is the state a library that never fills up ends up in,
      // so it has to have a sentence.
      FolderShare empty = new FolderShare(new ShareInfo(Guid.Empty, "Nowhere", null,
          "{provider}:///nowhere", SharesFakes.LOCAL_SYSTEM, true, Array.Empty<string>(), true));

      Assert.That(empty.CategoriesText, Does.Contain("nothing can be imported"));
      Assert.That(empty.Path, Does.Contain("nowhere"), "a share with no local path still says where it is");
    }

    [Test]
    public void AServerWithNoFoldersSaysSoRatherThanShowingAnEmptyBox()
    {
      _server.ClearShares();

      FoldersViewModel folders = Opened();

      Assert.That(folders.Shares.Count, Is.EqualTo(0));
      Assert.That(folders.Heading, Does.Contain("No folders yet"));
      Assert.That(folders.Status, Does.Contain("no folders to import yet"));
    }

    [Test]
    public void AScreenWithoutAServerSaysThatToo()
    {
      FoldersViewModel folders = new FoldersViewModel();

      folders.RefreshAsync().GetAwaiter().GetResult();

      Assert.That(folders.Heading, Does.Contain("No server connected"));
      Assert.That(folders.Status, Does.Contain("No server connected"));
      Assert.That(folders.CanAdd, Is.False);
    }

    #endregion

    #region Adding one

    [Test]
    public async Task AFolderIsRegisteredWithTheMediaTypesThatWereTicked()
    {
      // The queue is emptied first so that the watch after the add has an answer to bring the viewer rather than
      // sitting on a job belonging to another folder; what the watch does with a job that stays is the test after
      // this one.
      _server.Jobs.Clear();
      FoldersViewModel folders = Opened();
      Tick(folders, "Movie");
      folders.NewPath = _other;

      await folders.AddAsync();

      Assert.That(_server.Registered, Has.Count.EqualTo(1));
      Share registered = _server.Registered[0];
      Assert.That(registered.MediaCategories, Is.EqualTo(new[] { "Movie" }));
      Assert.That(registered.BaseResourcePath, Is.EqualTo(LocalFsResourceProviderBase.ToResourcePath(_other)));
      Assert.That(registered.SystemId, Is.EqualTo(SharesFakes.LOCAL_SYSTEM));

      // On screen without another press of Refresh, and the field is cleared once the folder is in.
      Assert.That(folders.Shares.Count, Is.EqualTo(3));
      Assert.That(folders.Shares.Any(row => row.Path == _other), Is.True);
      Assert.That(folders.NewPath, Is.Empty);
      Assert.That(folders.Status, Does.Contain("Added"));

      await folders.QueueWatch;
      Assert.That(folders.Queue, Is.EqualTo("nothing being imported"));
      Assert.That(folders.Status, Does.Contain("Added"),
          "a queue that never had work in it did not finish anything, so it does not talk over the answer");
    }

    [Test]
    public async Task RegisteringAFolderDoesNotQueueAnImportOfItsOwn()
    {
      // The library's <c>RegisterShare</c> schedules the walk. The screen's job is to follow the queue rather than
      // claim the folder is imported; queueing a second import of the same folder would double the work.
      FoldersViewModel folders = Opened();
      Tick(folders, "Movie", "Series");
      folders.NewPath = _other;

      await folders.AddAsync();

      Assert.That(_server.Registered, Has.Count.EqualTo(1), "the folder did go in, so the check below means something");
      Assert.That(_server.Scheduled, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task NothingIsSentWhileNobodyHasSaidWhatIsInIt()
    {
      FoldersViewModel folders = Opened();
      folders.NewPath = _folder;

      await folders.AddAsync();

      Assert.That(_server.Registered, Has.Count.EqualTo(0), "no request left the machine");
      Assert.That(folders.Status, Does.Contain("tick at least one"));
      Assert.That(folders.Shares.Count, Is.EqualTo(2), "and the list stayed as it was");
    }

    [Test]
    public async Task AFolderTheServerCannotSeeIsReportedInTheServersOwnWords()
    {
      FoldersViewModel folders = Opened();
      Tick(folders, "Movie");
      folders.NewPath = Path.Combine(_folder, "there is no folder called this");

      await folders.AddAsync();

      Assert.That(_server.Registered, Has.Count.EqualTo(0));
      Assert.That(folders.Status, Does.Contain("cannot see a folder"));
      // The server's sentence says whose filesystem it means, which is the one thing a viewer holding a remote
      // cannot tell from here.
      Assert.That(folders.Status, Does.Contain("server runs on"));
    }

    [Test]
    public async Task AFolderThatIsAlreadySharedPointsAtTheShareThatOwnsIt()
    {
      FoldersViewModel folders = Opened();
      Tick(folders, "Movie");
      folders.NewPath = _folder; // the "Films" share already points here

      await folders.AddAsync();

      Assert.That(folders.Status, Does.Contain("already in the library"));
      Assert.That(folders.Shares.Count, Is.EqualTo(2), "the folder was not added a second time");
    }

    #endregion

    #region Forgetting one

    [Test]
    public async Task ForgettingAFolderIsConfirmedByASecondPress()
    {
      FoldersViewModel folders = Opened();
      FolderShare films = folders.Shares.Single(row => row.Name == "Films");
      folders.Selected = films;

      await folders.RemoveAsync();

      Assert.That(films.AskedToRemove, Is.True);
      Assert.That(films.Confirmation, Does.Contain("Press remove again"));
      Assert.That(_server.Shares.ContainsKey(SharesFakes.LOCAL_SHARE), Is.True, "nothing was deleted yet");
      Assert.That(folders.Shares.Count, Is.EqualTo(2));

      await folders.RemoveAsync();

      Assert.That(_server.Shares.ContainsKey(SharesFakes.LOCAL_SHARE), Is.False);
      Assert.That(folders.Shares.Count, Is.EqualTo(1));
      Assert.That(folders.Status, Does.Contain("no longer imported"));
    }

    [Test]
    public async Task PickingAnotherFolderTakesThePendingConfirmationBack()
    {
      FoldersViewModel folders = Opened();
      FolderShare films = folders.Shares.Single(row => row.Name == "Films");
      FolderShare alien = folders.Shares.Single(row => row.Name == "Alien Earth");
      folders.Selected = films;
      await folders.RemoveAsync();
      Assert.That(films.AskedToRemove, Is.True);

      folders.Selected = alien;
      await folders.RemoveAsync();

      Assert.That(films.AskedToRemove, Is.False, "the first folder is no longer waiting to be deleted");
      Assert.That(alien.AskedToRemove, Is.True);
    }

    #endregion

    #region The queue

    [Test]
    public async Task TheQueueIsFollowedUntilItEmpties()
    {
      // The scripted importer answers one more look with the job it holds and every look after that with nothing,
      // which is the only sequence that tells the screen "still working" from "done". Arming happens after the
      // screen has read the queue for itself, so the first of the two reads below is the one the re-import is
      // answered with.
      FoldersViewModel folders = Opened();
      _server.ArmQueueDrainAfterReads(1);
      folders.Selected = folders.Shares.Single(row => row.Local);

      await folders.ReimportAsync();

      Assert.That(folders.Queue, Does.Contain("importing 1 folder"));
      await folders.QueueWatch;
      Assert.That(folders.Queue, Is.EqualTo("nothing being imported"));
      Assert.That(folders.Status, Is.EqualTo("Import finished."));
      Assert.That(_server.Scheduled, Has.Count.EqualTo(1), "the server was asked to walk that folder");
    }

    [Test]
    public void AQueueThatIsPausedSaysSo()
    {
      Assert.That(new ImportStatus(3, true, Array.Empty<ImportJob>()).Describe(),
          Does.Contain("waiting").And.Contain("paused"));
      Assert.That(new ImportStatus(0, false, Array.Empty<ImportJob>()).Describe(),
          Is.EqualTo("nothing being imported"));
    }

    #endregion

    #region What a refusal becomes

    [Test]
    public void TheServersOwnRefusalsArePutInWordsTheViewerCanActOn()
    {
      Uri url = new Uri("http://x/api/media/shares");

      Assert.That(FoldersViewModel.DescribeAddFailure(
              new ServerApiException(url, HttpStatusCode.Conflict, "a share for that folder is already registered")),
          Does.Contain("already in the library"));
      Assert.That(FoldersViewModel.DescribeAddFailure(
              new ServerApiException(url, HttpStatusCode.BadRequest, "no media category called 'Films'")),
          Does.Contain("will not take that folder").And.Contain("no media category"));
      // A 503 is the server's problem, and the number that says so means nothing on a television.
      Assert.That(FoldersViewModel.DescribeAddFailure(
              new ServerApiException(url, HttpStatusCode.ServiceUnavailable, "no library")),
          Does.Contain("not ready").And.Not.Contain("503"));
    }

    #endregion

    #region Changing a folder that is already there

    [Test]
    public void TheEditorShowsTheSelectedFolderAndSavesWhatItShows()
    {
      FoldersViewModel folders = Opened();
      FolderShare films = folders.Shares.Single(row => row.Name == "Films");
      folders.Selected = films;

      Assert.That(folders.EditName, Is.EqualTo("Films"));
      Assert.That(folders.EditPath, Is.EqualTo(_folder));
      Assert.That(folders.EditWatcher, Is.True);
      Assert.That(folders.EditCategories.Select(choice => choice.Name + (choice.IsOn ? "!" : "")),
          Is.EqualTo(new[] { "Movie!", "Series", "Video" }),
          "the folder's own category arrives ticked and the server's other offers unticked - the editor shows "
          + "what this folder is filed as, not what could ever be ticked");

      folders.EditName = "Films and friends";
      folders.EditCategories.Single(choice => choice.Name == "Video").IsOn = true;
      folders.SaveChangesAsync().GetAwaiter().GetResult();

      Assert.That(_server.Updates.Count, Is.EqualTo(1));
      SharesFakes.Update edit = _server.Updates[0];
      Assert.That(edit.Id, Is.EqualTo(SharesFakes.LOCAL_SHARE));
      Assert.That(edit.Name, Is.EqualTo("Films and friends"));
      Assert.That(edit.Categories, Is.EqualTo(new[] { "Movie", "Video" }));
      Assert.That(edit.Relocation,
          Is.EqualTo(MediaPortal.Backend.MediaLibrary.RelocationMode.None),
          "the path box held the folder's own path, which is not a move and must not relocate anything");
      Assert.That(folders.Shares.Single(row => row.Id == SharesFakes.LOCAL_SHARE).Name,
          Is.EqualTo("Films and friends"), "the list reads the edited folder back, not the old row");
      Assert.That(folders.Status, Does.Contain("changed"));
    }

    [Test]
    public void AMovedFolderRelocatesItsShareButAnUnrelatedOneIsNoOnesBusiness()
    {
      FoldersViewModel folders = Opened();
      folders.Selected = folders.Shares.Single(row => row.Name == "Films");
      folders.EditPath = _other;
      folders.SaveChangesAsync().GetAwaiter().GetResult();

      Assert.That(_server.Updates.Single().Relocation,
          Is.EqualTo(MediaPortal.Backend.MediaLibrary.RelocationMode.Relocate),
          "an edited path is read the way the screen says it is: the folder moved, and its items follow");
      Assert.That(folders.Shares.Single(row => row.Id == SharesFakes.LOCAL_SHARE).Path, Is.EqualTo(_other));
    }

    [Test]
    public void AFolderLeftWithoutASingleCategoryIsNotSent()
    {
      FoldersViewModel folders = Opened();
      folders.Selected = folders.Shares.Single(row => row.Name == "Films");
      foreach (CategoryChoice choice in folders.EditCategories)
        choice.IsOn = false;

      folders.SaveChangesAsync().GetAwaiter().GetResult();

      Assert.That(_server.Updates, Is.Empty,
          "a share without categories is imported by nobody - the server would take it, and the empty shelf "
          + "would only explain itself weeks later");
      Assert.That(folders.Status, Does.Contain("ticked"),
          "and the screen says what to do instead");
    }

    [Test]
    public void ARemoteFolderKeepsThePathItIsOn()
    {
      FoldersViewModel folders = Opened();
      FolderShare remote = folders.Shares.Single(row => !row.Local);
      folders.Selected = remote;
      folders.EditPath = _other;

      folders.SaveChangesAsync().GetAwaiter().GetResult();

      Assert.That(_server.Updates, Is.Empty,
          "this server cannot follow a folder it does not host, and it says no rather than storing a lie");
      Assert.That(folders.Status, Does.Contain("another system"));
    }

    #endregion
  }
}
