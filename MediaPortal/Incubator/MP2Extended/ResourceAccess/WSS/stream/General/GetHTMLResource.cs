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
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Utilities.SystemAPI;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "This function is inernally used by the MP2Ext webinterface.")]
  [ApiFunctionParam(Name = "path", Type = typeof(string), Nullable = false)]
  internal class GetHtmlResource : BaseSendData
  {
    private static string _localPath;
    private static string _appDataPath;

    static GetHtmlResource()
    {
      Assembly assembly = Assembly.GetExecutingAssembly();
      _localPath = Path.GetDirectoryName(assembly.Location);
      _appDataPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Web\");
    }

    public static async Task<bool> ProcessAsync(IOwinContext context, string path)
    {
      if (path == null)
      {
        if (context.Request.Uri.Segments.Length >= 3)
          path = string.Join("", context.Request.Uri.Segments.Skip(2));
      }

      if (path == null)
        throw new BadRequestException("GetHtmlResource: path is null");

      string skinPath = string.IsNullOrEmpty(MP2Extended.Settings.SkinName) ? "Default" : MP2Extended.Settings.SkinName;

      string resourceBasePath = Path.Combine(_localPath, "Skins", skinPath);
      string resourcePath = Path.GetFullPath(Path.Combine(resourceBasePath, path));
      if (!File.Exists(resourcePath))
      {
        resourceBasePath = Path.Combine(_appDataPath, "Skins", skinPath);
        resourcePath = Path.GetFullPath(Path.Combine(resourceBasePath, path));
      }

      if (!resourcePath.StartsWith(resourceBasePath))
        throw new BadRequestException(string.Format("GetHtmlResource: Outside home dir! Requested Path: {0}", resourcePath));

      if (!File.Exists(resourcePath))
        throw new BadRequestException(string.Format("GetHtmlResource: File doesn't exist! Requested Path: {0}", resourcePath));

      Logger.Debug("GetHtmlResource: Serving file: {0}", resourcePath);

      // Content
      bool onlyHeaders = context.Request.Method == "HEAD";
      context.Response.ContentType = MimeTypeDetector.GetMimeTypeFromExtension(Path.GetFileName(resourcePath));
      Stream resourceStream = File.OpenRead(resourcePath);
      await SendWholeFileAsync(context, resourceStream, onlyHeaders);
      resourceStream.Close();

      return true;
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
