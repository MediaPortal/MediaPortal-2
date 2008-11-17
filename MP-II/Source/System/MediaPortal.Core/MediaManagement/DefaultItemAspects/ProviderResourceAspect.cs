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
  /// Contains the metadata of the "ProviderResource" media item aspect which is assigned to all resource files
  /// provided by some media provider.
  /// </summary>
  public static class ProviderResourceAspect
  {
    public static Guid ASPECT_ID = new Guid("{0A296ACD-F95B-4a28-90A2-E4FD2A4CC4ED}");
    public static string ATTR_SOURCE_COMPUTER = "Source-Computer";
    public static string ATTR_PROVIDER_ID = "Provider-ID";
    public static string ATTR_PATH = "Path";

    public static MediaItemAspectMetadata Metadata = new MediaItemAspectMetadata(
        // TODO: Localize name
        ASPECT_ID, "ProviderResource", new[] {
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_SOURCE_COMPUTER, typeof(string), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_PROVIDER_ID, typeof(string), false),
            MediaItemAspectMetadata.CreateAttributeSpecification(ATTR_PATH, typeof(string), false),
  });
}

}
