using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.OnlineVideos
{
  /// <summary>
  /// OnlineVideos doesn't provide any kind of Ids so we need to generate our own ids to keep track of everything.
  /// </summary>
  static internal class OnlineVideosIdGenerator
  {
    internal const string ID_SEPARATOR = "[#:#:#]";

    /// <summary>
    /// Builds an Base64 encoded id by using the ID_SEPARATOR defined in the class
    /// </summary>
    /// <param name="siteName"></param>
    /// <returns>example: Base64.Encode(siteName)</returns>
    internal static string BuildSiteId(string siteName)
    {
      return Base64.Encode(siteName);
    }

    /// <summary>
    /// Builds an Base64 encoded id by using the ID_SEPARATOR defined in the class
    /// </summary>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    /// <returns>example: Base64.Encode(siteName[ID_SEPARATOR]categoryRecursiveName)</returns>
    internal static string BuildCategoryId(string siteName, string categoryRecursiveName)
    {
      StringBuilder output = new StringBuilder();
      output.Append(siteName).Append(ID_SEPARATOR);
      output.Append(categoryRecursiveName);
      return Base64.Encode(output.ToString());
    }

    /// <summary>
    /// Builds an Base64 encoded id by using the ID_SEPARATOR defined in the class
    /// </summary>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    /// <param name="videoUrl"></param>
    /// <returns>example: Base64.Encode(siteName[ID_SEPARATOR]categoryRecursiveName[ID_SEPERATOR]videoUrl)</returns>
    internal static string BuildVideoId(string siteName, string categoryRecursiveName, string videoUrl)
    {
      StringBuilder output = new StringBuilder();
      output.Append(siteName).Append(ID_SEPARATOR);
      output.Append(categoryRecursiveName).Append(ID_SEPARATOR);
      output.Append(videoUrl);
      return Base64.Encode(output.ToString());
    }

    /// <summary>
    /// Takes a Base64 encoded ID and outputs the siteName
    /// </summary>
    /// <param name="id"></param>
    /// <param name="siteName"></param>
    internal static void DecodeSiteId(string id, out string siteName)
    {
      siteName = Base64.Decode(id);
    }

    /// <summary>
    /// Takes a Base64 encoded ID and outputs the siteName and recrusive Name of the category
    /// </summary>
    /// <param name="id"></param>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    internal static void DecodeCategoryId(string id, out string siteName, out string categoryRecursiveName)
    {
      string idDecoded = Base64.Decode(id);
      string[] idSplitted = idDecoded.Split(new string[] { ID_SEPARATOR }, StringSplitOptions.None);
      siteName = idSplitted[0];
      categoryRecursiveName = idSplitted[1];
    }

    /// <summary>
    /// Takes a Base64 encoded ID and outputs the siteName, the recrusive Name of the category and the video Url
    /// </summary>
    /// <param name="id"></param>
    /// <param name="siteName"></param>
    /// <param name="categoryRecursiveName"></param>
    /// <param name="videoUrl"></param>
    internal static void DecodeVideoId(string id, out string siteName, out string categoryRecursiveName, out string videoUrl)
    {
      string idDecoded = Base64.Decode(id);
      string[] idSplitted = idDecoded.Split(new string[] { ID_SEPARATOR }, StringSplitOptions.None);
      siteName = idSplitted[0];
      categoryRecursiveName = idSplitted[1];
      videoUrl = idSplitted[2];
    }
  }
}
