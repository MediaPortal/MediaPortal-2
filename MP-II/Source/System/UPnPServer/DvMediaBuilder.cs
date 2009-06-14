#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections;
using Intel.UPNP.AV.MediaServer.DV;
using Intel.UPNP.AV.CdsMetadata;

namespace Components.UPnPServer
{
  public class DvMediaBuilder2
  {
    // Methods
    public static IList BuildMediaBranches(string DidlLiteXml)
    {
      IList list = MediaBuilder.BuildMediaBranches(DidlLiteXml, typeof(DvMediaItem2), typeof(DvMediaContainer2));
      foreach (IDvMedia media in list)
      {
        EnableMetadataTracking(media);
      }
      return list;
    }

    public static DvMediaContainer2 CreateContainer(MediaBuilder.container info)
    {
      DvMediaContainer2 container = new DvMediaContainer2();
      MediaBuilder.SetObjectProperties(container, info);
      container.TrackMetadataChanges = true;
      return container;
    }

    public static DvMediaItem2 CreateItem(MediaBuilder.item info)
    {
      DvMediaItem2 item = new DvMediaItem2();
      MediaBuilder.SetObjectProperties(item, info);
      item.TrackMetadataChanges = true;
      return item;
    }

    internal static DvRootContainer2 CreateRoot(MediaBuilder.container info)
    {
      info.ID = "0";
      info.IdIsValid = true;
      DvRootContainer2 container = new DvRootContainer2();
      MediaBuilder.SetObjectProperties(container, info);
      container.TrackMetadataChanges = true;
      return container;
    }

    private static void EnableMetadataTracking(IDvMedia dvm)
    {
      DvMediaContainer2 container = dvm as DvMediaContainer2;
      DvMediaItem2 item = dvm as DvMediaItem2;
      if (container != null)
      {
        container.TrackMetadataChanges = true;
        foreach (IDvMedia media in container.CompleteList)
        {
          EnableMetadataTracking(media);
        }
      }
      else if (item != null)
      {
        item.TrackMetadataChanges = true;
      }
    }
  }


}
