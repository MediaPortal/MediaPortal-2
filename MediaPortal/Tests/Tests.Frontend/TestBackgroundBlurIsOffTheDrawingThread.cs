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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Headless.NUnit;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using NUnit.Framework;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Frontend.Api;
using MediaPortal.Frontend.Settings;
using MediaPortal.Frontend.Theming;
using MediaPortal.Frontend.ViewModels;
using SkiaSharp;

namespace Tests.Frontend
{
  /// <summary>
  /// Throwing a whole screen of fanart out of focus is arithmetic - about a sixth of a second on the machine
  /// this was measured on - and a sixth of a second is a fifth of a step between menu entries. So the
  /// arithmetic is done away from the thread that draws, and its result is remembered. These tests hold the
  /// three things that make that worth anything: the work is not done by the thread that asked for it, a
  /// backdrop asked for twice is blurred once, and the screen ends up wearing the blurred copy while saying
  /// nought to the converter, so nothing is blurred a second time while the television is watching.
  /// </summary>
  [TestFixture]
  public class TestBackgroundBlurIsOffTheDrawingThread
  {
    private string _previousDir = "";
    private string _ownDir = "";
    private string _previousScreensFile = "";

    [SetUp]
    public void IsolateTheSkinSettingsFolder()
    {
      _previousDir = Environment.GetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR") ?? "";
      _ownDir = Path.Combine(Path.GetTempPath(), "mp2-blur-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(_ownDir);
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _ownDir);

      _previousScreensFile = Environment.GetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE") ?? "";
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", Path.Combine(_ownDir, "screens.json"));

      BackgroundBlur.Forget();
    }

    [TearDown]
    public void PutTheMachineBack()
    {
      Environment.SetEnvironmentVariable("MP2_SKIN_SETTINGS_DIR", _previousDir);
      Environment.SetEnvironmentVariable("MP2_SCREEN_VIEWS_FILE", _previousScreensFile);
      BackgroundBlur.RoomBytes = BackgroundBlur.DEFAULT_ROOM_BYTES;
      BackgroundBlur.Forget();

      try
      {
        Directory.Delete(_ownDir, true);
      }
      catch (IOException)
      {
      }
    }

    /// <summary>
    /// A backdrop with something in it to blur: noise, which is the worst case for a blur and for an encoder,
    /// at a quarter of a high-definition screen. Small enough to blur inside a test, big enough that the work
    /// is work - a tenth of the pixels a full-HD fanart has, and a tenth of the delay a viewer would feel.
    /// </summary>
    private static byte[] Backdrop(int width = 480, int height = 270)
    {
      using SKBitmap noise = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
      byte[] pixels = new byte[noise.ByteCount];
      new Random(20260729).NextBytes(pixels);
      Marshal.Copy(pixels, 0, noise.GetPixels(), pixels.Length);

      using SKImage image = SKImage.FromBitmap(noise);
      using SKData data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
      return data.ToArray();
    }

    /// <summary>
    /// How wide a picture the bytes hold, read off the PNG's own header. The headless toolkit answers every
    /// picture with a one-pixel stub, so the bytes have to speak for themselves - which they may, since a
    /// blurred copy is an encoded PNG and its size is the point.
    /// </summary>
    private static int PngWidth(byte[]? bytes)
    {
      Assert.That(bytes, Is.Not.Null);
      Assert.That(bytes.Length, Is.GreaterThan(24));
      Assert.That(bytes[0], Is.EqualTo(0x89), "the blur answers with a PNG and these are not one");
      Assert.That(bytes[16], Is.EqualTo(0));
      Assert.That(bytes[17], Is.EqualTo(0));

      return (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
    }

    /// <summary>As <see cref="PngWidth"/>, for the height standing next to it in the header.</summary>
    private static int PngHeight(byte[]? bytes)
    {
      Assert.That(bytes, Is.Not.Null);
      return (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
    }

    [Test]
    public async Task TheCopyIsNotMadeByTheThreadThatAskedForIt()
    {
      byte[] backdrop = Backdrop(960, 540);

      Task<byte[]?> working = BackgroundBlur.BlurredAsync(backdrop, 5);

      // A copy that stood ready before the question was finished being asked would mean the asker made it -
      // which is exactly the thread that has to keep drawing.
      Assert.False(working.IsCompleted, "the blur answered on the thread that asked for it");

      byte[]? blurred = await working;
      Assert.That(blurred, Is.Not.Null);

      Assert.That(PngWidth(blurred), Is.EqualTo(320),
          "a blur is paid for by the size it is made at, so it is made small");
      Assert.That(PngHeight(blurred), Is.EqualTo(180));
    }

    [Test]
    public async Task AFaceThatSpeaksNoBlurPaysNothing()
    {
      // Every skin but Nereus, and Nereus with the dial at nought: no arithmetic at all, and no resized copy
      // handed over either - those screens wear the picture exactly as the server drew it.
      Assert.That(await BackgroundBlur.BlurredAsync(Backdrop(), 0), Is.Null);
      Assert.That(await BackgroundBlur.BlurredAsync(Backdrop(), -1), Is.Null);
    }

    [Test]
    public async Task ABackdropAskedForTwiceIsBlurredOnce()
    {
      byte[] backdrop = Backdrop();
      byte[]? first = await BackgroundBlur.BlurredAsync(backdrop, 3);
      Assert.That(first, Is.Not.Null);

      Task<byte[]?> second = BackgroundBlur.BlurredAsync(backdrop, 3);

      // Walking back over a wall has to be as quick as walking over it the first time, so the remembered copy
      // is there before the walk is even finished - no second blur, and no waiting for one.
      Assert.True(second.IsCompleted, "the copy of a backdrop already blurred was not ready to hand");
      Assert.That(await second, Is.SameAs(first));
    }

    [Test]
    public async Task AnotherTurnOfTheDialIsAnotherCopy()
    {
      byte[] backdrop = Backdrop();
      byte[]? soft = await BackgroundBlur.BlurredAsync(backdrop, 1);
      byte[]? softer = await BackgroundBlur.BlurredAsync(backdrop, 5);

      Assert.That(soft, Is.Not.Null);
      Assert.That(softer, Is.Not.Null);
      Assert.That(softer, Is.Not.SameAs(soft), "turning the dial answered with the copy it did not ask for");
    }

    [Test]
    public async Task TheShelfOfCopiesIsPaidForInBytes()
    {
      // A machine that kept every copy of every backdrop it ever threw out of focus would fill up on a big
      // library, which is the same mistake the picture shelf made with its entries. So the copies are counted
      // in what they weigh, and an old one makes room for a new one.
      byte[] first = Backdrop(360, 202);
      byte[] next = Backdrop(384, 216);
      BackgroundBlur.RoomBytes = 1;

      byte[]? made = await BackgroundBlur.BlurredAsync(first, 5);
      Assert.That(made, Is.Not.Null);
      Assert.That(BackgroundBlur.RoomTaken, Is.GreaterThan(1), "a kept copy did not say how big it is");

      // The one copy the shelf is allowed to hold is the one the viewer is looking at, and it is handed out
      // again without arithmetic while it stands there.
      Assert.That(await BackgroundBlur.BlurredAsync(first, 5), Is.SameAs(made));

      byte[]? other = await BackgroundBlur.BlurredAsync(next, 5);
      Assert.That(other, Is.Not.Null);
      Assert.That(BackgroundBlur.RoomTaken, Is.EqualTo(other!.Length),
          "the shelf held on to a backdrop the viewer walked away from");

      Assert.That(await BackgroundBlur.BlurredAsync(first, 5), Is.Not.SameAs(made),
          "the copy that was promised to make room was still there");
    }

    [Test]
    public async Task BytesThatHoldNoPictureAreAnsweredWithSilence()
    {
      Assert.That(await BackgroundBlur.BlurredAsync(new byte[] { 1, 2, 3, 4 }, 5), Is.Null,
          "bytes that are no picture are no background, and the placeholder behind it is the answer");
    }

    // ---------------------------------------------------------------------------------------------
    // The screen wearing the copy.
    // ---------------------------------------------------------------------------------------------

    private static Application RequireApplication()
    {
      Application? app = Application.Current;
      Assert.That(app, Is.Not.Null, "the shelf needs the application to know what skin it wears");
      return app!;
    }

    private static HomeSection MoviesSection()
    {
      return new HomeSection("Movies",
          new AspectViewModel(new MediaPortal.Frontend.Api.MediaAspect(MovieAspect.ASPECT_ID,
              MovieAspect.Metadata.Name, false, false, null, Array.Empty<AspectAttribute>())));
    }

    private static void WaitUntil(string because, Func<bool> condition)
    {
      DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(15);
      while (DateTimeOffset.UtcNow < deadline)
      {
        Dispatcher.UIThread.RunJobs();
        if (condition())
          return;
        Thread.Sleep(5);
      }

      Assert.Fail("waited for " + because + " and it never came");
    }

    /// <summary>A server that hands one title's backdrop out as a real picture and everything else as a 404.</summary>
    private sealed class OneBackdropServer : HttpMessageHandler
    {
      private readonly Guid _title;
      private readonly byte[] _backdrop;

      public OneBackdropServer(Guid title, byte[] backdrop)
      {
        _title = title;
        _backdrop = backdrop;
      }

      protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
          CancellationToken cancellation)
      {
        string url = request.RequestUri!.ToString();
        bool asked = url.EndsWith("/fanart/FanArt", StringComparison.OrdinalIgnoreCase);
        bool known = url.Contains(_title.ToString("D"), StringComparison.OrdinalIgnoreCase);
        if (asked && known)
          return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
          {
            Content = new ByteArrayContent(_backdrop)
          });

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
      }
    }

    [AvaloniaTest]
    public void TheShelfWearsTheBlurredCopyAndSaysNought()
    {
      Application app = RequireApplication();

      string file = Path.Combine(_ownDir, "skin-nereus.json");
      NereusSkinSettings settings = NereusSkinSettings.Load(file, out _);
      settings.EnableFanart = true;
      settings.FanartBlur = 2;
      settings.Save(file, out _);
      FrontendTheme.Apply(app, "nereus", out _);

      byte[] backdrop = Backdrop();
      Guid title = Guid.NewGuid();
      ServerApiClient client = new ServerApiClient(
          new Uri("http://127.0.0.1:1/MediaPortal/Server/"),
          new HttpClient(new OneBackdropServer(title, backdrop)));

      MediaBrowserViewModel vm = new MediaBrowserViewModel(client, MoviesSection(), null);
      Assert.That(vm.FanartBlurRadius, Is.EqualTo(16), "two turns of the dial are sixteen pixels");

      vm.Selected = new TitleViewModel(new MediaTitle(title, "With Fanart", null, null, null, null, false));
      WaitUntil("the blurred backdrop to be worn", delegate
      {
        return vm.BackgroundArtwork != null && !ReferenceEquals(vm.BackgroundArtwork, backdrop);
      });

      // What the screen wears is the copy, not the fetched picture...
      Assert.That(vm.BackgroundArtwork, Is.Not.SameAs(backdrop), "the shelf still wears the untouched picture");
      Assert.That(PngWidth(vm.BackgroundArtwork), Is.EqualTo(160),
          "what the shelf wears is the copy the blur makes, which is a third of the picture");
      Assert.That(PngHeight(vm.BackgroundArtwork), Is.EqualTo(90));

      // ...and the screen says nought to the converter, so the converter cannot be moved to blur the same
      // picture a second time - on the thread that draws - while the viewer stands in front of it.
      Assert.That(vm.FanartBlurRadius, Is.Zero, "a copy that arrives blurred may not be asked for again");

      // Walking onto a row without a picture takes the backdrop away, and with it the copy - so the dial the
      // next row is answered with is the dial the skin speaks again.
      vm.Selected = new TitleViewModel(new MediaTitle(Guid.NewGuid(), "Without", null, null, null, null, false));
      WaitUntil("the backdrop to go with the row that has none",
          delegate { return vm.BackgroundArtwork == null; });
      Assert.That(vm.FanartBlurRadius, Is.EqualTo(16), "and the dial is the dial again with nothing worn");

      FrontendTheme.Apply(app, FrontendTheme.DEFAULT_NAME, out _);
    }
  }
}
