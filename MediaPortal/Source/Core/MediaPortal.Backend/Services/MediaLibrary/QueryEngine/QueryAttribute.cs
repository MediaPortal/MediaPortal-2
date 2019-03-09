#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common.MediaManagement;

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

    public override bool Equals(object obj)
    {
        if (!(obj is QueryAttribute))
            return false;

        QueryAttribute qa = (QueryAttribute)obj;
        return Attr.Equals(qa.Attr);
    }

    protected bool Equals(QueryAttribute other)
    {
      return Equals(_attr, other._attr);
    }

    public override int GetHashCode()
    {
      return (_attr != null ? _attr.GetHashCode() : 0);
    }

    public override string ToString()
    {
      return _attr.ParentMIAM.Name + "." + _attr.AttributeName;
    }
  }
}
