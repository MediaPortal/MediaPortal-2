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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.AppLauncher.Models
{
  public class AppLauncherSettingsMenuModel : IWorkflowModel
  {
    public const string MODEL_ID_STR = "488A54AF-8EE5-4E9E-8C91-DE637DDB650C";
    public const int MAX_MENU_APPS = 5;

    private ItemsList _items = new ItemsList();
    private ItemsList _appItems = new ItemsList();
    private Apps _apps = new Apps();
    private string _selectedAppId;

    protected AbstractProperty _selectedItemProperty;

    #region Properties

    public ItemsList Items
    {
      get => _items;
      set => _items = value;
    }

    public ItemsList AppItems
    {
      get => _appItems;
      set => _appItems = value;
    }

    public AbstractProperty SelectedItemProperty
    {
      get { return _selectedItemProperty; }
    }

    public ListItem SelectedItem
    {
      get { return (ListItem)_selectedItemProperty.GetValue(); }
      set { _selectedItemProperty.SetValue(value); }
    }

    #endregion

    public void DeleteSelectedMenuMapping()
    {
      if (SelectedItem != null)
      {
        var selectedItem = SelectedItem;
        var appNumber = (int)selectedItem.AdditionalProperties[Consts.KEY_MENU];

        foreach (var app in _apps.AppsList.Where(a => a.MenuNumber == appNumber).ToList())
          app.MenuNumber = 0;
        Helper.SaveApps(_apps);

        selectedItem.SetLabel(Consts.KEY_APP, "");
        selectedItem.SetLabel(Consts.KEY_ICON, "");
        selectedItem.AdditionalProperties[Consts.KEY_ID] = "";
        selectedItem.FireChange();
      }
    }

    public void SelectMenuApp(ListItem item)
    {
      _selectedAppId = (string)item.AdditionalProperties[Consts.KEY_ID];

      // Close the Dialog
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    /// <summary>
    /// Read the Applications from MP Registration
    /// </summary>
    private void Init()
    {
      Clear();
      _apps = Helper.LoadApps(true);
      FillItems();
      FillAppItems();
    }

    private void FillItems()
    {
      _items.Clear();
      for (int appNumber = 1; appNumber <= MAX_MENU_APPS; appNumber++)
      {
        var app = _apps.AppsList.FirstOrDefault(a => a.MenuNumber == appNumber);
        var listItem = new ListItem(Consts.KEY_NAME, $"{LocalizationHelper.Translate(Consts.RES_MENU)} \"{appNumber}\"");
        listItem.Command = new MethodDelegateCommand(() => ChooseApp(listItem));
        listItem.SetLabel(Consts.KEY_APP, app?.ShortName ?? "");
        listItem.SetLabel(Consts.KEY_ICON, app?.IconPath ?? "");
        listItem.AdditionalProperties[Consts.KEY_ID] = app?.Id.ToString() ?? "";
        listItem.AdditionalProperties[Consts.KEY_MENU] = appNumber;
        _items.Add(listItem);
      }
      _items.FireChange();
    }

    private void FillAppItems()
    {
      _appItems.Clear();
      foreach (var a in _apps.AppsList)
      {
        var item = new ListItem();
        item.AdditionalProperties[Consts.KEY_ID] = Convert.ToString(a.Id);
        item.SetLabel(Consts.KEY_NAME, a.ShortName);
        item.SetLabel(Consts.KEY_ICON, a.IconPath);
        Items.Add(item);
      }
      _appItems.FireChange();
    }

    private void ChooseApp(ListItem item)
    {
      _selectedAppId = null;
      var dlgHandle = ServiceRegistration.Get<IScreenManager>().ShowDialog("DlgAppLauncherMenuApps", (s, g) =>
      {
        try
        {
          if (!string.IsNullOrEmpty(_selectedAppId))
          {
            var appNumber = (int)item.AdditionalProperties[Consts.KEY_MENU];
            var app = _apps.AppsList.FirstOrDefault(a => a.Id.ToString().Equals(_selectedAppId, StringComparison.InvariantCultureIgnoreCase));
            if (app != null)
            {
              foreach (var conflictApp in _apps.AppsList.Where(a => a.MenuNumber == appNumber).ToList())
                conflictApp.MenuNumber = 0;
              app.MenuNumber = appNumber;

              item.SetLabel(Consts.KEY_APP, app.ShortName);
              item.SetLabel(Consts.KEY_ICON, app.IconPath);
              item.AdditionalProperties[Consts.KEY_ID] = _selectedAppId;
              item.FireChange();
            }
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("Error selecting App", ex);
        }
      });
    }

    private void Clear()
    {
      _items.Clear();
      _appItems.Clear();
      _apps = null;
    }

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
