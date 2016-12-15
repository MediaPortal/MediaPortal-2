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


using System.Collections.Generic;
using System.Xml.Serialization;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.Services.MediaManagement
{
  /// <summary>
  /// Represents the status of one <see cref="ImportJobController"/> to make this status serializable.
  /// </summary>
  /// <remarks>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </remarks>
  public class ImportJobNewGen
  {
    public ImportJobNewGen(ImportJobInformation importJobInformation, List<PendingImportResourceNewGen> pendingImportResources)
    {
      ImportJobInformation = importJobInformation;
      PendingImportResources = pendingImportResources;
    }

    public ImportJobNewGen()
    { }

    [XmlElement("JobInfo")]
    public ImportJobInformation ImportJobInformation { get; set; }

    [XmlArray("PendingResources", IsNullable = true)]
    [XmlArrayItem("Resource")]
    public List<PendingImportResourceNewGen> PendingImportResources { get; set; }
  }
}
