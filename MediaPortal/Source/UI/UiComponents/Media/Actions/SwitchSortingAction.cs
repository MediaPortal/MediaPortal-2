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

using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;

namespace MediaPortal.UiComponents.Media.Actions
{
  public class SwitchSortingAction : IWorkflowContributor
  {
    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return LocalizationHelper.CreateResourceString(Consts.RES_SWITCH_SORTING); }
    }

    public void Initialize()
    {
    }

    public void Uninitialize()
    {
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return MediaNavigationModel.GetNavigationData(context, false) != null;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return true;
    }

    public void Execute()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.DIALOG_SWITCH_SORTING);
    }

    #endregion
  }
}
