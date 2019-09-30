#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.AppLauncher.Models
{
  public class AppLauncherRemoveGroupModel : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "3C38DF86-AE80-4411-8C3D-9480E7AAB279";

    #endregion

    #region Vars

    private ItemsList _items = new ItemsList();
    private Apps _apps;

    #endregion

    #region Properties

    public ItemsList Items
    {
      get => _items;
      set => _items = value;
    }

    #endregion

    #region Public Methods

    public void Select(ListItem item)
    {
      item.Selected = item.Selected != true;
      item.FireChange();
    }

    public void Delete()
    {
      foreach (var a in _items.Where(item => item.Selected).SelectMany(item => _apps.AppsList.Where(a => a.Group == (string)item.AdditionalProperties[Consts.KEY_GROUP])))
      {
        a.Group = "";
      }
      Helper.SaveApps(_apps);

      // Close the Dialog
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    #endregion

    #region Private Methods

    private void Init()
    {
      Clear();
      _apps = Helper.LoadApps();
      var groups = new List<string>();

      _items.Clear();

      foreach (var a in _apps.AppsList.Where(a => !groups.Contains(a.Group) & a.Group != ""))
      {
        groups.Add(a.Group);
        var item = new ListItem();
        item.AdditionalProperties[Consts.KEY_GROUP] = a.Group;
        item.SetLabel(Consts.KEY_NAME, a.Group);
        _items.Add(item);
      }
      _items.FireChange();
    }

    private void Clear()
    {
      _items.Clear();
      _apps?.AppsList?.Clear();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Init();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Clear();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Todo: select any or the Last ListItem
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
