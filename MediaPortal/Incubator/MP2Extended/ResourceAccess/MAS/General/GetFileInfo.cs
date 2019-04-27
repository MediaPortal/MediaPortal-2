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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MP2Extended.Extensions;
using Microsoft.Owin;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetFileInfo : BaseSendData
  {
    public static Task<WebFileInfo> ProcessAsync(IOwinContext context, WebMediaType mediatype, WebFileType filetype, string id, int offset)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      MediaItem item = MediaLibraryAccess.GetMediaItemById(context, id, necessaryMIATypes, null);
      if (item == null)
        throw new BadRequestException("GetFileInfo: no media item found");

      var files = ResourceAccessUtils.GetResourcePaths(item);
      var ra = GetResourceAccessor(files[offset]);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetFileInfo: failed to create IFileSystemResourceAccessor");

      WebFileInfo webFileInfo = new WebFileInfo
      {
        Exists = fsra.Exists,
        Extension = fsra.Path.Split('.').Last(),
        IsLocalFile = true,
        IsReadOnly = true,
        LastAccessTime = DateTime.Now,
        LastModifiedTime = fsra.LastChanged,
        Name = fsra.ResourceName,
        OnNetworkDrive = false,
        Path = files[offset].FileName,
        Size = fsra.Size,
      };
      return Task.FromResult(webFileInfo);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
