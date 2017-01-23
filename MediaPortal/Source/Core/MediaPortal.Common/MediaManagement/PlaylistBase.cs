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

namespace MediaPortal.Common.MediaManagement
{
  /// <summary>
  /// Base class for some playlist data objects.
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

    /// <summary>
    /// Gets this playlist's id.
    /// </summary>
    /// <remarks>
    /// The id is unique among all playlists.
    /// </remarks>
    [XmlIgnore]
    public Guid PlaylistId
    {
      get { return _playlistId; }
    }

    /// <summary>
    /// Gets or sets this playlist's name.
    /// </summary>
    /// <remarks>
    /// The name should be a human readable name for this playlist. The name doesn't need to be unique.
    /// </remarks>
    [XmlIgnore]
    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    /// <summary>
    /// Gets the number of items in this playlist.
    /// </summary>
    [XmlIgnore]
    public abstract int NumItems { get; }

    /// <summary>
    /// Gets this playlist's type.
    /// </summary>
    /// <remarks>
    /// The playlist type can be used to define the type(s) of media items contained in this playlist. The string is an
    /// application defined string which doesn't need to be in a special format.
    /// </remarks>
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