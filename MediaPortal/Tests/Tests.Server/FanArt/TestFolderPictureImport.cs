#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Logging;
using NUnit.Framework;
using Tests.Server.FanArt.FanArtHandlersForTests;
using Tests.Server.FanArt.MockFanArtAccess;

namespace Tests.Server.FanArt
{
  /// <summary>
  /// Tests how the pictures lying in a title's own folder reach the fan art cache, and under which kind each of
  /// them is filed.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This walk is the reference's <c>Extract*FolderFanArt</c> step, and on a server that is not Windows it used
  /// to be missing altogether: the four extractors that ran it there are all built for Windows, so a media
  /// manager that had filed <c>clearlogo.png</c>, <c>clearart.png</c> or <c>disc.png</c> beside a film saw
  /// nothing of it reach the library. Measured on a shelf of 1136 films, that was 937 logos, 708 clear arts and
  /// 777 disc pictures thrown away by nobody in particular.
  /// </para>
  /// <para>
  /// The tests below therefore pin the names as well as the walking, because the name is the only information a
  /// picture lying in a folder carries: <c>disc.png</c> has to become disc art and <c>clearlogo.png</c> a logo,
  /// and a walk that filed everything as a backdrop would be worse than no walk at all.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFolderPictureImport
  {
    MockResourceAccess _mockResourceAccess;
    RecordingFanArtCache _fanArtCache;

    /// <summary>
    /// A film's folder as a media manager leaves it: one picture per kind, the extras folder, and two files
    /// that are not pictures at all.
    /// </summary>
    const string FILM_FOLDER = "Movies/The Physician (2013)";

    [OneTimeSetUp]
    public void Setup()
    {
      _mockResourceAccess = new MockResourceAccess();
      _mockResourceAccess.Provider.AddDirectory(FILM_FOLDER + "/", new[]
      {
        "poster.png", "fanart.jpg", "clearlogo.png", "clearart.png", "disc.png", "banner.jpg", "thumb.jpg",
        "the-physician-clearart.png", "movie.mkv", "notes.txt"
      });
      _mockResourceAccess.Provider.AddDirectory(FILM_FOLDER + "/ExtraFanArt/", new[] { "fanart-01.jpg" });
      _mockResourceAccess.Provider.AddDirectory("Movies/Empty/", new[] { "movie.mkv" });
    }

    [SetUp]
    public void ClearCache()
    {
      _fanArtCache = new RecordingFanArtCache();
      ServiceRegistration.Set<IFanArtCache>(_fanArtCache);
      // A folder that is away is written down as a warning rather than thrown at the importer, and a warning
      // needs the log service the same way everything else on a running server does.
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [Test]
    public void EveryPictureIsFiledUnderTheKindItsNameSays()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      int found = handler.TestSaveFolderPictures("test", FolderPath(FILM_FOLDER), filmId, "The Physician").Result;

      Assert.That(Stored(FanArtTypes.Poster), Has.Some.Contains("poster.png"));
      Assert.That(Stored(FanArtTypes.FanArt), Has.Some.Contains("fanart.jpg"));
      Assert.That(Stored(FanArtTypes.Logo), Has.Some.Contains("clearlogo.png"),
          "a clear logo is a logo, not a poster and not a backdrop");
      Assert.That(Stored(FanArtTypes.ClearArt), Has.Some.Contains("clearart.png"));
      Assert.That(Stored(FanArtTypes.DiscArt), Has.Some.Contains("disc.png"),
          "the reference lists \"disc\" among the disc art names, which is the name the media managers use");
      Assert.That(Stored(FanArtTypes.Banner), Has.Some.Contains("banner.jpg"));
      Assert.That(Stored(FanArtTypes.Thumbnail), Has.Some.Contains("thumb.jpg"));

      // The count a caller is told is what the names asked for, so a log line can say whether a shelf holds
      // anything at all: the poster, backdrop, logo, clear art, disc, banner and thumb of the film. The picture
      // named after the film is not among them, because no file name was given to match it against.
      Assert.That(found, Is.EqualTo(7));
    }

    [Test]
    public void ThingsThatAreNotPicturesAreLeftAlone()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      handler.TestSaveFolderPictures("test", FolderPath(FILM_FOLDER), filmId, "The Physician").Wait();

      Assert.That(_fanArtCache.AllPaths(), Has.None.Contains("movie.mkv"));
      Assert.That(_fanArtCache.AllPaths(), Has.None.Contains("notes.txt"));
    }

    [Test]
    public void ACallerThatNamesTheFileAlsoGetsItsNamedPictures()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      int found = handler.TestSaveFolderPictures("test", FolderPath(FILM_FOLDER), filmId, "The Physician",
          "the-physician").Result;

      Assert.That(Stored(FanArtTypes.ClearArt), Has.Some.Contains("the-physician-clearart.png"),
          "a media manager files a shelf's art after the film's own name, and the reference reads those too");
      Assert.That(Stored(FanArtTypes.Poster), Has.Some.Contains("poster.png"),
          "naming the file adds pictures, it does not replace the plain names");
      Assert.That(Stored(FanArtTypes.ClearArt), Has.Some.Contains("clearart.png"),
          "a plain name and a name built from the file are two pictures of the same kind, not one that beats "
          + "the other - the helper ranking them used to hand both the same rank and complain about it, which "
          + "lost the whole folder for the title");
      Assert.That(found, Is.EqualTo(8));
    }

    [Test]
    public void ACallerThatNamesNoFileDoesNotGetTheNamedOnes()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      handler.TestSaveFolderPictures("test", FolderPath(FILM_FOLDER), filmId, "The Physician").Wait();

      Assert.That(_fanArtCache.AllPaths(), Has.None.Contains("the-physician-clearart.png"),
          "without a file name there is nothing to match a picture named after a file against");
    }

    [Test]
    public void FurtherBackdropsComeOutOfTheFolderTheMediaManagersUse()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      handler.TestSaveFolderPictures("test", FolderPath(FILM_FOLDER), filmId, "The Physician", null,
          "ExtraFanArt/").Wait();

      Assert.That(Stored(FanArtTypes.FanArt), Has.Some.Contains("ExtraFanArt/fanart-01.jpg"),
          "the reference reads ExtraFanArt for the shelves that hold more than the one backdrop");
    }

    [Test]
    public void AFolderWithoutPicturesIsNoErrorAndNoWork()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      int found = handler.TestSaveFolderPictures("test", FolderPath("Movies/Empty"), filmId, "Empty", null,
          "ExtraFanArt/").Result;

      Assert.That(found, Is.EqualTo(0));
      Assert.That(_fanArtCache.Stored, Is.Empty);
    }

    [Test]
    public void AFolderThatCannotBeReadIsNoError()
    {
      Guid filmId = Guid.NewGuid();
      BaseFanArtHandlerForTests handler = new BaseFanArtHandlerForTests();

      int found = handler.TestSaveFolderPictures("test", FolderPath("Movies/Nowhere"), filmId, "Nowhere").Result;

      Assert.That(found, Is.EqualTo(0), "a share that is away must stop the import of one title, not of the run");
      Assert.That(_fanArtCache.Stored, Is.Empty);
    }

    /// <summary>
    /// The path of a folder of the mocked shelf, in the shape a resource locator hands it over.
    /// </summary>
    private static ResourcePath FolderPath(string folder)
    {
      return ResourcePath.BuildBaseProviderPath(MockResourceProvider.PROVIDER_ID, folder + "/");
    }

    /// <summary>
    /// The paths stored for the one title the tests write, of the kind asked for.
    /// </summary>
    private IEnumerable<string> Stored(string fanArtType)
    {
      return _fanArtCache.Stored.Values.SelectMany(entries => entries)
          .Where(entry => entry.Item1 == fanArtType)
          .Select(entry => entry.Item2);
    }

    /// <summary>
    /// The fan art cache of the fan art cache's own tests, with one thing added: it remembers which kind each
    /// picture was filed under, which is the whole question the tests above ask.
    /// </summary>
    class RecordingFanArtCache : IFanArtCache
    {
      /// <summary>Pictures by the title they belong to, each as its kind and its path.</summary>
      public readonly IDictionary<Guid, List<Tuple<string, string>>> Stored =
          new Dictionary<Guid, List<Tuple<string, string>>>();

      public IEnumerable<string> AllPaths()
      {
        return Stored.Values.SelectMany(entries => entries).Select(entry => entry.Item2);
      }

      public Task<bool> TrySaveFanArt(Guid mediaItemId, string title, string fanArtType,
          TrySaveFanArtAsyncDelegate saveDlgt)
      {
        throw new NotImplementedException();
      }

      public Task<int> TrySaveFanArt<T>(Guid mediaItemId, string title, string fanArtType, ICollection<T> files,
          TrySaveMultipleFanArtAsyncDelegate<T> saveDlgt)
      {
        if (!Stored.TryGetValue(mediaItemId, out List<Tuple<string, string>> entries))
          Stored[mediaItemId] = entries = new List<Tuple<string, string>>();
        foreach (T file in files ?? Array.Empty<T>())
          entries.Add(Tuple.Create(fanArtType, file.ToString()));
        return Task.FromResult(files?.Count ?? 0);
      }

      public IList<string> GetFanArtFiles(Guid mediaItemId, string fanArtType)
      {
        throw new NotImplementedException();
      }

      public ICollection<Guid> GetAllFanArtIds()
      {
        throw new NotImplementedException();
      }

      public void DeleteFanArtFiles(Guid mediaItemId)
      {
        throw new NotImplementedException();
      }
    }
  }
}
