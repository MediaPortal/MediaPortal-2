#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Xml.Serialization;

namespace MediaPortal.Core.MediaManagement
{
  /// <summary>
  /// Contains playlist identification data: Name and id.
  /// </summary>
  public abstract class PlaylistBase
  {
    protected Guid _playlistId;
    protected string _name;
    protected string _playlistType;

    protected PlaylistBase(Guid playlistId, string name, string playlistType)
    {
      _playlistId = playlistId;
      _name = name;
      _playlistType = playlistType;
    }

    [XmlIgnore]
    public Guid PlaylistId
    {
      get { return _playlistId; }
    }

    [XmlIgnore]
    public string Name
    {
      get { return _name; }
    }

    [XmlIgnore]
    public string PlaylistType
    {
      get { return _playlistType; }
    }

    #region Additional members for the XML serialization

    internal PlaylistBase() { }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Id")]
    public Guid XML_Id
    {
      get { return _playlistId; }
      set { _playlistId = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Name")]
    public string XML_Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// For internal use of the XML serialization system only.
    /// </summary>
    [XmlAttribute("Type")]
    public string XML_Type
    {
      get { return _playlistType; }
      set { _playlistType = value; }
    }

    #endregion
  }
}