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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using MediaPortal.Mock;
using Moq;
using NUnit.Framework;

namespace Tests.Server.NfoMetadataExtractor
{
  /// <summary>
  /// Tests for the picture a title is drawn with: the one its nfo-file names, the one lying next to it under the name
  /// the tools that write nfo-files use, and the absence of both.
  /// </summary>
  /// <remarks>
  /// <para>
  /// A library whose titles have no pictures is a shelf of grey rectangles, and for years the picture a title had was
  /// read out of the nfo-file and then thrown away: the writers that store it were commented out in favour of a FanArt
  /// collector that no server built on nfo-files runs, and the addresses those files carry moved from http to https,
  /// which the reader did not accept. Both are pinned down here, together with the rule that a picture lying in the
  /// folder is used when the nfo-file names none - the rule that makes a server without internet access draw its
  /// titles at all.
  /// </para>
  /// <para>
  /// The tests read real files from real temporary directories. Nothing here reaches the internet: the addresses are
  /// only judged, never fetched, so a suite run on a machine behind a firewall says the same thing as one run on a
  /// laptop on a train.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestNfoPictures
  {
    /// <summary>
    /// The first bytes of a JPEG. The readers do not decode pictures, but a picture that does not begin like a
    /// picture is a picture no front end will draw, so the fixtures are not allowed to be plain text.
    /// </summary>
    private static readonly byte[] JPEG = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };

    private string _folder;
    private ILogger _logger;

    [OneTimeSetUp]
    public void SetUpCore()
    {
      // The resource accessors used below are built by the local filesystem resource provider, which the media
      // accessor registers - so the core services have to be up, as they are on a running server. The database comes
      // first because MockCore's aspect management reads it while it is being constructed.
      MockDBUtils.Reset();
      MockCore.Reset();
      // Registers the local filesystem resource provider, which every accessor below and the directory lookup inside
      // the readers go through.
      ServiceRegistration.Get<IMediaAccessor>().Initialize();
      if (!ServiceRegistration.IsRegistered<ILocalization>())
        ServiceRegistration.Set<ILocalization>(new NoLocalization());
    }

    [SetUp]
    public void SetUp()
    {
      _folder = Directory.CreateDirectory(
          Path.Combine(Path.GetTempPath(), "mp2-nfo-pictures-" + Guid.NewGuid().ToString("N"))).FullName;
      _logger = new Mock<ILogger>().Object;

      // The readers cache stub objects by the nfo-file's path for five minutes. The folders are unique per test, but
      // a reader's cache outlives the temporary folder it was filled from, so it is emptied between tests.
      NfoSeriesEpisodeReader.ClearCache();
      NfoSeriesReader.ClearCache();
      NfoMovieReader.ClearCache();
    }

    [TearDown]
    public void TearDown()
    {
      try { Directory.Delete(_folder, true); }
      catch (IOException) { }
    }

    #region Fixtures

    private string WriteFile(string name, byte[] content)
    {
      string path = Path.Combine(_folder, name);
      string directory = Path.GetDirectoryName(path);
      if (directory != null)
        Directory.CreateDirectory(directory);
      File.WriteAllBytes(path, content);
      return path;
    }

    /// <summary>
    /// Writes a picture whose bytes are <paramref name="tag"/> repeated, so that a test can tell from the bytes alone
    /// which file on disk the reader ended up storing.
    /// </summary>
    private string WritePicture(string name, byte tag)
    {
      var content = new List<byte>(JPEG);
      for (int i = 0; i < 20; i++)
        content.Add(tag);
      return WriteFile(name, content.ToArray());
    }

    private string WriteNfo(string name, string rootElement, params string[] elements)
    {
      var text = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<" + rootElement + ">\n";
      foreach (var element in elements)
        text += "  " + element + "\n";
      text += "</" + rootElement + ">\n";
      return WriteFile(name, System.Text.Encoding.UTF8.GetBytes(text));
    }

    private static IFileSystemResourceAccessor AccessorFor(string path)
    {
      return new LocalFsResourceAccessor(new LocalFsResourceProvider(),
          LocalFsResourceProviderBase.ToProviderPath(path));
    }

    private IDictionary<Guid, IList<MediaItemAspect>> ReadEpisode(string nfoPath, bool forceQuickMode = false)
    {
      return Read(new NfoSeriesEpisodeReader(_logger, 1, forceQuickMode, false, null,
          new NfoSeriesMetadataExtractorSettings(), false), nfoPath);
    }

    private IDictionary<Guid, IList<MediaItemAspect>> ReadSeries(string nfoPath, bool forceQuickMode = false)
    {
      return Read(new NfoSeriesReader(_logger, 1, forceQuickMode, false, false, null,
          new NfoSeriesMetadataExtractorSettings(), false), nfoPath);
    }

    private IDictionary<Guid, IList<MediaItemAspect>> ReadMovie(string nfoPath, bool forceQuickMode = false)
    {
      return Read(new NfoMovieReader(_logger, 1, false, forceQuickMode, false, null,
          new NfoMovieMetadataExtractorSettings(), false), nfoPath);
    }

    /// <summary>
    /// Reads an nfo-file and writes what the reader found into aspects, the way an import does.
    /// </summary>
    private static IDictionary<Guid, IList<MediaItemAspect>> Read<TStub>(NfoReaderBase<TStub> reader, string nfoPath)
        where TStub : new()
    {
      var aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      using (var accessor = AccessorFor(nfoPath))
      {
        if (!reader.TryReadMetadataAsync(accessor).Result)
          Assert.Fail("the nfo-file " + Path.GetFileName(nfoPath) + " was not read at all");

        Assert.IsTrue(reader.TryWriteMetadata(aspects),
            "the reader wrote nothing at all, so the nfo-file was not understood");
      }
      return aspects;
    }

    /// <summary>
    /// The picture the reader stored, or <c>null</c> when the title was left without one.
    /// </summary>
    private static byte[] PictureOf(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<MediaItemAspect> entries;
      if (!aspects.TryGetValue(ThumbnailLargeAspect.ASPECT_ID, out entries) || entries == null || entries.Count == 0)
        return null;
      return entries[0].GetAttributeValue<byte[]>(ThumbnailLargeAspect.ATTR_THUMBNAIL);
    }

    private static void AssertPictureIs(byte[] picture, string file)
    {
      Assert.IsNotNull(picture, "the title was left without a picture");
      Assert.That(picture, Is.EqualTo(File.ReadAllBytes(file)),
          "the stored bytes are not the bytes of " + Path.GetFileName(file));
    }

    #endregion

    #region The picture the nfo-file names

    [Test]
    public void EpisodePictureNamedByItsNfoIsStored()
    {
      var still = WritePicture("Still.jpg", 0x11);
      var nfo = WriteNfo("Show - S01E01 - Beobachtung.nfo", "episodedetails",
          "<title>Beobachtung</title>", "<season>1</season>", "<episode>1</episode>",
          "<thumb>" + Path.GetFileName(still) + "</thumb>");

      AssertPictureIs(PictureOf(ReadEpisode(nfo)), still);
    }

    [Test]
    public void MovieCoverNamedByItsNfoIsStored()
    {
      var cover = WritePicture("cover.jpg", 0x22);
      var nfo = WriteNfo("movie.nfo", "movie", "<title>Alien</title>", "<year>1979</year>",
          "<thumb>" + Path.GetFileName(cover) + "</thumb>");

      AssertPictureIs(PictureOf(ReadMovie(nfo)), cover);
    }

    [Test]
    public void SeriesPosterNamedByItsNfoIsStoredOverTheFanartPictures()
    {
      // A tvshow.nfo names a dozen pictures and the reader collects them in a set, so "the first one" is not an answer.
      // The picture a series is drawn with is the poster of the whole series, whatever order the others came in.
      WritePicture("fanart1.jpg", 0x31);
      WritePicture("fanart2.jpg", 0x32);
      var seasonPoster = WritePicture("season1.jpg", 0x33);
      var poster = WritePicture("series-poster.jpg", 0x34);

      var nfo = WriteNfo("tvshow.nfo", "tvshow", "<title>Alien: Earth</title>",
          "<thumb>" + "fanart1.jpg" + "</thumb>",
          "<thumb>" + "fanart2.jpg" + "</thumb>",
          "<thumb aspect=\"poster\" season=\"1\" type=\"season\">" + Path.GetFileName(seasonPoster) + "</thumb>",
          "<thumb aspect=\"poster\">" + Path.GetFileName(poster) + "</thumb>");

      AssertPictureIs(PictureOf(ReadSeries(nfo)), poster);
    }

    [Test]
    public void AnEmptyPictureFileIsNotAPicture()
    {
      var empty = WriteFile("Still.jpg", new byte[0]);
      var nfo = WriteNfo("Show - S01E02.nfo", "episodedetails",
          "<title>Mr. October</title>", "<season>1</season>", "<episode>2</episode>",
          "<thumb>" + Path.GetFileName(empty) + "</thumb>");

      Assert.IsNull(PictureOf(ReadEpisode(nfo)),
          "zero bytes were stored as a picture, which a front end cannot draw and should have been a placeholder");
    }

    #endregion

    #region The picture lying next to the media

    [Test]
    public void EpisodePictureLyingNextToTheNfoIsUsedWhenTheNfoNamesNone()
    {
      // This is what Kodi and MediaElch write when they are told to keep artwork with the media: the picture carries
      // the name of the episode, because a season folder holds more than one title.
      var still = WritePicture("Show - S01E04 - Beobachtung-thumb.jpg", 0x41);
      WritePicture("poster.jpg", 0x42);
      var nfo = WriteNfo("Show - S01E04 - Beobachtung.nfo", "episodedetails",
          "<title>Beobachtung</title>", "<season>1</season>", "<episode>4</episode>");

      AssertPictureIs(PictureOf(ReadEpisode(nfo)), still);
    }

    [Test]
    public void EpisodeDoesNotTakeTheSeriesPosterHangingInItsFolder()
    {
      WritePicture("poster.jpg", 0x43);
      var nfo = WriteNfo("Show - S01E05 - Im Weltraum.nfo", "episodedetails",
          "<title>Im Weltraum</title>", "<season>1</season>", "<episode>5</episode>");

      Assert.IsNull(PictureOf(ReadEpisode(nfo)),
          "an episode was given the series poster, so every episode of the series would be drawn alike");
    }

    [Test]
    public void MovieFolderPosterIsUsedWhenTheMovieNfoNamesNone()
    {
      var poster = WritePicture("poster.jpg", 0x51);
      var nfo = WriteNfo("movie.nfo", "movie", "<title>Arrival</title>", "<year>2016</year>");

      AssertPictureIs(PictureOf(ReadMovie(nfo)), poster);
    }

    [Test]
    public void SeriesPosterIsUsedWhenTheSeriesNfoNamesNone()
    {
      var poster = WritePicture("poster.jpg", 0x61);
      var nfo = WriteNfo("tvshow.nfo", "tvshow", "<title>Alien: Earth</title>");

      AssertPictureIs(PictureOf(ReadSeries(nfo)), poster);
    }

    [Test]
    public void ThePictureTheNfoNamesWinsOverTheOneLyingAround()
    {
      var named = WritePicture("named.jpg", 0x71);
      WritePicture("Show - S01E06 - Die Fliege-thumb.jpg", 0x72);
      var nfo = WriteNfo("Show - S01E06 - Die Fliege.nfo", "episodedetails",
          "<title>Die Fliege</title>", "<season>1</season>", "<episode>6</episode>",
          "<thumb>" + Path.GetFileName(named) + "</thumb>");

      AssertPictureIs(PictureOf(ReadEpisode(nfo)), named);
    }

    [Test]
    public void ATitleWithoutAnyPictureIsLeftWithoutOne()
    {
      var nfo = WriteNfo("Show - S01E07 - Emergenz.nfo", "episodedetails",
          "<title>Emergenz</title>", "<season>1</season>", "<episode>7</episode>");

      Assert.IsNull(PictureOf(ReadEpisode(nfo)),
          "a title with no picture anywhere was given one; the placeholder a front end draws for nothing is the " +
          "answer, not a guess at what the folder might have meant");
    }

    [Test]
    public void QuickModeDoesNotReadPictures()
    {
      WritePicture("Show - S01E03 - Metamorphosis-thumb.jpg", 0x91);
      var nfo = WriteNfo("Show - S01E03 - Metamorphosis.nfo", "episodedetails",
          "<title>Metamorphosis</title>", "<season>1</season>", "<episode>3</episode>");

      Assert.IsNull(PictureOf(ReadEpisode(nfo, true)),
          "a quick import, which promises not to do anything slow, read picture files");
    }

    #endregion

    #region The addresses an nfo-file hands out

    [Test]
    public void PictureAddressesAreAcceptedForHttpAndHttps()
    {
      Uri uri;
      Assert.IsTrue(NfoSeriesEpisodeReader.IsPictureUrl(
          "http://image.tmdb.org/t/p/w1280/hzE6VJ37veL4p54IicLcI5pDDSJ.jpg", out uri));
      Assert.AreEqual("http", uri.Scheme);

      // The databases that write these files serve https only. When only http was accepted, every library built from
      // their nfo-files lost all of its artwork without one error message.
      Assert.IsTrue(NfoSeriesEpisodeReader.IsPictureUrl(
          "https://image.tmdb.org/t/p/w1280/hzE6VJ37veL4p54IicLcI5pDDSJ.jpg", out uri));
      Assert.AreEqual("https", uri.Scheme);
    }

    [Test]
    public void PictureAddressesAreRefusedForEverythingElse()
    {
      Uri uri;
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl("ftp://nas/poster.jpg", out uri));
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl("file:///C:/Windows/win.ini", out uri),
          "an nfo-file downloaded from the internet must not be able to make the importer read any path on the server");
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl("poster.jpg", out uri));
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl(@"extrafanart\fanart1.jpg", out uri));
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl(string.Empty, out uri));
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl("   ", out uri));
      Assert.IsFalse(NfoSeriesEpisodeReader.IsPictureUrl(null, out uri));
    }

    #endregion
  }
}
