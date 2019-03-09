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

using System;
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.SystemCommunication
{
  /// <summary>
  /// Interface of the MediaPortal 2 server's ServerController service. This service is implemented by the
  /// MediaPortal 2 server.
  /// </summary>
  public interface IServerController
  {
    void AttachClient(string clientSystemId);
    void DetachClient(string clientSystemId);
    ICollection<MPClientMetadata> GetAttachedClients();
    ICollection<string> GetConnectedClients();
    void ScheduleImports(IEnumerable<Guid> shareIds, ImportJobType importJobType);

    SystemName GetSystemNameForSystemId(string systemId);
  }
}
