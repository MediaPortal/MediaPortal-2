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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.SubDbV1.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.SubDbV1
{
  internal class SubDbV1
  {
    #region Constants

    private const string URL_API_BASE = "api.thesubdb.com/";
    private const string URL_QUERYLANGUAGES = URL_API_BASE + "?action=languages";
    private const string URL_GETSUBTITLE =   URL_API_BASE + "?action=download&hash={0}&language={1}";
    private const string URL_QUERYSUBTITLES = URL_API_BASE + "?action=search&hash={0}";

    #endregion

    #region Fields

    private static readonly FileVersionInfo FILE_VERSION_INFO;
    private readonly Downloader _downloader;
    private readonly bool _useHttps;

    #endregion

    #region Constructor

    static SubDbV1()
    {
      FILE_VERSION_INFO = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetCallingAssembly().Location);
    }

    public SubDbV1()
    {
      _useHttps = false;
      _downloader = new Downloader { EnableCompression = true };
      _downloader.Headers["User-Agent"] = $"SubDB/1.0 (MediaPortal/{FILE_VERSION_INFO.FileVersion}; https://www.team-mediaportal.com)";
    }

    #endregion

    #region Public members

    /// <summary>
    /// Search for subtitles by file stream given in <paramref name="videoFile"/>.
    /// </summary>
    /// <param name="videoFile">The video file</param>
    /// <returns>List of available languages</returns>
    public async Task<List<SubDbSearchResult>> SearchSubtitlesAsync(Stream videoFile)
    {
      var hash = GetHashString(videoFile);
      string url = GetUrl(URL_QUERYSUBTITLES, hash);
      string results = await _downloader.DownloadStringAsync(url).ConfigureAwait(false);
      if (string.IsNullOrEmpty(results))
        return new List<SubDbSearchResult>();
      return new List<SubDbSearchResult>(results.Split(',').Select(s => new SubDbSearchResult { LanguageCode = s, DownloadUrl = GetUrl(URL_GETSUBTITLE, hash, s) }));
    }

    /// <summary>
    /// Downloads subtitle to the specified path.
    /// </summary>
    /// <param name="subUrl">URL of the subtitle to download</param>
    /// <param name="filePath">The target file name</param>
    /// <returns>File info if successful</returns>
    public async Task<FileInfo> DownloadSubtileAsync(string subUrl)
    {
      var fileName = Path.Combine(Path.GetTempPath(), "SubDb_" + Path.GetRandomFileName() + ".srt");
      if (await _downloader.DownloadFileAsync(subUrl, fileName))
      {
        string subContent = File.ReadAllText(fileName);
        if (subContent.Contains("[INFORMATION]"))
        {
          var newFileName = $"{fileName.Substring(0, fileName.Length - 3)}sub";
          File.Move(fileName, newFileName);
          fileName = newFileName;
        }
        return new FileInfo(fileName);
      }
      return null;
    }

    #endregion

    #region Protected members

    private string GetHashString(Stream file)
    {
      var binHash = ComputeVideoHash(file);

      StringBuilder hex = new StringBuilder(binHash.Length * 2);
      foreach (byte b in binHash)
        hex.AppendFormat("{0:x2}", b);
      return hex.ToString();
    }

    private byte[] ComputeVideoHash(Stream file)
    {
      int readSize = 64 * 1024;
      byte[] array = new byte[readSize];
      List<byte> data = new List<byte>();

      file.Position = 0;
      file.Read(array, 0, readSize);
      data.AddRange(array);

      long endPos = file.Length - readSize;
      file.Position = endPos;
      file.Read(array, 0, readSize);
      data.AddRange(array);

      return new MD5CryptoServiceProvider().ComputeHash(data.ToArray());
    }

    /// <summary>
    /// Builds and returns the full request url.
    /// </summary>
    /// <param name="urlBase">Query base</param>
    /// <param name="args">Optional arguments to format <paramref name="urlBase"/></param>
    /// <returns>Complete url</returns>
    protected string GetUrl(string urlBase, params object[] args)
    {
      string replacedUrl = string.Format(urlBase, args);

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
