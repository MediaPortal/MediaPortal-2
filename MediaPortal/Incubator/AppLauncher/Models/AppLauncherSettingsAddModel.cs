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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Utilities;
using MediaPortal.UI.Presentation.Workflow;
using Microsoft.Win32;

namespace MediaPortal.Plugins.AppLauncher.Models
{
  public class AppLauncherSettingsAddModel : IWorkflowModel
  {
    #region Const

    public const string MODEL_ID_STR = "D47E22A3-3D0F-4A28-8EF6-1121B811508C";
    public const string VIEW_NOW = "[AppLauncher.Settings.Add.NoWindow]";
    public const string RES_ICO = "[AppLauncher.Settings.Add.SearchIcon]";
    public const string RES_APP = "[AppLauncher.Settings.Add.SearchApp]";

    #endregion

    #region Private Fields

    private AbstractProperty _asAdmin = new WProperty(typeof(bool), true);
    private AbstractProperty _shortName = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _appPath = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _arguments = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _group = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _description = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _username = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _password = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _iconPath = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _windowStyleString = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _maxStringProperty = new WProperty(typeof(string), string.Empty);
    private ItemsList _appItems = new ItemsList();
    private ItemsList _groupItems = new ItemsList();
    private PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;
    private Apps _apps = new Apps();
    private ProcessWindowStyle _windowStyle = ProcessWindowStyle.Maximized;
    private string _fallback = "no-icon.png";

    #endregion

    #region Properties

    public AbstractProperty AsAdminProperty
    {
      get { return _asAdmin; }
    }
    public bool AsAdmin
    {
      get { return (bool)_asAdmin.GetValue(); }
      set { _asAdmin.SetValue(value); }
    }

    public AbstractProperty ShortNameProperty
    {
      get { return _shortName; }
    }
    public string ShortName
    {
      get { return (string)_shortName.GetValue(); }
      set { _shortName.SetValue(value); }
    }

    public AbstractProperty AppPathProperty
    {
      get { return _appPath; }
    }
    public string AppPath
    {
      get { return (string)_appPath.GetValue(); }
      set { _appPath.SetValue(value); }
    }

    public AbstractProperty ArgumentsProperty
    {
      get { return _arguments; }
    }
    public string Arguments
    {
      get { return (string)_arguments.GetValue(); }
      set { _arguments.SetValue(value); }
    }

    public AbstractProperty GroupProperty
    {
      get { return _group; }
    }
    public string Group
    {
      get { return (string)_group.GetValue(); }
      set { _group.SetValue(value); }
    }

    public AbstractProperty DescriptionProperty
    {
      get { return _description; }
    }
    public string Description
    {
      get { return (string)_description.GetValue(); }
      set { _description.SetValue(value); }
    }

    public AbstractProperty UsernameProperty
    {
      get { return _username; }
    }
    public string Username
    {
      get { return (string)_username.GetValue(); }
      set { _username.SetValue(value); }
    }

    public AbstractProperty PasswordProperty
    {
      get { return _password; }
    }
    public string Password
    {
      get { return (string)_password.GetValue(); }
      set { _password.SetValue(value); }
    }

    public AbstractProperty IconPathProperty
    {
      get { return _iconPath; }
    }
    public string IconPath
    {
      get { return (string)_iconPath.GetValue(); }
      set { _iconPath.SetValue(value); }
    }

    public AbstractProperty WindowStyleProperty
    {
      get { return _windowStyleString; }
    }
    public string WindowStyle
    {
      get { return (string)_windowStyleString.GetValue(); }
      set { _windowStyleString.SetValue(value); }
    }

    /// <summary>
    /// Hold the Length of the largest Path (Only to view the Dialog in one width)
    /// </summary>
    public AbstractProperty MaxStringProperty
    {
      get { return _maxStringProperty; }
    }
    public string MaxString
    {
      get { return (string)_maxStringProperty.GetValue(); }
      set { _maxStringProperty.SetValue(value); }
    }

    public ItemsList InstalledAppItems
    {
      get => _appItems;
      set => _appItems = value;
    }

    public ItemsList GroupItems
    {
      get => _groupItems;
      set => _groupItems = value;
    }

    #endregion

    private void Init()
    {
      Clear(true);
      _apps = Helper.LoadApps(true);
      InitInstalledApps();
      InitGroups();

      // If the Call comes from the Edit page
      if (AppLauncherSettingsEditModel.CurrentApp != null)
      {
        ShortName = AppLauncherSettingsEditModel.CurrentApp.ShortName;
        Arguments = AppLauncherSettingsEditModel.CurrentApp.Arguments;
        AppPath = AppLauncherSettingsEditModel.CurrentApp.ApplicationPath;
        Description = AppLauncherSettingsEditModel.CurrentApp.Description;
        IconPath = AppLauncherSettingsEditModel.CurrentApp.IconPath;
        Password = AppLauncherSettingsEditModel.CurrentApp.Password;
        Username = AppLauncherSettingsEditModel.CurrentApp.Username;
        WindowStyle = AppLauncherSettingsEditModel.CurrentApp.WindowStyle.ToString();
        Group = AppLauncherSettingsEditModel.CurrentApp.Group;
      }

      Maximum();
    }

    private void InitInstalledApps()
    {
      // Read the Software key from Registry (only for installed Software)
      var rKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths");

      if (rKey == null) return;
      var sKeyNames = rKey.GetSubKeyNames();

      // Loop over all Keys
      foreach (var sKeyName in sKeyNames)
      {
        var sKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + sKeyName);

        if (sKey == null) continue;

        // Read the Application path
        var path = (string)sKey.GetValue("", "", RegistryValueOptions.None);

        if (path == null | !File.Exists(path)) continue;

        // only executable Files
        if (!path.EndsWith(".exe")) continue;

        // Set the max length for Dialog width
        if (path.Length > MaxString.Length)
          MaxString = path;

        // Fill the List with Items
        var item = new ListItem();
        item.AdditionalProperties[Consts.KEY_NAME] = Path.GetFileNameWithoutExtension(sKeyName);
        item.AdditionalProperties[Consts.KEY_PATH] = path;
        item.SetLabel(Consts.KEY_NAME, Path.GetFileNameWithoutExtension(sKeyName));

        // Extract the Icon
        var icon = Icon.ExtractAssociatedIcon(path);

        if (icon != null)
        {
          // Check if Icon already exists and save it if not
          string iconPath = Helper.GetIconPath(sKeyName, icon.ToBitmap());
          item.SetLabel(Consts.KEY_ICON, iconPath);
          item.AdditionalProperties[Consts.KEY_ICON_PATH] = iconPath;
        }
        _appItems.Add(item);
      }
    }

    private void InitGroups()
    {
      _groupItems.Clear();
      var groups = new List<string>();

      var item = new ListItem();
      item.AdditionalProperties[Consts.KEY_GROUP] = "";
      item.SetLabel(Consts.KEY_NAME, Consts.RES_UNGROUPED);
      foreach (var a in _apps.AppsList)
      {
        if (!groups.Contains(a.Group) && a.Group != "")
        {
          groups.Add(a.Group);
          item = new ListItem();
          item.AdditionalProperties[Consts.KEY_GROUP] = a.Group;
          item.SetLabel(Consts.KEY_NAME, a.Group);
          _groupItems.Add(item);
        }
      }
    }

    public void SelectInstalledApp(ListItem item)
    {
      // Added the selected Application to the Screen
      AppPath = (string)item.AdditionalProperties[Consts.KEY_PATH];
      IconPath = (string)item.AdditionalProperties[Consts.KEY_ICON_PATH];
      if (ShortName == "")
        ShortName = (string)item.AdditionalProperties[Consts.KEY_NAME];

      // Close the Dialog
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    public void SelectGroup(ListItem item)
    {
      // Added the selected Application to the Screen
      Group = (string)item.AdditionalProperties[Consts.KEY_GROUP];

      // Close the Dialog
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
    }

    public void SearchApp()
    {
      string initialPath = "C:\\";
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(RES_APP, true, false,
        string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
        path =>
        {
          string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
          if (string.IsNullOrEmpty(choosenPath))
            return false;

          return true;
        });

      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();

      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath =>
      {
        AppPath = LocalFsResourceProviderBase.ToDosPath(choosenPath);
        ShortName = choosenPath.FileName.Substring(0, choosenPath.FileName.LastIndexOf(".", System.StringComparison.Ordinal));

        var icon = Icon.ExtractAssociatedIcon(AppPath);
        if (icon != null)
        {
          IconPath = Helper.GetIconPath(choosenPath.FileName, icon.ToBitmap());
        }
      }, null);
    }

    public void SelectApp()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DlgAppLauncherAllApps");
    }

    public void SelectGroup()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DlgAppLauncherGroups");
    }

    public void SearchIcon()
    {
      string initialPath = "C:\\";
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(RES_ICO, true, false,
        string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
        path =>
        {
          string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
          if (string.IsNullOrEmpty(choosenPath))
            return false;

          return true;
        });

      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();

      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath => { IconPath = LocalFsResourceProviderBase.ToDosPath(choosenPath); }, null);
    }

    public void NoWindow()
    {
      _windowStyle = ProcessWindowStyle.Hidden;
      WindowStyle = _windowStyle.ToString();
    }

    public void Minimum()
    {
      _windowStyle = ProcessWindowStyle.Minimized;
      WindowStyle = _windowStyle.ToString();
    }

    public void Normal()
    {
      _windowStyle = ProcessWindowStyle.Normal;
      WindowStyle = _windowStyle.ToString();
    }

    public void Maximum()
    {
      _windowStyle = ProcessWindowStyle.Maximized;
      WindowStyle = _windowStyle.ToString();
    }

    public void Add()
    {
      if (AppPath != "")
      {
        if (AppLauncherSettingsEditModel.CurrentApp != null)
          _apps.AppsList.Remove(AppLauncherSettingsEditModel.CurrentApp);

        var app = new App
        {
          ShortName = ShortName,
          ApplicationPath = AppPath,
          Arguments = Arguments,
          Description = Description,
          IconPath = IconPath,
          Password = Password,
          Username = Username,
          WindowStyle = _windowStyle,
          Id = Guid.NewGuid(),
          Admin = AsAdmin,
          Group = Group
        };
        _apps.AppsList.Add(app);
      }
      Helper.SaveApps(_apps);
      Clear();
    }

    /// <summary>
    /// Clear all Fields in Screen
    /// </summary>
    private void Clear(bool includeInitData = false)
    {
      if (includeInitData)
      {
        _appItems.Clear();
        _apps = null;
        _groupItems.Clear();
      }

      ShortName = "";
      Arguments = "";
      AppPath = "";
      Description = "";
      IconPath = "";
      Password = "";
      Username = "";
      _windowStyle = ProcessWindowStyle.Maximized;
      Group = "";
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
      Clear(true);
      AppLauncherSettingsEditModel.CurrentApp = null;
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
