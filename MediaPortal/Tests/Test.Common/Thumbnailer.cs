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

using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ThumbnailGenerator;
using MediaPortal.Extensions.MetadataExtractors.ImageProcessorThumbnailProvider;
//using MediaPortal.Extensions.MetadataExtractors.WICThumbnailProvider;
using NUnit.Framework;

namespace Test.Common
{
  //[TestFixture]
  //public class WicThumbnailer : Thumbnailer<WICThumbnailProvider>
  //{
  //}

  [TestFixture]
  public class ImageProcessorThumbnailer : Thumbnailer<ImageProcessorThumbnailProvider>
  {
  }

  public abstract class Thumbnailer<T>
    where T : IThumbnailProvider, new()
  {
    [SetUp]
    public void SetUp()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [Test]
    public void CreateThumbnailsForAllJpg()
    {
      var dir = new DirectoryInfo("Images");
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.jpg"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Jpeg);
    }

    [Test]
    public void CreateThumbnailsForAllPng()
    {
      var dir = new DirectoryInfo("Images");
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.png"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Png);
    }

    [Test]
    public void CreateThumbnailsForAllTif()
    {
      var dir = new DirectoryInfo("Images");
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.tif"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Png);
    }

    [Test]
    public void CreateThumbnailsForAllBmp()
    {
      var dir = new DirectoryInfo("Images");
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.bmp"))
          TestSingleThumbCreation(file.FullName, size, null); // Could be with or without alpha channel
    }

    [Test]
    public void CreateThumbnailsForAllGif()
    {
      var dir = new DirectoryInfo("Images");
      foreach (int size in new int[] { 512, 1024, 2048 })
        foreach (var file in dir.GetFiles("*.gif"))
          TestSingleThumbCreation(file.FullName, size, ImageType.Jpeg);
    }

    private void TestSingleThumbCreation(string fileName, int size, ImageType? expectedImageType = null)
    {
      string file = Path.GetFileName(fileName);
      byte[] imageData;
      ImageType imageType;
      var result = GetProvider().GetThumbnail(fileName, size, size, false, out imageData, out imageType);
      Assert.AreEqual(result, true, $"{GetType().Name}: Thumbnail creation failed ({file}, {size})");
      Assert.AreNotEqual(imageData, null, $"{GetType().Name}: Thumbnail creation success, but no image data returned (null) ({file}, {size})");
      Assert.AreNotEqual(imageData?.Length, 0, $"{GetType().Name}: Thumbnail creation success, but no image data returned (length=0) ({file}, {size})");
      if (expectedImageType.HasValue)
        Assert.AreEqual(expectedImageType.Value, imageType, $"{GetType().Name}: Thumbnail creation success, but resulting image types is wrong ({file}, {size})");

#if DEBUG
      // Only write images in debug mode for checking output quality
      if (result)
      {
        var targetBase = Path.GetFileNameWithoutExtension(fileName);
        if (!Directory.Exists("Resized"))
          Directory.CreateDirectory("Resized");
        string targetFile = $@"Resized\{targetBase}_{size}.{imageType}";
        File.WriteAllBytes(targetFile, imageData);
      }
#endif
    }

    protected IThumbnailProvider GetProvider()
    {
      return new T();
    }
  }
}
