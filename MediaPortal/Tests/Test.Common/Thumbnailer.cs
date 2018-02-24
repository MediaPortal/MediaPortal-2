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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Services.ThumbnailGenerator;
//using MediaPortal.Extensions.MetadataExtractors.ShellThumbnailProvider;
using MediaPortal.Extensions.MetadataExtractors.WICThumbnailProvider;
using NUnit.Framework;

namespace Test.Common
{
  [TestFixture]
  public class WicThumbnailer : Thumbnailer<WICThumbnailProvider>
  {
  }

  //[TestFixture]
  //public class ShellThumbnailer : Thumbnailer<ShellThumbnailProvider>
  //{
  //}

  public abstract class Thumbnailer<T>
    where T : IThumbnailProvider, new()
  {
    [SetUp]
    public void SetUp()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [Test]
    public void CreateThumbnail512ForJpg20MP()
    {
      string fileName = @"Images\jpg_20mp.JPG";
      int size = 512;
      TestSingleThumbCreation(fileName, size, ImageType.Jpeg);
    }

    [Test]
    public void CreateThumbnail1024ForJpg20MP()
    {
      string fileName = @"Images\jpg_20mp.JPG";
      int size = 1024;
      TestSingleThumbCreation(fileName, size, ImageType.Jpeg);
    }

    [Test]
    public void CreateThumbnail2048ForJpg20MP()
    {
      string fileName = @"Images\jpg_20mp.JPG";
      int size = 2048;
      TestSingleThumbCreation(fileName, size, ImageType.Jpeg);
    }

    [Test]
    public void CreateThumbnail512ForPng()
    {
      string fileName = @"Images\png_transparent.png";
      int size = 512;
      TestSingleThumbCreation(fileName, size, null /*ImageType.Png*/); // TODO: do we expect to get png thumbs if the source is png? (This would keep the alpha channel)
    }

    [Test]
    public void CreateThumbnail1024ForPng()
    {
      string fileName = @"Images\png_transparent.png";
      int size = 1024;
      TestSingleThumbCreation(fileName, size, null /*ImageType.Png*/); // TODO: do we expect to get png thumbs if the source is png? (This would keep the alpha channel)
    }

    [Test]
    public void CreateThumbnail2048ForPng()
    {
      string fileName = @"Images\png_transparent.png";
      int size = 2048;
      TestSingleThumbCreation(fileName, size, null /*ImageType.Png*/); // TODO: do we expect to get png thumbs if the source is png? (This would keep the alpha channel)
    }

    private void TestSingleThumbCreation(string fileName, int size, ImageType? expectedImageType = null)
    {
      byte[] imageData;
      ImageType imageType;
      var result = GetProvider().GetThumbnail(fileName, size, size, false, out imageData, out imageType);
      Assert.AreEqual(result, true, "Thumbnail creation failed");
      Assert.AreNotEqual(imageData, null, "Thumbnail creation success, but no image data returned (null)");
      Assert.AreNotEqual(imageData?.Length, 0, "Thumbnail creation success, but no image data returned (length=0)");
      if (expectedImageType.HasValue)
        Assert.AreEqual(expectedImageType.Value, imageType, "Thumbnail creation success, but resulting image types is wrong");
    }

    protected IThumbnailProvider GetProvider()
    {
      return new T();
    }
  }
}
