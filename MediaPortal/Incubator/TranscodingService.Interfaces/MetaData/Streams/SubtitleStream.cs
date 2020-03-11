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

using System.IO;
using System.Net;
using MediaPortal.Common.ResourceAccess;
using Newtonsoft.Json;

namespace MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams
{
  public class SubtitleStream
  {
    public SubtitleCodec Codec { get; set; } = SubtitleCodec.Unknown;
    public int StreamIndex { get; set; } = -1;
    public string Language { get; set; }
    public string SourcePath { get; set; }
    public string CharacterEncoding { get; set; } = "";
    public bool Default { get; set; } = false;

    public string GetFileSystemPath()
    {
      if (!string.IsNullOrEmpty(SourcePath))
      {
        var systemPath = LocalFsResourceProviderBase.ToDosPath(SourcePath);
        if (File.Exists(systemPath))
          return systemPath;

        var path = ResourcePath.Deserialize(SourcePath);
        if (path.BasePathSegment.ProviderId == LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID)
        {
          return LocalFsResourceProviderBase.ToDosPath(path);
        }
      }

      return null;
    }

    [JsonIgnore]
    public bool IsEmbedded
    {
      get
      {
        if (StreamIndex >= 0) return true;
        return false;
      }
    }

    [JsonIgnore]
    public bool IsPartial { get; set; }
  }
}
