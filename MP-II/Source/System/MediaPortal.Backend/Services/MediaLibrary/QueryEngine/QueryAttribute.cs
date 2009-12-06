#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Backend.Services.MediaLibrary.QueryEngine
{
  /// <summary>
  /// Identifies a media item aspect attribute which is queried in a special role. We need a special wrapping object for the
  /// contained attribute type because an attribute type might be requested multiple times in the same query, each queried
  /// on a different query table instance.
  /// </summary>
  public class QueryAttribute
  {
    protected readonly MediaItemAspectMetadata.AttributeSpecification _attr;

    public QueryAttribute(MediaItemAspectMetadata.AttributeSpecification attr)
    {
      _attr = attr;
    }

    public MediaItemAspectMetadata.AttributeSpecification Attr
    {
      get { return _attr; }
    }
  }
}
