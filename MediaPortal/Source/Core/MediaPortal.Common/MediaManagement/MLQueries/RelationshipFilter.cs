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
using System.Xml.Serialization;

namespace MediaPortal.Common.MediaManagement.MLQueries
{
  /// <summary>
  /// Filter which filters based on the relationship
  /// </summary>
  public class RelationshipFilter : AbstractRelationshipFilter
  {
    protected Guid _linkedRole;
    protected Guid _linkedMediaItemId;

    public RelationshipFilter(Guid role, Guid linkedRole, Guid linkedMediaItemId) :
      base(role)
    {
      _linkedRole = linkedRole;
      _linkedMediaItemId = linkedMediaItemId;
    }

    [XmlIgnore]
    public Guid LinkedMediaItemId
    {
      get { return _linkedMediaItemId; }
      set { _linkedMediaItemId = value; }
    }

    [XmlIgnore]
    public Guid LinkedRole
    {
      get { return _linkedRole; }
      set { _linkedRole = value; }
    }

    public override string ToString()
    {
      return "(LINKED_ID = '" + _linkedMediaItemId + "'" + (_role != Guid.Empty ? " AND ROLE='" + _role + "'" : "") + " AND LINKED_ROLE='" + _linkedRole + "')";
    }

    #region Additional members for the XML serialization

    internal RelationshipFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    public Guid XML_LinkedId
    {
      get { return _linkedMediaItemId; }
      set { _linkedMediaItemId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("LinkedRole")]
    public Guid XML_LinkedRole
    {
      get { return _linkedRole; }
      set { _linkedRole = value; }
    }

    #endregion
  }
}
