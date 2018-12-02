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

using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;
using System;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Base player model that can show/hide an OSD.
  /// </summary>
  public abstract class BaseOSDPlayerModel : BasePlayerModel
  {
    protected DateTime _lastOSDMouseUsageTime = DateTime.MinValue;
    protected bool _isOsdOpenOnDemand;

    protected AbstractProperty _isOSDVisibleProperty;

    public BaseOSDPlayerModel(Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
      : base(currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId)
    {
      _isOSDVisibleProperty = new WProperty(typeof(bool), false);
    }

    protected override void Update()
    {
      UpdateOSDVisibilty();
    }

    protected virtual void UpdateOSDVisibilty()
    {
      //Don't show the OSD if there is a dialog visible
      var sm = ServiceRegistration.Get<IScreenManager>();
      if (sm.IsDialogVisible)
      {
        IsOSDVisible = false;
        return;
      }

      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      //Only show the OSD on mouse usage if the mouse has been used again since the last time it was closed.
      if (!_isOsdOpenOnDemand && inputManager.LastMouseUsageTime > _lastOSDMouseUsageTime)
        IsOSDVisible = inputManager.IsMouseUsed;
    }

    /// <summary>
    /// Should be called when closing the OSD to set the last time the mouse
    /// was used whilst the OSD was open to avoid immediately reopening it again.
    /// </summary>
    /// <remarks>
    /// The OSD gets shown automatically on mouse usage but if we've deliberately
    /// closed it with the mouse, and therefore the mouse is being used,
    /// we don't want it to open again. We store the last mouse usage when closing
    /// and only show the OSD if the mouse has been used again since closing the OSD.
    /// </remarks>
    protected void SetLastOSDMouseUsageTime()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      //Add a second to allow for mouse movement just after closing
      _lastOSDMouseUsageTime = inputManager.LastMouseUsageTime.AddSeconds(1);
    }

    #region Members to be accessed from the GUI

    public AbstractProperty IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDVisible
    {
      get { return (bool)_isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    public virtual void ToggleOSD()
    {
      IsOSDVisible = !IsOSDVisible;
      if (!IsOSDVisible)
        SetLastOSDMouseUsageTime();
      _isOsdOpenOnDemand = IsOSDVisible;
    }

    public virtual void CloseOSD()
    {
      IsOSDVisible = false;
      _isOsdOpenOnDemand = false;
      SetLastOSDMouseUsageTime();
    }

    #endregion
  }
}
