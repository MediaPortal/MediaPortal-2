#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.SystemResolver;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.UI.Services.ServerCommunication
{
  public class ImportResultCallback : IImportResultCallback
  {
    protected IContentDirectory _cds;
    protected string _localSystemId;

    public ImportResultCallback()
    {
      _cds = ServiceScope.Get<IServerConnectionManager>().ContentDirectory;
      _localSystemId = ServiceScope.Get<ISystemResolver>().LocalSystemId;
    }

    #region IImportResultCallback implementation

    public void StartImport(ResourcePath path)
    {
      //TODO
    }

    public void EndImport(ResourcePath path)
    {
      //TODO
    }

    public void ImportError(ResourcePath path)
    {
      //TODO
    }

    public void UpdateMediaItem(ResourcePath path, IEnumerable<MediaItemAspect> updatedAspects)
    {
      if (_cds == null)
        throw new IllegalCallException("The MediaPortal server is not connected");
      _cds.AddOrUpdateMediaItem(_localSystemId, path, updatedAspects);
    }

    public void DeleteMediaItem(ResourcePath path)
    {
      if (_cds == null)
        throw new IllegalCallException("The MediaPortal server is not connected");
      _cds.DeleteMediaItemOrPath(_localSystemId, path);
    }

    #endregion
  }
}
