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
