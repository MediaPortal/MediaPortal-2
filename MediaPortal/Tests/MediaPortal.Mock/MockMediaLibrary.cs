#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using MediaPortal.Backend.Services.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;

namespace MediaPortal.Mock
{
  public class MockMediaLibrary : MediaLibrary
  {
    private IList<Guid> _newMediaItemsIds = new List<Guid>();

    public MockMediaLibrary()
    {
      _miaManagement = MockCore.Management;

      _systemsOnline["mock"] = SystemName.GetLocalSystemName();
    }

    public bool UpdateRelationshipsEnabled { get; set; }

    protected override void Reconcile(Guid mediaItemId, bool isRefresh)
    {
      UpdateRelationships(mediaItemId, true);
    }

    protected override void UpdateRelationships(Guid mediaItemId, bool isRefresh)
    {
      if (UpdateRelationshipsEnabled)
        base.UpdateRelationships(mediaItemId, isRefresh);
      else
        ServiceRegistration.Get<ILogger>().Debug("Update relationships is disabled");
    }

    public void AddMediaItemId(Guid mediaItemId)
    {
      _newMediaItemsIds.Add(mediaItemId);
    }

    protected override Guid NewMediaItemId()
    {
      Guid mediaItemId;
      if (_newMediaItemsIds.Count > 0)
      {
        mediaItemId = _newMediaItemsIds[0];
        _newMediaItemsIds.RemoveAt(0);
      }
      else
      {
        mediaItemId = Guid.NewGuid();
      }

      return mediaItemId;
    }

  }
}
