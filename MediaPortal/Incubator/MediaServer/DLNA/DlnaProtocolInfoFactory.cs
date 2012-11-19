#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.MediaServer.DLNA
{
  public class DlnaProtocolInfoFactory
  {
    public DlnaProtocolInfo GetProtocolInfo(MediaItem item)
    {
      var info = new DlnaProtocolInfo
                   {
                     Protocol = "http-get",
                     Network = "*",
                     MediaType = item.Aspects[MediaAspect.ASPECT_ID]
                       .GetAttributeValue(MediaAspect.ATTR_MIME_TYPE)
                       .ToString(),
                     AdditionalInfo = new DlnaForthField()
                   };

      ConfigureProfile(info.AdditionalInfo, item, info.MediaType);
      return info;
    }

    private static string ConfigureProfile(DlnaForthField dlnaField, MediaItem item, string mediaType)
    {
      //TODO: much better type resolution        
      switch (mediaType)
      {
        case "audio/mpeg":
          dlnaField.ProfileParameter.ProfileName = DlnaProfiles.Mp3;
          dlnaField.FlagsParameter.StreamingMode = true;
          dlnaField.FlagsParameter.InteractiveMode = false;
          dlnaField.FlagsParameter.BackgroundMode = true;
          break;
        case "video/mpeg":
          dlnaField.ProfileParameter.ProfileName = DlnaProfiles.MpegPsPal;
          dlnaField.FlagsParameter.StreamingMode = true;
          dlnaField.FlagsParameter.InteractiveMode = false;
          dlnaField.FlagsParameter.BackgroundMode = true;
          break;
        case "image/jpeg":
          dlnaField.ProfileParameter.ProfileName = DlnaProfiles.JpegLarge;
          dlnaField.FlagsParameter.StreamingMode = false;
          dlnaField.FlagsParameter.InteractiveMode = true;
          dlnaField.FlagsParameter.BackgroundMode = true;
          break;
      }
      return null;
    }

    public static DlnaProtocolInfo GetProfileInfo(MediaItem item)
    {
      var factory = new DlnaProtocolInfoFactory();
      return factory.GetProtocolInfo(item);
    }
  }
}