#region Copyright (C) 2007-2023 Team MediaPortal

/*
    Copyright (C) 2007-2023 Team MediaPortal
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
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using Webradio.Models;

namespace Webradio.Helper
{
  public partial class Radiostations
  {
    public static string WebradioDataFolder = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Webradio");
    public static string StreamListFile = Path.Combine(WebradioDataFolder, "RadioStations.json");
    public static string StreamlistServerPath = "https://install.team-mediaportal.com/MP2/Webradio/RadioStations.json";

    public static async Task<bool> NeedUpdate()
    {
      if (WebradioDataModel.UpdateChecked)
        return false;

      WebradioDataModel.OnlineVersion = await OnlineVersion();
      WebradioDataModel.OfflineVersion = await OfflineVersion();

      return WebradioDataModel.OfflineVersion < WebradioDataModel.OnlineVersion;
    }

    public static async Task MakeUpdate()
    {
      try
      {
        _instance = null;
        var client = new WebClient();
        client.DownloadFileCompleted += DownloadCompleted;
        client.DownloadProgressChanged += DownloadStatusChanged;
        await client.DownloadFileTaskAsync(new Uri(StreamlistServerPath), StreamListFile);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Webradio: Error read Online Stationslist '{0}'", ex);
      }
    }

    public static async Task SetInfos()
    {
      WebradioDataModel.OfflineVersion = await OfflineVersion();
      WebradioDataModel.RadiostationsCount = Instance.Stations?.Count ?? 0;
    }

    private static Task<int> OnlineVersion()
    {
      var request = WebRequest.Create(StreamlistServerPath);
      request.Credentials = CredentialCache.DefaultCredentials;

      try
      {
        using (var response = request.GetResponse())
        using (var reader = new StreamReader(response.GetResponseStream()))
        {
          var buffer = new char[100];
          reader.ReadAsync(buffer, 0, 100);

          var s = new string(buffer);
          var a = s.IndexOf(":", StringComparison.Ordinal) + 1;
          var b = s.IndexOf(",", StringComparison.Ordinal);
          return Task.FromResult(Convert.ToInt32(s.Substring(a, b - a)));
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Webradio: Error read OnlineVersion '{0}'", ex);
        return Task.FromResult(-1);
      }
    }

    private static Task<int> OfflineVersion()
    {
      if (!StreamListExists()) return Task.FromResult(-1);

      try
      {
        var ms = Instance;
        return Task.FromResult(ms.Version);
      }
      catch (Exception)
      {
        return Task.FromResult(-1);
      }
    }

    private static bool StreamListExists()
    {
      if (!Directory.Exists(WebradioDataFolder))
        Directory.CreateDirectory(WebradioDataFolder);
      return File.Exists(StreamListFile);
    }

    private static void DownloadCompleted(object sender, AsyncCompletedEventArgs e)
    {
      WebradioDataModel.UpdateProgress = 0;
      WebradioDataModel.UpdateInfo = e.Error == null ? DOWNLOAD_COMPLETE : DOWNLOAD_ERROR;
    }

    private static void DownloadStatusChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      WebradioDataModel.UpdateProgress = e.ProgressPercentage;
    }

    #region Consts
    
    protected const string DOWNLOAD_COMPLETE = "[Webradio.Dialog.LoadUpdate.DownloadComplete]";
    protected const string DOWNLOAD_ERROR = "[Webradio.Dialog.LoadUpdate.DownloadError]";

    #endregion
  }
}
