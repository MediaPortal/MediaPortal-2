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
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "This function is inernally used by the MP2Ext webinterface.")]
  [ApiFunctionParam(Name = "path", Type = typeof(string), Nullable = false)]
  internal class GetHtmlResource : BaseSendData
  {
    /// <summary>
    /// The folder inside the MP2Ext folder where the files are stored
    /// </summary>
    private const string RESOURCE_DIR = "www";
    
    public async Task<bool> ProcessAsync(IOwinContext context, string path)
    {
      string[] uriParts = context.Request.Path.Value.Split('/');
      if (uriParts.Length >= 6)
        path = string.Join("/", uriParts.Skip(5));

      if (path == null)
        throw new BadRequestException("GetHtmlResource: path is null");

      string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (assemblyPath == null)
        throw new BadRequestException("GetHtmlResource: assemblyPath is null");

      string resourceBasePath = Path.Combine(assemblyPath, RESOURCE_DIR);

      string resourcePath = Path.GetFullPath(Path.Combine(resourceBasePath, path));

      if (!resourcePath.StartsWith(resourceBasePath))
        throw new BadRequestException(string.Format("GetHtmlResource: outside home dir! reguested Path: {0}", resourcePath));

      if (!File.Exists(resourcePath))
        throw new BadRequestException(string.Format("GetHtmlResource: File doesn't exist! reguested Path: {0}", resourcePath));

      // Headers

      DateTime lastChanged = File.GetLastWriteTime(resourcePath);

      // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
      if (!string.IsNullOrEmpty(context.Request.Headers["If-Modified-Since"]))
      {
        DateTime lastRequest = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
        if (lastRequest.CompareTo(lastChanged) <= 0)
          context.Response.StatusCode = (int)HttpStatusCode.NotModified;
      }

      // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
      context.Response.Headers["Last-Modified"] = lastChanged.ToUniversalTime().ToString("r");

      // Cache
      context.Response.Headers["Cache-Control"] = "public; max-age=31536000";
      context.Response.Headers["Expires"] = DateTime.Now.AddYears(1).ToString("r");

      // Content
      bool onlyHeaders = true; // httpContext.Request.Method == Method.Header || httpContext.Response.StatusCode == StatusCodes.Status304NotModified;
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
