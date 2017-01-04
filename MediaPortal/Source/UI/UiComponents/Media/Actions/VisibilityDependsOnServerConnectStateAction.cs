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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class VisibilityDependsOnServerConnectStateAction : BaseTrackServerConnectionAction
  {
    #region Protected fields

    protected readonly bool _visibleOnServerConnect;
    protected readonly Guid? _targetWorkflowStateId;
    protected readonly IResourceString _displayTitle;

    // This is the only attribute to be updated so we can optimize using volatile instead of using a lock
    protected volatile bool _isVisible;

    #endregion

    public VisibilityDependsOnServerConnectStateAction(bool visibleOnServerConnect, Guid? targetWorkflowStateId, string displayTitleResource)
    {
      _visibleOnServerConnect = visibleOnServerConnect;
      _targetWorkflowStateId = targetWorkflowStateId;
      _displayTitle = LocalizationHelper.CreateResourceString(displayTitleResource);
    }

    protected override void Update()
    {
      base.Update();
      bool lastVisible = _isVisible;
      _isVisible = (_isHomeServerConnected ^ !_visibleOnServerConnect) && IsVisibleOverride;
      if (lastVisible != _isVisible)
        FireStateChanged();
    }

    protected virtual bool IsVisibleOverride
    {
      get { return true; }
    }

    #region IWorkflowContributor implementation

    public override IResourceString DisplayTitle
    {
      get { return _displayTitle; }
    }

    public override bool IsActionVisible(NavigationContext context)
    {
      return _isVisible;
    }

    public override bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public override void Execute()
    {
      if (!_targetWorkflowStateId.HasValue)
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(_targetWorkflowStateId.Value);
    }

    #endregion
  }
}
