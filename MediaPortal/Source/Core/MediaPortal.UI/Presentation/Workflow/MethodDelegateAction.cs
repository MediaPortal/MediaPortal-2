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
using System.Collections.Generic;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;

namespace MediaPortal.UI.Presentation.Workflow
{
  /// <summary>
  /// Workflow action that calls the specified method when invoked. In contrast to <see cref="MethodCallAction"/>,
  /// the specified to-be-executed method is more general.
  /// </summary>
  public class MethodDelegateAction : WorkflowAction
  {
    protected bool _isEnabled = true;
    protected bool _isVisible = true;
    protected ParameterlessMethod _executor;

    public MethodDelegateAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle,
        ParameterlessMethod executor) :
        this(actionId, name, sourceStateIds, displayTitle, null, executor)
    {
    }

    public MethodDelegateAction(Guid actionId, string name, IEnumerable<Guid> sourceStateIds, IResourceString displayTitle, IResourceString helpText,
        ParameterlessMethod executor) :
        base(actionId, name, sourceStateIds, displayTitle, helpText)
    {
      _executor = executor;
    }

    public void SetEnabled(bool enabled)
    {
      if (_isEnabled == enabled)
        return;
      _isEnabled = enabled;
      FireStateChanged();
    }

    public void SetVisible(bool visible)
    {
      if (_isVisible == visible)
        return;
      _isVisible = visible;
      FireStateChanged();
    }

    #region Overrides of WorkflowAction

    public override bool IsVisible(NavigationContext context)
    {
      return _isVisible;
    }

    public override bool IsEnabled(NavigationContext context)
    {
      return _isEnabled;
    }

    public override void Execute()
    {
      if (_executor != null)
        _executor();
    }

    #endregion
  }
}
