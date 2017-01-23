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
  /// Base class for filters that link items by role
  /// </summary>
  public abstract class AbstractRelationshipFilter : IFilter
  {
    protected Guid _role;

    public AbstractRelationshipFilter(Guid role)
    {
      _role = role;
    }

    [XmlIgnore]
    public Guid Role
    {
      get { return _role; }
      set { _role = value; }
    }

    #region Additional members for the XML serialization

    internal AbstractRelationshipFilter() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Role")]
    public Guid XML_Role
    {
      get { return _role; }
      set { _role = value; }
    }

    #endregion
  }
}
