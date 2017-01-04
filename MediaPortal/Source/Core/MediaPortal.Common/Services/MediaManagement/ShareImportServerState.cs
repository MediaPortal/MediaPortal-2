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

namespace MediaPortal.Common.Services.MediaManagement
{
  public class ShareImportServerState
  {
    public static readonly Guid STATE_ID = new Guid("C37B62D0-8E87-42A4-877E-B41944DA9FC9");

    [XmlArray("Shares", IsNullable = false)]
    [XmlArrayItem("Share")]
    public ShareImportState[] Shares { get; set; }
    [XmlAttribute("IsImporting")]
    public bool IsImporting { get; set; }
    [XmlAttribute("Progress")]
    public int Progress { get; set; }
  }

  public class ShareImportState
  {
    public Guid ShareId { get; set; }
    public bool IsImporting { get; set; }
    public int Progress { get; set; }
  }
}
