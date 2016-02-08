using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using OnlineVideos;
using OnlineVideos.Downloading;
using OnlineVideos.Sites;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  internal static class OnlineVideosThumbs
  {
    private const string ICONS_DIR = "Icons";
    
    internal static byte[] GetThumb(WebOnlineVideosMediaType mediaType, string id)
    {
      string file = string.Empty;

      switch (mediaType)
      {
        case WebOnlineVideosMediaType.Site:
          file = GetSiteThumbPath(id);
          break;
        case WebOnlineVideosMediaType.GlobalSite:
          file = GetGlobalSiteThumbPath(id);
          break;
        case WebOnlineVideosMediaType.Category:
        case WebOnlineVideosMediaType.SubCategory:
          file = GetCategoryThumbPath(id, mediaType);
          break;
        case WebOnlineVideosMediaType.Video:
            file = GetVideoThumbPath(id);
          break;
      }

      return File.Exists(file) ? File.ReadAllBytes(file) : GetDummyImage();
    }

    private static string GetSiteThumbPath(string id)
    {
      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(id, out siteName);

      SiteUtilBase site = MP2Extended.OnlineVideosManager.GetSites().Single(x => x.Settings.Name == siteName);

      // use Icon with the same name as the Site
      string image = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, ICONS_DIR + @"/" + site.Settings.Name + ".png");
      if (File.Exists(image))
        return image;

      // if that does not exist, try icon with the same name as the Util
      image = Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, ICONS_DIR + @"/" + site.Settings.UtilName + ".png");
      return File.Exists(image) ? image : string.Empty;
    }

    private static string GetGlobalSiteThumbPath(string id)
    {
      string siteName;
      OnlineVideosIdGenerator.DecodeSiteId(id, out siteName);
      GlobalSite site = MP2Extended.OnlineVideosManager.GetGlobalSites().Single(x => x.Site.Name == siteName);

      return Path.Combine(OnlineVideoSettings.Instance.ThumbsDir, ICONS_DIR + @"/" + site.Site.Name + ".png");
    }

    private static string GetCategoryThumbPath(string id, WebOnlineVideosMediaType mediaType)
    {
      string siteName;
      string categoryRecrusiveName;
      OnlineVideosIdGenerator.DecodeCategoryId(id, out siteName, out categoryRecrusiveName);

      Category category;
      if (mediaType == WebOnlineVideosMediaType.Category)
        category = MP2Extended.OnlineVideosManager.GetSiteCategories(siteName).Single(x => x.RecursiveName() == categoryRecrusiveName);
      else
        category = MP2Extended.OnlineVideosManager.GetSubCategories(siteName, categoryRecrusiveName).Single(x => x.RecursiveName() == categoryRecrusiveName);

      // Download the thumb in case it doesn't exist
      if (category.ThumbnailImage == null)
        ImageDownloader.DownloadImages(new List<Category> { category });

      return category.ThumbnailImage;
    }

    private static string GetVideoThumbPath(string id)
    {
      string siteName;
      string categoryRecrusiveName;
      string videoUrl;
      OnlineVideosIdGenerator.DecodeVideoId(id, out siteName, out categoryRecrusiveName, out videoUrl);

      VideoInfo video = MP2Extended.OnlineVideosManager.GetCategoryVideos(siteName, categoryRecrusiveName).Single(x => x.VideoUrl == videoUrl);

      // Download the thumb in case it doesn't exist
      if (video.ThumbnailImage == null)
        ImageDownloader.DownloadImages(new List<VideoInfo> { video });

      return video.ThumbnailImage;
    }

    private static byte[] GetDummyImage()
    {
      Bitmap newImage = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
      Graphics graphic = Graphics.FromImage(newImage);
      graphic.Clear(Color.Transparent);
      MemoryStream ms = new MemoryStream();
      newImage.Save(ms, ImageFormat.Png);
      return ms.ToArray();
    }
  }
}
