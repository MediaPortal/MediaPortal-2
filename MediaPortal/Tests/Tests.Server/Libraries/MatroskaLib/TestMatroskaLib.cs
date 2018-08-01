using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using NUnit.Framework;
using System.Linq;
using MediaPortal.Common.Logging;

namespace Tests.Server.Libraries.MatroskaLib
{
  [TestFixture]
  public class TestMatroskaLib
  {
    [SetUp]
    public void Inti()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
    }

    private MatroskaBinaryReader GetMatroskaBinaryFileReader()
    {
      string testFileName = "Test Media.mkv";
      string testDataDir = TestContext.CurrentContext.TestDirectory + "\\Libraries\\MatroskaLib\\TestData\\";
      ILocalFsResourceAccessor lfsra = new LocalFsResourceAccessor(new LocalFsResourceProvider(), "/" + Path.Combine(testDataDir, testFileName));
      return new MatroskaBinaryReader(lfsra);
    }

    [Test]
    public void TestMatroskaBinaryTagInfo()
    {
      // Arrange
      MatroskaBinaryReader matroskaInfoReader = GetMatroskaBinaryFileReader();
      var tags = MatroskaConsts.DefaultVideoTags;

      // Act
      matroskaInfoReader.ReadTagsAsync(tags).Wait();

      // Assert
      Assert.AreEqual("Test Title", tags[MatroskaConsts.TAG_SIMPLE_TITLE].First());
      Assert.AreEqual("Doctor Who", tags[MatroskaConsts.TAG_SERIES_TITLE].First());
      Assert.AreEqual("76107", tags[MatroskaConsts.TAG_SERIES_TVDB_ID].First());
      Assert.IsTrue(tags[MatroskaConsts.TAG_EPISODE_SUMMARY].First().Length > 0);
      Assert.AreEqual("1963-11-23", tags[MatroskaConsts.TAG_EPISODE_YEAR].First());
      Assert.AreEqual("Doctor Who", tags[MatroskaConsts.TAG_EPISODE_TITLE].First());
      Assert.AreEqual("1", tags[MatroskaConsts.TAG_EPISODE_NUMBER].First());
      Assert.AreEqual("tt0056751", tags[MatroskaConsts.TAG_MOVIE_IMDB_ID].First());
    }

    [Test]
    public void TestMatroskaBinaryStereoscopicInfo()
    {
      // Arrange
      MatroskaBinaryReader matroskaInfoReader = GetMatroskaBinaryFileReader();

      // Act
      MatroskaConsts.StereoMode actualStereoMode = matroskaInfoReader.ReadStereoModeAsync().Result;

      // Assert
      Assert.AreEqual(MatroskaConsts.StereoMode.SBSLeftEyeFirst, actualStereoMode);
    }

    [Test]
    public void TestMatroskaBinaryAttachmentInfo()
    {
      // Arrange
      MatroskaBinaryReader matroskaInfoReader = GetMatroskaBinaryFileReader();

      // Act
      matroskaInfoReader.ReadAttachmentsAsync().Wait();

      // Assert
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("cover.").Result.Length > 0);
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("banner.").Result.Length > 0);
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("poster.").Result.Length > 0);
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("fanart.").Result.Length > 0);
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("clearart.").Result.Length > 0);
      Assert.IsTrue(matroskaInfoReader.GetAttachmentByNameAsync("clearlogo.").Result.Length > 0);
    }
  }
}
