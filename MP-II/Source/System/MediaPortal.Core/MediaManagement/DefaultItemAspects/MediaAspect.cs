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

using System;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Core.MediaManagement.DefaultItemAspects
{
  /// <summary>
  /// Contains the metadata of the "Media" media item aspect which is assigned to all media items.
  /// </summary>
  public static class MediaAspect
  {
    public static Guid ASPECT_ID = new Guid("{A01B7D6E-A6F2-434b-AC12-49D7D5CBD377}");
    public static string ATTR_TITLE = "Title";
    public static string ATTR_ENCODING = "Encoding";
    public static string ATTR_DATE = "Recording time";
    public static string ATTR_RATING = "Rating";

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "MediaItem", new[] {
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_TITLE, typeof(string), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_ENCODING, typeof(string), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_DATE, typeof(DateTime), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_RATING, typeof(int), false),
  });
}

}
