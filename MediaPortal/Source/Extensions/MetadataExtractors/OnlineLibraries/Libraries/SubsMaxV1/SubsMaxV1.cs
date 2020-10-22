#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using ICSharpCode.SharpZipLib.Zip;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubsMaxV1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SubsMaxV1
{
  internal class SubsMaxV1
  {
    #region Constants

    private const int MAX_SUBTITLES_PER_QUERY = 10;
    private const string URL_API_BASE = "subsmax.com/subtitles-api/";

    #endregion

    #region Fields

    private static readonly FileVersionInfo FILE_VERSION_INFO;
    private readonly Downloader _downloader;
    private readonly bool _useHttps;

    #endregion

    #region Constructor

    static SubsMaxV1()
    {
      FILE_VERSION_INFO = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetCallingAssembly().Location);
    }

    public SubsMaxV1()
    {
      _useHttps = false;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["User-Agent"] = $"SubsMax/1.0 (MediaPortal/{FILE_VERSION_INFO.FileVersion}; https://www.team-mediaportal.com)";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for subtitles by title and year.
    /// </summary>
    /// <returns>List of available subtitles</returns>
    public Task<List<SubsMaxSearchResult>> SearchMovieSubtitlesByTitleAndYearAsync(string title, int? year, string language)
    {
      List<string> searchArgs = new List<string>(GetSearchArguments(title));
      searchArgs.Add(language);
      if (year.HasValue)
        searchArgs.Add(year.Value.ToString());

      return GetResultsAsync(searchArgs);
    }

    /// <summary>
    /// Search for subtitles by episode.
    /// </summary>
    /// <returns>List of available subtitles</returns>
    public Task<List<SubsMaxSearchResult>> SearchSeriesSubtitlesAsync(string series, int season, int episode, string language)
    {
      List<string> searchArgs = new List<string>(GetSearchArguments(series));
      searchArgs.Add($"S{season.ToString("00")}E{episode.ToString("00")}");
      searchArgs.Add(language);

      return GetResultsAsync(searchArgs);
    }

    /// <summary>
    /// Downloads subtitle to the specified path.
    /// </summary>
    /// <param name="subUrl">URL of the subtitle to download</param>
    /// <returns>File info if successful</returns>
    public async Task<FileInfo[]> DownloadSubtileAsync(string subUrl)
    {
      int idx = subUrl.LastIndexOf("/");
      string downloadUrl = $"{subUrl.Substring(0, idx + 1)}download-subtitle/{subUrl.Substring(idx + 1)}";
      List<FileInfo> subFiles = new List<FileInfo>();
      var fileName = Path.Combine(Path.GetTempPath(), "SubsMax_" + Path.GetRandomFileName() + ".zip");
      if (await _downloader.DownloadFileAsync(downloadUrl, fileName))
      {
        var zipFile = new ZipFile(fileName);
        foreach (ZipEntry zipEntry in zipFile)
        {
          if (!zipEntry.IsFile)
            continue;

          using (var zipStream = zipFile.GetInputStream(zipEntry))
          {
            var subFile = Path.Combine(Path.GetTempPath(), "SubsMax_" + Path.GetRandomFileName() + Path.GetExtension(zipEntry.Name));
            using (var subFileStream = File.OpenWrite(subFile))
              await zipStream.CopyToAsync(subFileStream);
            subFiles.Add(new FileInfo(subFile));
          }
        }
        zipFile.Close();
        try { File.Delete(fileName); } catch { };
        return subFiles.ToArray();
      }
      return null;
    }

    #endregion

    #region Protected members

    public async Task<List<SubsMaxSearchResult>> GetResultsAsync(IEnumerable<string> searchArgs)
    {
      string xml = await _downloader.DownloadStringAsync(GetUrl(searchArgs));
      var doc = XDocument.Parse(xml);
      List<SubsMaxSearchResult> results = new List<SubsMaxSearchResult>();
      foreach (var item in doc.Root.Descendants("item"))
      {
        SubsMaxSearchResult result = new SubsMaxSearchResult();
        result.DownloadUrl = item.Element("link").Value;
        string fileList = item.Element("files_in_archive").Value;
        foreach (var file in fileList.Split('|'))
        {
          var fileInfo = file.Split(',');
          result.ArchiveFiles.Add(new SubsMaxFile
          {
            Name = fileInfo[0],
            Language = fileInfo[1]
          });
        }
        results.Add(result);
      }
      return results;
    }

    protected string[] GetSearchArguments(string title)
    {
      return Slugify(title).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    protected string RemoveAccent(string txt)
    {
      byte[] bytes = Encoding.GetEncoding("Cyrillic").GetBytes(txt);
      return Encoding.ASCII.GetString(bytes);
    }

    protected string Slugify(string txt)
    {
      string str = RemoveAccent(txt).ToLower();
      str = System.Text.RegularExpressions.Regex.Replace(str, @"[^\w\d\s-]", ""); // Remove all non valid chars          
      str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim(); // Convert multiple spaces into one space  
      str = System.Text.RegularExpressions.Regex.Replace(str, @"\s", "-"); // //Replace spaces by dashes
      str = System.Text.RegularExpressions.Regex.Replace(str, @"\-+", "- "); // Convert multiple dashes into one dash
      return str;
    }

    /// <summary>
    /// Builds and returns the full request url.
    /// </summary>
    /// <param name="args">Arguments for the request</param>
    /// <returns>Complete url</returns>
    protected string GetUrl(IEnumerable<string> args)
    {
      string replacedUrl = $"{URL_API_BASE}{MAX_SUBTITLES_PER_QUERY}/" + string.Join("-", args);

      if(_useHttps)
        return string.Format("https://{0}", replacedUrl);
      else
        return string.Format("http://{0}", replacedUrl);
    }

    protected static ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    #endregion
  }
}
