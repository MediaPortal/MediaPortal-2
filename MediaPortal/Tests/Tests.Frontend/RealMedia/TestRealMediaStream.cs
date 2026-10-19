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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Player;
using MediaPortal.Server.Api;
using MediaPortal.Utilities.SystemAPI;
using Moq;
using NUnit.Framework;
using ServerMediaAspect = MediaPortal.Common.MediaManagement.DefaultItemAspects.MediaAspect;

namespace Tests.Frontend.RealMedia
{
  /// <summary>
  /// The same file that <see cref="TestRealMediaFile"/> plays from disk, played again through the server's HTTP
  /// stream endpoint - which is the only way anybody who is not the server ever sees a film.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Compiled under the same <c>-p:RealMediaTests=true</c> flag and with the same environment variables as the
  /// file fixture, so <c>scripts\ci\test-player-file.ps1 -Stream</c> measures both against one set of numbers
  /// from <c>ffprobe</c>. The file is opened by the server through MP2's own local-filesystem resource accessor
  /// rather than a test double, so what is proved here includes the part every other test in this repository
  /// mocks away: a real provider handing over a handle to a real file, on the same code path a share the
  /// importer found would take.
  /// </para>
  /// <para>
  /// What the file fixture cannot say and this one can: that the range answers are good enough for a decoder.
  /// A Matroska keeps its index (the cues) after the clusters, and a player has to fetch the tail of the file
  /// before it will show a single frame. A server that answers every request with the whole file from byte zero
  /// plays sound with no picture, which is exactly the failure a viewer describes as "the film is black". So the
  /// assertions below are about ranges and about frames, and the duration is asserted too because a player only
  /// knows the length of a streamed film after it has sought.
  /// </para>
  /// <para>
  /// One honesty note: the player is pointed at a server in this process, on a loopback port, reading a file on
  /// the same machine. It is not a network. What it does prove is everything past the socket - the endpoint, the
  /// range handling, the media type, and libVLC's HTTP access layer with seeking - and the part it cannot prove,
  /// that bytes survive a real network, is not this codebase's to prove.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestRealMediaStream
  {
    private const int HEAD_BYTES = 1 << 12;

    private string _path = "";
    private long _expectedLengthMs;
    private bool _expectsVideo;
    private int _expectedVideoWidth;
    private int _expectedVideoHeight;
    private int _watchMilliseconds;

    private InProcessApi _api = null!;
    private ServerApiClient _client = null!;
    private Guid _titleId;
    private LibVlcPlayer _player = null!;

    [OneTimeSetUp]
    public void FindTheFile()
    {
      _path = (Environment.GetEnvironmentVariable("MP2_TEST_MEDIA") ?? string.Empty).Trim().Trim('"');
      Assert.That(_path, Is.Not.Empty,
          "this fixture was compiled on purpose, so it needs MP2_TEST_MEDIA to point at a media file");
      Assert.That(File.Exists(_path), Is.True, "there is no file at MP2_TEST_MEDIA: " + _path);

      _expectedLengthMs = ReadNumber("MP2_TEST_MEDIA_LENGTH_MS", 0);
      _expectsVideo = Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_HAS_VIDEO") != "0";
      _watchMilliseconds = (int)ReadNumber("MP2_TEST_MEDIA_SECONDS", 6) * 1000;

      string video = (Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_VIDEO") ?? string.Empty).Trim();
      int separator = video.IndexOf('x');
      if (separator > 0)
      {
        int.TryParse(video.Substring(0, separator), out _expectedVideoWidth);
        int.TryParse(video.Substring(separator + 1), out _expectedVideoHeight);
      }

      TestContext.Progress.WriteLine("  media:    " + Path.GetFileName(_path));
      TestContext.Progress.WriteLine("  bytes:    " + new FileInfo(_path).Length.ToString("N0", CultureInfo.InvariantCulture));
      TestContext.Progress.WriteLine("  expected: " + DescribeExpectations());
    }

    [SetUp]
    public void ServeTheFile()
    {
      _titleId = Guid.NewGuid();
      InProcessApi.RegisterMediaLibrary(TheLibraryHolding(_titleId));
      _api = InProcessApi.Start();
      _client = new ServerApiClient(_api.BaseUrl);

      bool show = Environment.GetEnvironmentVariable("MP2_TEST_MEDIA_SHOW") == "1";
      if (!LibVlcPlayer.TryCreate(out LibVlcPlayer? player, out string reason,
                                  new PlayerOptions { Headless = !show }))
      {
        Assert.Ignore("needs libVLC, which this host cannot provide: " + reason);
      }
      _player = player!;
      TestContext.Progress.WriteLine("  engine:   " + _player.EngineDescription);
    }

    [TearDown]
    public void PutItAllAway()
    {
      _player?.Dispose();
      _player = null!;
      _client?.Dispose();
      _client = null!;
      _api?.Dispose();
      _api = null!;
      InProcessApi.RemoveMediaLibrary();
    }

    /// <summary>
    /// The two halves the front end depends on: the library names the file, and the address the client builds
    /// from the id really carries that file.
    /// </summary>
    [Test]
    public async Task TheTitlesAnswerAndTheStreamAnswerAreAboutTheSameFile()
    {
      MediaTitles titles = await _client.GetTitlesAsync(MovieAspect.Metadata.Name);
      Assert.That(titles.Error, Is.Null, titles.Error ?? "");
      Assert.That(titles.Titles, Has.Count.EqualTo(1), "the library holds one film and the server lists one");

      MediaTitle title = titles.Titles[0];
      Assert.That(title.Id, Is.EqualTo(_titleId));
      Assert.That(title.Path, Is.Not.Null.And.Not.Empty,
          "the server names the file it found; without that the front end has nothing to stream");
      Assert.That(title.Path, Does.Contain(Path.GetFileName(_path)),
          "and the name it says is the file on disk, serialized as the server's own resource path");

      Uri stream = _client.GetStreamUri(title.Id);
      TestContext.Progress.WriteLine("  stream:   " + stream);

      using (var http = new HttpClient())
      {
        using (HttpResponseMessage head = await http.SendAsync(new HttpRequestMessage(HttpMethod.Head, stream)))
        {
          Assert.That(head.StatusCode, Is.EqualTo(HttpStatusCode.OK),
              "a player asks how big the film is before it commits to it, and that has to be answered");
          long? announced = head.Content.Headers.ContentLength;
          Assert.That(announced, Is.EqualTo(new FileInfo(_path).Length),
              "the length a player is told is the length of the file, or its progress bar lies");
          Assert.That(head.Headers.AcceptRanges.Contains("bytes"), Is.True,
              "a server that does not advertise ranges is a server whose films cannot be seeked in");
        }

        using (HttpResponseMessage whole = await http.GetAsync(stream, HttpCompletionOption.ResponseHeadersRead))
        {
          Assert.That(whole.StatusCode, Is.EqualTo(HttpStatusCode.OK));
          string mediaType = whole.Content.Headers.ContentType?.MediaType ?? "";
          Assert.That(mediaType, Is.EqualTo(ExpectedMediaType()),
              "the type is the one MP2's own table gives this extension; a player that is told \"octet-stream\" "
              + "for an MKV is being told nobody knows what it is");

          byte[] first = await ReadAsync(whole, HEAD_BYTES);
          Assert.That(first, Is.EqualTo(HeadOfFile(HEAD_BYTES)),
              "the bytes that came out of the server are not the bytes of the file");
          TestContext.Progress.WriteLine("  bytes:    first " + HEAD_BYTES + " identical to the file, type "
              + mediaType);
        }
      }
    }

    /// <summary>
    /// The request a decoder makes before it shows anything: the end of the file, where a Matroska keeps its
    /// index. Without a correct answer here the viewer hears the film and sees nothing.
    /// </summary>
    [Test]
    public async Task TheEndOfTheFileIsAnsweredByteForByte()
    {
      long size = new FileInfo(_path).Length;
      long from = size - HEAD_BYTES;

      using (var http = new HttpClient())
      using (var request = new HttpRequestMessage(HttpMethod.Get, _client.GetStreamUri(_titleId)))
      {
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(from, size - 1);
        using (HttpResponseMessage answer = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
        {
          Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.PartialContent),
              "a range request answered with 200 hands the player the whole film from the start, which is how a "
              + "seek ends up back at the beginning");

          byte[] tail = await ReadAsync(answer, HEAD_BYTES);
          Assert.That(tail, Is.EqualTo(TailOfFile(HEAD_BYTES)),
              "the last bytes of the stream are not the last bytes of the file");

          string range = answer.Content.Headers.ContentRange?.ToString() ?? "";
          Assert.That(range, Is.EqualTo("bytes " + from.ToString(CultureInfo.InvariantCulture) + "-"
              + (size - 1).ToString(CultureInfo.InvariantCulture) + "/" + size.ToString(CultureInfo.InvariantCulture)),
              "the answer says it carries " + range + ", and a player that believes a wrong Content-Range reads "
              + "past what it was given");
          TestContext.Progress.WriteLine("  range:    " + range + " answered byte for byte");
        }
      }
    }

    [Test]
    public void TheFilmPlaysFromTheServersAddress()
    {
      OpenTheStream();

      Assert.That(_player.LengthInMs, Is.GreaterThan(0),
          "a streamed film whose length is unknown cannot be resumed, and its progress bar cannot be drawn");

      if (_expectedLengthMs > 0)
      {
        long difference = Math.Abs(_player.LengthInMs - _expectedLengthMs);
        Assert.That(difference, Is.LessThan(1500),
            "the container says " + Format(_expectedLengthMs) + " and the player got " + Format(_player.LengthInMs)
            + " out of the HTTP stream; a duration only comes out of a stream whose ranges are right, so this is "
            + "the range test that a decoder performs by itself");
      }

      TestContext.Progress.WriteLine("  length:   " + Format(_player.LengthInMs) + " over HTTP");
    }

    /// <summary>
    /// The frame counter over a stream. A file that plays from disk and a file that plays from a URL reach the
    /// same sink by different routes through libVLC, and only the second one goes through the HTTP access module
    /// and through this server's answers.
    /// </summary>
    [Test]
    public void ThePictureArrivesFromAnHttpStream()
    {
      Assert.That(_expectsVideo, Is.True, "the gate says this file holds no video");

      Assert.That(LibVlcPlayer.TryCreate(out LibVlcPlayer? painter, out string reason,
                                         new PlayerOptions { Headless = false }),
                  Is.True, "no second engine to ask for pictures: " + reason);

      using (painter)
      using (VideoFrameSink frames = new VideoFrameSink())
      {
        Assert.That(painter!.AttachTo(frames), Is.True,
            painter.LastFailure ?? "the player refused to hand its pictures over, and said nothing about why");

        Uri stream = _client.GetStreamUri(_titleId);
        Assert.That(painter.Open(MediaLocation.FromUrl(stream)), Is.True,
            painter.LastFailure ?? "libVLC would not open the server's stream address at all");
        painter.Play();

        Assert.That(Wait(delegate { return frames.FramesArrived > 0; }, 30_000), Is.True,
            "the stream played for thirty seconds without one picture reaching the sink. That is the black screen "
            + "with sound, and the most likely reason is that the server's range answers are wrong");
        Thread.Sleep(_watchMilliseconds);

        PlayerStatistics counted = painter.Statistics;
        TestContext.Progress.WriteLine("  pictures: " + frames.FramesArrived + " of " + frames.PictureWidth
            + "x" + frames.PictureHeight + ", decoded " + counted.DecodedVideoFrames + " audio "
            + counted.DecodedAudioFrames);

        Assert.That(frames.PictureWidth, Is.GreaterThan(0));
        Assert.That(frames.PictureHeight, Is.GreaterThan(0));
        Assert.That(counted.DecodedAudioFrames, Is.GreaterThan(0),
            "an http stream that decodes no audio is not playing the film, whatever it says about its state");

        if (_expectedVideoWidth > 0 && _expectedVideoHeight > 0)
        {
          Assert.That(frames.PictureWidth, Is.EqualTo(_expectedVideoWidth),
              "the streamed picture is " + frames.PictureWidth + " wide against a container that says "
              + _expectedVideoWidth);
          Assert.That(frames.PictureHeight, Is.EqualTo(_expectedVideoHeight),
              "the streamed picture is " + frames.PictureHeight + " lines high against a container that says "
              + _expectedVideoHeight + ", so the block-rounded lines are being drawn");
        }
      }
    }

    /// <summary>
    /// Seeking in a streamed film - the thing a viewer does ten times a film and the thing a server without
    /// working ranges cannot do at all.
    /// </summary>
    [Test]
    public void SeekingIntoAStreamedFilmLandsThere()
    {
      OpenTheStream();

      long target = _player.LengthInMs / 3;
      _player.TimeInMs = target;

      long tolerance = Math.Min(120_000, Math.Max(20_000, _player.LengthInMs / 20));
      Assert.That(Wait(delegate { return Math.Abs(_player.TimeInMs - target) <= tolerance; }, 30_000), Is.True,
          "asked for " + Format(target) + " of the stream and is at " + Format(_player.TimeInMs) + " after 30 s");
      Assert.That(_player.TimeInMs, Is.GreaterThan(_player.LengthInMs / 10),
          "the position barely moved from the start: the server answered the seek with the beginning of the file");

      Assert.That(Wait(delegate { return _player.State == PlayerState.Playing; }, 15_000), Is.True,
          "the stream stopped answering after the seek");
      TestContext.Progress.WriteLine("  seek:     asked " + Format(target) + ", arrived at "
          + Format(_player.TimeInMs) + " over HTTP");
    }

    // ---- helpers -------------------------------------------------------------------------------------

    /// <summary>
    /// The address the front end would hand a player, opened and played. Everything below the endpoint is the
    /// product's own; nothing here reaches past the HTTP client.
    /// </summary>
    private void OpenTheStream()
    {
      Uri stream = _client.GetStreamUri(_titleId);
      Assert.That(_player.Open(MediaLocation.FromUrl(stream)), Is.True,
          _player.LastFailure ?? "libVLC refused the server's address: " + stream);
      WaitFor(delegate { return _player.State == PlayerState.Playing; }, 30_000,
          "the streamed film never started playing");
      WaitFor(delegate { return _player.LengthInMs > 0; }, 20_000,
          "the film streams and its length is still unknown, which means it could not seek to read its index");
    }

    /// <summary>
    /// The one film in the library, reached the way a title imported from a share is reached: MP2's local
    /// filesystem resource accessor on the file's own path.
    /// </summary>
    private static IMediaLibrary TheLibraryHolding(Guid id)
    {
      var library = new Mock<IMediaLibrary>(MockBehavior.Strict);
      library.Setup(get => get.GetManagedMediaItemAspectMetadata()).Returns(new Dictionary<Guid, MediaItemAspectMetadata>
      {
          { ServerMediaAspect.ASPECT_ID, ServerMediaAspect.Metadata },
          { MovieAspect.ASPECT_ID, MovieAspect.Metadata }
      });

      library.Setup(get => get.Search(It.IsAny<MediaItemQuery>(), It.IsAny<bool>(), It.IsAny<Guid?>(),
                                     It.IsAny<bool>()))
          .Returns(new List<MediaItem> { new StreamedFilm(id, Environment.GetEnvironmentVariable("MP2_TEST_MEDIA")!) });
      library.Setup(get => get.CountMediaItems(It.IsAny<IEnumerable<Guid>>(), It.IsAny<IFilter>(), It.IsAny<bool>(),
                                               It.IsAny<bool>()))
          .Returns(1);
      return library.Object;
    }

    private string ExpectedMediaType()
    {
      // MP2's own table, the same one the endpoint reads: what the server sends has to be what that table says
      // about this file, and when the table is silent the answer has to be the honest fallback rather than a
      // guess at MP4.
      return MimeTypeDetector.GetMimeTypeFromExtension(_path) ?? MediaController.DEFAULT_STREAM_MEDIA_TYPE;
    }

    private static async Task<byte[]> ReadAsync(HttpResponseMessage answer, int maximum)
    {
      using (Stream body = await answer.Content.ReadAsStreamAsync())
      {
        byte[] buffer = new byte[maximum];
        int total = 0;
        while (total < maximum)
        {
          int read = await body.ReadAsync(buffer.AsMemory(total, maximum - total));
          if (read <= 0)
            break;
          total += read;
        }
        return total == maximum ? buffer : buffer.Take(total).ToArray();
      }
    }

    private byte[] HeadOfFile(int count)
    {
      using (FileStream file = File.OpenRead(_path))
      {
        byte[] buffer = new byte[count];
        int read = 0;
        while (read < count)
        {
          int got = file.Read(buffer, read, count - read);
          if (got <= 0)
            break;
          read += got;
        }
        return read == count ? buffer : buffer.Take(read).ToArray();
      }
    }

    private byte[] TailOfFile(int count)
    {
      long size = new FileInfo(_path).Length;
      long from = Math.Max(0, size - count);
      using (FileStream file = File.OpenRead(_path))
      {
        file.Seek(from, SeekOrigin.Begin);
        byte[] buffer = new byte[(int)(size - from)];
        int read = 0;
        while (read < buffer.Length)
        {
          int got = file.Read(buffer, read, buffer.Length - read);
          if (got <= 0)
            break;
          read += got;
        }
        return read == buffer.Length ? buffer : buffer.Take(read).ToArray();
      }
    }

    private void WaitFor(Func<bool> condition, int milliseconds, string because)
    {
      if (Wait(condition, milliseconds))
        return;
      Assert.That(condition(), Is.True,
          because + $" (state {_player.State}, failure: {_player.LastFailure ?? "none"})");
    }

    private static bool Wait(Func<bool> condition, int milliseconds)
    {
      Stopwatch watch = Stopwatch.StartNew();
      while (watch.ElapsedMilliseconds < milliseconds)
      {
        if (condition())
          return true;
        Thread.Sleep(50);
      }
      return false;
    }

    private static long ReadNumber(string variable, long whenAbsent)
    {
      string? raw = Environment.GetEnvironmentVariable(variable);
      return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
          ? parsed
          : whenAbsent;
    }

    private string DescribeExpectations()
    {
      return (_expectedLengthMs > 0 ? Format(_expectedLengthMs) : "length unstated") + ", "
             + (_expectsVideo ? "video expected" : "no video track")
             + (_expectedVideoWidth > 0 ? " at " + _expectedVideoWidth + "x" + _expectedVideoHeight : "")
             + ", watching " + (_watchMilliseconds / 1000) + " s";
    }

    private static string Format(long milliseconds)
    {
      return TimeSpan.FromMilliseconds(milliseconds).ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// A title whose file is on this machine and is opened through the provider the server would really use.
    /// </summary>
    private class StreamedFilm : MediaItem
    {
      private readonly string _dosPath;

      public StreamedFilm(Guid id, string dosPath)
          : base(id, DescribeTheFilm())
      {
        _dosPath = dosPath;
      }

      private static IDictionary<Guid, IList<MediaItemAspect>> DescribeTheFilm()
      {
        SingleMediaItemAspect media = new SingleMediaItemAspect(ServerMediaAspect.Metadata);
        media.SetAttribute(ServerMediaAspect.ATTR_TITLE, "Streamed film");
        media.SetAttribute(ServerMediaAspect.ATTR_SORT_TITLE, "Streamed film");
        return new Dictionary<Guid, IList<MediaItemAspect>>
        {
            { ServerMediaAspect.ASPECT_ID, new List<MediaItemAspect> { media } }
        };
      }

      public override IResourceLocator GetResourceLocator()
      {
        return new FilmOnDisk(_dosPath);
      }
    }

    /// <summary>
    /// A locator that hands out MP2's real local-filesystem accessor for one file, so that the endpoint is
    /// proved against the accessor production uses instead of one a test wrote.
    /// </summary>
    private class FilmOnDisk : IResourceLocator
    {
      private static readonly LocalFsResourceProvider PROVIDER = new LocalFsResourceProvider();

      private readonly string _providerPath;

      public FilmOnDisk(string dosPath)
      {
        _providerPath = LocalFsResourceProviderBase.ToProviderPath(dosPath);
      }

      public string NativeSystemId
      {
        get { return Guid.Empty.ToString("B"); }
      }

      public ResourcePath NativeResourcePath
      {
        get
        {
          return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
              _providerPath);
        }
      }

      public IResourceAccessor CreateAccessor()
      {
        return new LocalFsResourceAccessor(PROVIDER, _providerPath);
      }

      public bool TryCreateLocalFsAccessor(out ILocalFsResourceAccessor localFsResourceAccessor)
      {
        localFsResourceAccessor = new LocalFsResourceAccessor(PROVIDER, _providerPath);
        return true;
      }
    }
  }
}
