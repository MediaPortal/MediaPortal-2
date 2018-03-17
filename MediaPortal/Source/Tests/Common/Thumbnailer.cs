#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Extensions.MetadataExtractors.GDIThumbnailProvider;
using MediaPortal.Extensions.MetadataExtractors.WICThumbnailProvider;
using NUnit.Framework;

namespace Tests.Common
{
  [TestFixture]
  public class WicThumbnailer : Thumbnailer
  {
    protected override void InitProviders()
    {
      _providers.Add(new WICThumbnailProvider());
      _skipJpg = true; // We know that the contained images are not working with WIC, skip this test
    }
  }

  [TestFixture]
  public class GdiThumbnailer : Thumbnailer
  {
    protected override void InitProviders()
    {
      _providers.Add(new GDIThumbnailProvider());
    }
  }

  [TestFixture]
  public class CombinedThumbnailer : Thumbnailer
  {
    protected override void InitProviders()
    {
      _providers.Add(new WICThumbnailProvider());
      _providers.Add(new GDIThumbnailProvider());
    }
  }


  public abstract class Thumbnailer
  {
    protected string IMAGE_FOLDER;
    protected string RESIZED_FOLDER;

    protected readonly List<IThumbnailProvider> _providers = new List<IThumbnailProvider>();
    protected bool _skipJpg = false;

    [SetUp]
    public void SetUp()
    {
      var contextTestDirectory = TestContext.CurrentContext.TestDirectory;
      Console.WriteLine((string)"Test setup: Current test folder {0}", (object)contextTestDirectory);

      // Workaround for TeamCity build failure, where the test is executed inside another folder.
      if (!contextTestDirectory.Contains("Tests"))
      {
        var testRoot = @"MediaPortal\Tests\";
        var pos = contextTestDirectory.IndexOf(testRoot);
        contextTestDirectory = contextTestDirectory.Substring(0, pos) + testRoot + @"Tests\bin\x86\Release";
        Console.WriteLine((string)"Test setup: Remapped folder for image test to {0}", (object)contextTestDirectory);
      }

      IMAGE_FOLDER = contextTestDirectory + "\\Images\\";
      RESIZED_FOLDER = contextTestDirectory + "\\Resized\\";

      ServiceRegistration.Set<ILogger>(new NoLogger());
      InitProviders();
    }

    protected abstract void InitProviders();

    [Test]
    public void CreateThumbnailsForAllJpg()
    {
      if (_skipJpg)
        return;
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.jpg"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Jpeg);
    }

    [Test]
    public void CreateThumbnailsForAllJpgParallel()
    {
      if (_skipJpg)
        return;
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        Parallel.ForEach(dir.GetFiles("*.jpg"), file =>
        {
          TestSingleThumbCreation(file.FullName, size, ImageType.Jpeg);
        });
    }

    [Test]
    public void CreateThumbnailsForAllPng()
    {
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.png"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Png);
    }

    [Test]
    public void CreateThumbnailsForAllTif()
    {
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.tif"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Png);
    }

    [Test]
    public void CreateThumbnailsForAllBmp()
    {
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.bmp"))
          TestSingleThumbCreation(file.FullName, size, null); // Could be with or without alpha channel
    }

    [Test]
    public void CreateThumbnailsForAllGif()
    {
      var dir = new DirectoryInfo(IMAGE_FOLDER);
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.gif"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Jpeg);
    }

    private void TestSingleThumbCreation(string fileName, int size, ImageType? expectedImageType = null)
    {
      string file = Path.GetFileName(fileName);
      byte[] imageData = null;
      ImageType imageType = ImageType.Unknown;
      bool result = false;
      IThumbnailProvider lastUsedProvider = null;
      foreach (IThumbnailProvider provider in _providers)
      {
        // We know that not all providers can support all formats, so we allow all to be tried.
        lastUsedProvider = provider;
        result = provider.GetThumbnail(fileName, size, size, false, out imageData, out imageType);
        if (result)
          break;
      }

      Assert.AreEqual(true, result, $"{lastUsedProvider?.GetType().Name}: Thumbnail creation failed ({file}, {size})");
      Assert.AreNotEqual(null, imageData, $"{lastUsedProvider?.GetType().Name}: Thumbnail creation success, but no image data returned (null) ({file}, {size})");
      Assert.AreNotEqual(0, imageData?.Length, $"{lastUsedProvider?.GetType().Name}: Thumbnail creation success, but no image data returned (length=0) ({file}, {size})");
      if (expectedImageType.HasValue)
        Assert.AreEqual(expectedImageType.Value, imageType, $"{lastUsedProvider?.GetType().Name}: Thumbnail creation success, but resulting image types is wrong ({file}, {size})");

#if DEBUG
      // Only write images in debug mode for checking output quality
      if (result)
      {
        var targetBase = Path.GetFileNameWithoutExtension(fileName);
        if (!Directory.Exists(RESIZED_FOLDER))
          Directory.CreateDirectory(RESIZED_FOLDER);
        string targetFile = RESIZED_FOLDER+ $"{targetBase}_{size}.{imageType}";
        File.WriteAllBytes(targetFile, imageData);
      }
#endif
    }
  }
}
