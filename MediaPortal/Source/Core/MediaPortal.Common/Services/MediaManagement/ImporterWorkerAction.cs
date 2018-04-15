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
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;

namespace MediaPortal.Common.Services.MediaManagement
{
  class ImporterWorkerAction
  {
    public enum ActionType
    {
      Startup,
      Activate,
      ScheduleImport,
      CancelImport,
      Suspend,
      Shutdown
    }

    private readonly ActionType _actionType;
    private readonly ImportJobInformation? _importJobInformation;
    private readonly IMediaBrowsing _mediaBrowsingCallback;
    private readonly IImportResultHandler _importResultHandler;
    private readonly TaskCompletionSource<object> _tcs;

    public ImporterWorkerAction(ActionType actionType)
    {
      if (actionType == ActionType.Activate)
        throw new ArgumentException("ActionType.Activate requires an IMediaBrowsing and an IImportResultHandler");
      if (actionType == ActionType.ScheduleImport)
        throw new ArgumentException("ActionType.ScheduleImport requires an ImportJobInformation");
      _actionType = actionType;
      _importJobInformation = null;
      _mediaBrowsingCallback = null;
      _importResultHandler = null;
      _tcs = new TaskCompletionSource<object>();
    }

    public ImporterWorkerAction(ActionType actionType, ImportJobInformation importJobInformation)
    {
      if (actionType == ActionType.Startup)
        throw new ArgumentException("ActionType.Startup must not relate to an ImportJobInformation");
      if (actionType == ActionType.Activate)
        throw new ArgumentException("ActionType.Activate must not relate to an ImportJobInformation and requires an IMediaBrowsing and an IImportResultHandler");
      if (actionType == ActionType.Suspend)
        throw new ArgumentException("ActionType.Suspend must not relate to an ImportJobInformation");
      if (actionType == ActionType.Shutdown)
        throw new ArgumentException("ActionType.Shutdown must not relate to an ImportJobInformation");
      _actionType = actionType;
      _importJobInformation = importJobInformation;
      _mediaBrowsingCallback = null;
      _importResultHandler = null;
      _tcs = new TaskCompletionSource<object>();
    }

    public ImporterWorkerAction(ActionType actionType, IMediaBrowsing mediaBrowsingCallback, IImportResultHandler importResultHandler)
    {
      if (actionType == ActionType.Startup)
        throw new ArgumentException("ActionType.Startup must not relate to an IMediaBrowsing and an IImportResultHandler");
      if (actionType == ActionType.ScheduleImport)
        throw new ArgumentException("ActionType.ScheduleImport must not relate to an IMediaBrowsing and an IImportResultHandler and requires an ImportJobInformation");
      if (actionType == ActionType.CancelImport)
        throw new ArgumentException("ActionType.CancelImport must not relate to an IMediaBrowsing and an IImportResultHandler");
      if (actionType == ActionType.Suspend)
        throw new ArgumentException("ActionType.Suspend must not relate to an IMediaBrowsing and an IImportResultHandler");
      if (actionType == ActionType.Shutdown)
        throw new ArgumentException("ActionType.Shutdown must not relate to an IMediaBrowsing and an IImportResultHandler");
      _actionType = actionType;
      _importJobInformation = null;
      _mediaBrowsingCallback = mediaBrowsingCallback;
      _importResultHandler = importResultHandler;
      _tcs = new TaskCompletionSource<object>();
    }

    public ActionType Type
    {
      get { return _actionType; }
    }

    public ImportJobInformation? JobInformation
    {
      get { return _importJobInformation; }
    }

    public IMediaBrowsing MediaBrowsingCallback
    {
      get { return _mediaBrowsingCallback; }
    }

    public IImportResultHandler ImportResultHandler
    {
      get { return _importResultHandler; }
    }

    public Task Completion
    {
      get { return _tcs.Task; }
    }

    public void Complete()
    {
      _tcs.SetResult(null);
    }

    public void Fault(Exception ex)
    {
      _tcs.SetException(ex);
    }

  }
}
