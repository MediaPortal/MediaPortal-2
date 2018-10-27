using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using NUnit.Framework;
using MediaPortal.Common.Logging;
using System.Collections.Generic;
using System;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.OCVVideoThumbnailer;

namespace Tests.Server.Libraries.MatroskaLib
{
  [TestFixture]
  public class TestOCVVideoThumbnailer
  {
    [SetUp]
    public void Inti()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    [Test]
    public void TestExtractThumbnail()
    {
      // Arrange
      string testFileName = "Test Media.mkv";
      string testDataDir = TestContext.CurrentContext.TestDirectory + "\\VideoThumbnailer\\TestData\\";
      ILocalFsResourceAccessor lfsra = new LocalFsResourceAccessor(new LocalFsResourceProvider(), "/" + Path.Combine(testDataDir, testFileName));

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      MultipleMediaItemAspect videoStreamAspect = new MultipleMediaItemAspect(VideoStreamAspect.Metadata);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, 0);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 1);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_DURATION, (long)1);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1076);
      videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1916);
      MediaItemAspect.AddOrUpdateAspect(aspects, videoStreamAspect);
      MultipleMediaItemAspect resourceAspect = new MultipleMediaItemAspect(ProviderResourceAspect.Metadata);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, 0);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
      resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, lfsra.CanonicalLocalResourcePath.Serialize());
      MediaItemAspect.AddOrUpdateAspect(aspects, resourceAspect);

      // Act
      bool success = new OCVVideoThumbnailer().TryExtractMetadataAsync(lfsra, aspects, false).Result;

      // Assert
      Assert.IsTrue(success);
      Assert.IsTrue(aspects.ContainsKey(ThumbnailLargeAspect.ASPECT_ID));
    }
  }
}
