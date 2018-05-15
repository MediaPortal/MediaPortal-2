using System.IO;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.MetadataExtractors.MatroskaLib;
using NUnit.Framework;

namespace Tests.Client.Libraries.MatroskaLib
{
  [TestFixture]
  public class TestMatroskaLib
  {
    [Test]
    public void TestStereoscopicInfo()
    {
      // Arrange
      string testFileName = "big-buck-bunny_trailer.webm";
      string testDataDir = TestContext.CurrentContext.TestDirectory + "\\Libraries\\MatroskaLib\\TestData\\";
      ILocalFsResourceAccessor lfsra = new LocalFsResourceAccessor(new LocalFsResourceProvider(), "/" + Path.Combine(testDataDir, testFileName));
      MatroskaInfoReader matroskaInfoReader = new MatroskaInfoReader(lfsra);
      
      // Act
      MatroskaInfoReader.StereoMode actualStereoMode = matroskaInfoReader.ReadStereoModeAsync().Result;

      // Assert
      Assert.AreEqual(MatroskaInfoReader.StereoMode.Mono, actualStereoMode);
    }
  }
}
