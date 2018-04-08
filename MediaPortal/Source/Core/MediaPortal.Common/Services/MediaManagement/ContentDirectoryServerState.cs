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

using MediaPortal.Common.MediaManagement;
using System;
using System.Xml.Serialization;

namespace MediaPortal.Common.Services.MediaManagement
{
  public class ContentDirectoryServerState
  {
    public static readonly Guid STATE_ID = new Guid("1B9171AF-C53C-4761-942C-77C3BC1FAE2D");

    [XmlAttribute("SystemId")]
    public string SystemId { get; set; }
    [XmlAttribute("ChangeType")]
    public ContentDirectoryMessaging.MediaItemChangeType ChangeType { get; set; }
    [XmlAttribute("MediaItemPath")]
    public string MediaItemPath { get; set; }
    [XmlAttribute("OldPath")]
    public string OldPath { get; set; }
  }
}
