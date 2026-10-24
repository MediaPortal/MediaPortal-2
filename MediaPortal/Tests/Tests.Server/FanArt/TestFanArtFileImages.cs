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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.Imaging;
using MediaPortal.Common.Services.ThumbnailGenerator;
using Moq;
using NUnit.Framework;
using Tests.Server.FanArt.FanArtHandlersForTests;

namespace Tests.Server.FanArt
{
  /// <summary>
  /// Tests how <see cref="BaseFanArtHandlerForTests"/> files artwork that came in as bytes, which is
  /// what every NFO driven library does with its posters, fanarts, logos and actor pictures.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The reference re-encoded all of it to JPEG and named the result <c>.jpg</c>, which needed an
  /// image transformer and threw the alpha channel away. Both of those consequences showed on the
  /// headless server, where no transformer can be registered: a library of 459 titles lost 887
  /// pictures to the missing service, so its fan art cache stayed empty. The tests below pin the
  /// behaviour that replaced it - a JPEG or PNG is filed as it is and under the extension its bytes
  /// are, a format that cannot be filed honestly is only filed where a converter was promised.
  /// </para>
  /// </remarks>
  [TestFixture]
  public class TestFanArtFileImages
  {
    #region Pictures of known make

    /// <summary>
    /// A PNG by its signature, with whatever bytes a transparency chunk might carry behind it.
    /// </summary>
    static readonly byte[] PNG = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x12, 0x34 };

    /// <summary>
    /// A JPEG by its signature.
    /// </summary>
    static readonly byte[] JPEG = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x11, 0x22, 0x33 };

    /// <summary>
    /// Neither of the two, so only a converter could do anything with it.
    /// </summary>
    static readonly byte[] GIF = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x44, 0x55 };

    #endregion

    private string _folder;
    private bool _addedLogger;
    private BaseFanArtHandlerForTests _handler;

    [SetUp]
    public void SetUp()
    {
      _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "FanArtFileImages." + Guid.NewGuid().ToString())).FullName;
      _handler = new BaseFanArtHandlerForTests();
      if (!ServiceRegistration.IsRegistered<ILogger>())
      {
        ServiceRegistration.Set<ILogger>(new Mock<ILogger>().Object);
        _addedLogger = true;
      }
    }

    [TearDown]
    public void TearDown()
    {
      if (ServiceRegistration.IsRegistered<IImageTransformer>())
        ServiceRegistration.Remove<IImageTransformer>();
      if (_addedLogger && ServiceRegistration.IsRegistered<ILogger>())
        ServiceRegistration.Remove<ILogger>();
      try { Directory.Delete(_folder, true); } catch (IOException) { }
    }

    #region A picture that already is what the cache stores

    [Test]
    public async Task ATransparentPngIsStoredByItselfAndNamedForWhatItIs()
    {
      Assert.That(await _handler.TestSaveFileImage(PNG, _folder, "ClearArt", "Nfo."), Is.True);

      // The name says PNG, because that is what the bytes are, and the cache lists PNG beside JPEG.
      string path = Path.Combine(_folder, "Nfo.ClearArt.png");
      Assert.That(File.Exists(path), Is.True);
      Assert.That(Directory.GetFiles(_folder), Is.EquivalentTo(new[] { path }));
      // Written unchanged, so the transparency a clearart is made of is still in there.
      Assert.That(File.ReadAllBytes(path), Is.EqualTo(PNG));
    }

    [Test]
    public async Task AJpegIsNotReEncodedOnItsWayIntoTheCache()
    {
      // A transformer that would answer is registered on purpose: even with one going, bytes that
      // already are JPEG are not pushed through it a second time and lose quality.
      ServiceRegistration.Set<IImageTransformer>(new JpegConverter());
      Assert.That(await _handler.TestSaveFileImage(JPEG, _folder, "Poster", "Nfo."), Is.True);

      string path = Path.Combine(_folder, "Nfo.Poster.jpg");
      Assert.That(File.ReadAllBytes(path), Is.EqualTo(JPEG));
    }

    [Test]
    public async Task APictureOnceStoredIsNotWrittenAgain()
    {
      Assert.That(await _handler.TestSaveFileImage(JPEG, _folder, "Poster", "Nfo."), Is.True);
      Assert.That(await _handler.TestSaveFileImage(JPEG, _folder, "Poster", "Nfo."), Is.False);
      Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(1));
    }

    #endregion

    #region A picture the cache has no name for

    [Test]
    public void APictureOfAnotherFormatIsNotFiledUnderANameThatLiesAboutIt()
    {
      // No transformer: the only honest answer is no file at all. This is the case that used to
      // throw, take the picture with it and write a stack trace per picture into the server log.
      Assert.That(_handler.TestSaveFileImage(GIF, _folder, "FanArt", "Nfo.").Result, Is.False);
      Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(0));
    }

    [Test]
    public async Task APictureOfAnotherFormatBecomesAJpegWhereAConverterWasRegistered()
    {
      ServiceRegistration.Set<IImageTransformer>(new JpegConverter());
      Assert.That(await _handler.TestSaveFileImage(GIF, _folder, "FanArt", "Nfo."), Is.True);

      string path = Path.Combine(_folder, "Nfo.FanArt.jpg");
      Assert.That(File.ReadAllBytes(path), Is.EqualTo(JpegConverter.JpegBytes));
    }

    [Test]
    public async Task APictureAConverterRefusesIsLeftAlone()
    {
      ServiceRegistration.Set<IImageTransformer>(new JpegConverter());
      Assert.That(await _handler.TestSaveFileImage(new byte[] { 0x01, 0x02, 0x03, 0x04 }, _folder, "Banner", "Nfo."), Is.False);
      Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(0));
    }

    #endregion

    #region Nothing to store

    [Test]
    public async Task APictureTooShortToRecognizeIsNotClaimedToBeAJpeg()
    {
      // Not long enough to hold any signature, so it can only be judged by a converter, which has
      // none here. Naming it ".jpg" the way the reference did would file rubbish as a photograph.
      Assert.That(await _handler.TestSaveFileImage(new byte[] { 0xFF, 0xD8 }, _folder, "Logo", "Nfo."), Is.False);
      Assert.That(Directory.GetFiles(_folder).Length, Is.EqualTo(0));
    }

    #endregion

    /// <summary>
    /// A converter that turns anything beginning with a known signature into <see cref="JpegBytes"/>
    /// and refuses everything else, which is enough to tell the two paths apart.
    /// </summary>
    class JpegConverter : IImageTransformer
    {
      public static readonly byte[] JpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xDB, 0x99 };

      public bool TryEncode(Stream source, ImageType targetFormat, out byte[] imageData)
      {
        imageData = null;
        if (targetFormat != ImageType.Jpeg)
          return false;
        using (MemoryStream copy = new MemoryStream())
        {
          source.CopyTo(copy);
          if (!copy.ToArray().SequenceEqual(GIF))
            return false;
        }
        imageData = JpegBytes;
        return true;
      }

      public bool TryResize(Stream source, int maxWidth, int maxHeight, ImageType targetFormat, out byte[] imageData)
      {
        imageData = null;
        return false;
      }
    }
  }
}
