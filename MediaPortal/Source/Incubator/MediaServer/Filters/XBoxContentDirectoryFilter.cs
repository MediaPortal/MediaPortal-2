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

using MediaPortal.Plugins.MediaServer.Objects.Basic;

namespace MediaPortal.Plugins.MediaServer.Filters
{
  public class XBoxContentDirectoryFilter : GenericContentDirectoryFilter
  {
    //Music Object ID = 1, Root ID = 0
    //  Album Object ID = 7, Root ID = 1
    //  All Music Object ID = 4, Root ID = 1
    //  Artist Object ID = 6, Root ID = 1
    //  Folders Object ID = 14, Root ID = 1
    //  Genre Object ID = 5, Root ID = 1
    //  Playlist Object ID = F, Root ID = 1
    //Pictures Object ID = 3, Root ID = 0
    //  Album Object ID = D, Root ID = 3
    //  All Pictures Object ID = B, Root ID = 3
    //  Date Taken Object ID = C, Root ID = 3
    //  Folders Object ID = 16, Root ID = 3
    //  Playlist Object ID = 11, Root ID = 3
    //Playlists Object ID = 12, Root ID = 0
    //  All Playlists Object ID = 13, Root ID = 12
    //Folders Object ID = 17, Root ID = 0
    //Video Object ID = 2, Root ID = 0
    //  Actor Object ID = A, Root ID = 2
    //  Album Object ID = E, Root ID = 2
    //  All Video Object ID = 8, Root ID = 2
    //  Folders Object ID = 15, Root ID = 2
    //  Genre Object ID = 9, Root ID = 2
    //  Playlist Object ID = 10, Root ID = 2

    public override string FilterObjectId(string requestedNodeId, bool isSearch)
    {
      if (requestedNodeId.Equals("2"))
        return "V";
      if (requestedNodeId.Equals("3"))
        return "I";
      if (requestedNodeId.Equals("1"))
        return "A";

      if (isSearch)
      {
        //TODO: Specify ids once defined

        //if (requestedNodeId.Equals("4"))
        //  return "AAS";
        if (requestedNodeId.Equals("5"))
          return "AG";
        //if (requestedNodeId.Equals("6"))
        //  return "AAA";
        if (requestedNodeId.Equals("7"))
          return "AA";
        if (requestedNodeId.Equals("14"))
          return "AAS";
        //if (requestedNodeId.Equals("F"))
        //  return "APL";

        //if (requestedNodeId.Equals("D"))
        //  return "IA";
        //if (requestedNodeId.Equals("B"))
        //  return "IAI";
        //if (requestedNodeId.Equals("C"))
        //  return "IY";
        if (requestedNodeId.Equals("16"))
          return "IIS";
        //if (requestedNodeId.Equals("11"))
        //  return "IPL";

        //if (requestedNodeId.Equals("A"))
        //  return "VA";
        //if (requestedNodeId.Equals("8"))
        //  return "VAV";
        if (requestedNodeId.Equals("9"))
          return "VG";
        if (requestedNodeId.Equals("15"))
          return "VS";
        //if (requestedNodeId.Equals("10"))
        //  return "VPL";

        if (requestedNodeId.Equals("17"))
          return "M";
      }
      else
      {
        if (requestedNodeId.Equals("14"))
        {
          return "A";
        }
        if (requestedNodeId.Equals("15"))
        {
          return "V";
        }
        if (requestedNodeId.Equals("16"))
        {
          return "I";
        }
        if (requestedNodeId.Equals("17"))
        {
          return "M";
        }
      }

      return requestedNodeId;
    }

    public override void FilterContainerClassType(string objectId, ref BasicObject container)
    {
      if (container is BasicContainer)
      {
        if (container != null && objectId.StartsWith("A") == false && container.Class.StartsWith("object.container"))
        {
          container.Class = "object.container.storageFolder";
        }
      }
    }

    public override void FilterClassProperties(string objectId, ref BasicObject container)
    {
      if (container is BasicContainer)
      {
        if (objectId.StartsWith("A") || objectId.Equals("I"))
        {
          ((BasicContainer)container).Searchable = true;
        }
        else
        {
          ((BasicContainer)container).Searchable = false;
        }
      }
    }
  }
}
