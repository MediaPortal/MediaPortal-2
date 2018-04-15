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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Backend.ClientCommunication
{
  /// <summary>
  /// Interface of the MP2 client's ClientController service.
  /// This service controls several functions of an MP2 client which can be called via UPnP.
  /// </summary>
  public interface IClientController
  {
    string GetHomeServerSystemId();
    void ImportLocation(ResourcePath path, IEnumerable<string> mediaCategories, ImportJobType importJobType);
  }
}
