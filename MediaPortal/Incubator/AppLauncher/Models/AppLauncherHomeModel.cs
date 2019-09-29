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
using System.IO;
using System.Linq;
using System.Security;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AppLauncher.General;
using MediaPortal.Plugins.AppLauncher.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.AppLauncher.Models
{
  public class AppLauncherHomeModel : IWorkflowModel
  {
    #region Consts

    public const string MODEL_ID_STR = "624339C2-0D3B-437B-8046-6F540D704A93";

    #endregion

    #region Private Fields

    private ItemsList _items = new ItemsList();
    private ItemsList _groupItems = new ItemsList();
    private Apps _apps;
    private AbstractProperty _primaryTitle = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _secondaryTitle = new WProperty(typeof(string), string.Empty);
    private AbstractProperty _secondaryVisible = new WProperty(typeof(bool), false);

    #endregion

    #region public Methods

    public void StartApp(ListItem item)
    {
      Start(_apps.AppsList.FirstOrDefault(a => Convert.ToString(a.Id) == (string)item.AdditionalProperties[Consts.KEY_ID]));
    }

    public void SelectGroup(ListItem item)
    {
      foreach (var group in _groupItems)
        group.Selected = false;
      item.Selected = true;
      FillItems((string)item.AdditionalProperties[Consts.KEY_GROUP]);
    }

    public void PrimaryStop()
    {
      ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext?.Stop();
    }

    public void PrimaryPause()
    {
      ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext?.Pause();
    }

    public void SecondaryStop()
    {
      ServiceRegistration.Get<IPlayerContextManager>().SecondaryPlayerContext?.Stop();
    }

    public void SecondaryPause()
    {
      ServiceRegistration.Get<IPlayerContextManager>().SecondaryPlayerContext?.Pause();
    }

    #endregion

    #region Properties

    public AbstractProperty PrimaryTitleProperty
    {
      get { return _primaryTitle; }
    }

    public string PrimaryTitle
    {
      get { return (string)_primaryTitle.GetValue(); }
      set { _primaryTitle.SetValue(value); }
    }

    public AbstractProperty SecondaryTitleProperty
    {
      get { return _secondaryTitle; }
    }

    public string SecondaryTitle
    {
      get { return (string)_secondaryTitle.GetValue(); }
      set { _secondaryTitle.SetValue(value); }
    }

    public AbstractProperty SecondaryVisibleProperty
    {
      get { return _secondaryVisible; }
    }

    public bool SecondaryVisible
    {
      get { return (bool)_secondaryVisible.GetValue(); }
      set { _secondaryVisible.SetValue(value); }
    }

    public ItemsList Items
    {
      get => _items;
      set => _items = value;
    }

    public ItemsList Groups
    {
      get => _groupItems;
      set => _groupItems = value;
    }

    #endregion

    #region private Methods

    private void Start(App app)
    {
      try
      {
        if (ServiceRegistration.Get<IPlayerContextManager>().NumActivePlayerContexts > 0)
        {
          var pp = ServiceRegistration.Get<IPlayerContextManager>().PrimaryPlayerContext;
          PrimaryTitle = pp.CurrentPlayer.MediaItemTitle;

          if (ServiceRegistration.Get<IPlayerContextManager>().NumActivePlayerContexts >= 2)
          {
            var sp = ServiceRegistration.Get<IPlayerContextManager>().SecondaryPlayerContext;
            SecondaryTitle = sp.CurrentPlayer.MediaItemTitle;
            SecondaryVisible = true;
          }
          else
          {
            SecondaryVisible = false;
          }

          ServiceRegistration.Get<IScreenManager>().ShowDialog("DlgSwitchPlaying", (s, g) =>
          {
            StartProcess(app);
          });
        }
        else
        {
          StartProcess(app);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error starting application {0}", ex, Path.GetFileName(app.ApplicationPath));
      }
    }

    private void StartProcess(App app)
    {
      try
      {
        ProcessStartInfo pInfo = new ProcessStartInfo { FileName = app.ApplicationPath, Arguments = app.Arguments };
        pInfo.WindowStyle = app.WindowStyle;
        if (app.Admin == false & app.Username != "" & app.Password != "")
        {
          pInfo.UserName = app.Username;
          pInfo.Password = ToSecureString(app.Password);
        }

        var p = new Process { StartInfo = pInfo };
        p.Start();
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("Error starting process {0}", ex, Path.GetFileName(app.ApplicationPath));
      }
    }

    private SecureString ToSecureString(string password)
    {
      var pass = new SecureString();
      foreach (var c in password)
      {
        pass.AppendChar(c);
      }
      return pass;
    }

    private void Init()
    {
      _groupItems.Clear();

      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      _apps = settingsManager.Load<Apps>();
      var groups = new List<string>();

      var item = new ListItem();
      item.AdditionalProperties[Consts.KEY_GROUP] = "";
      item.SetLabel(Consts.KEY_NAME, Consts.RES_UNGROUPED);
      item.Selected = true;
      _groupItems.Add(item);
      foreach (var a in _apps.AppsList.Where(a => !groups.Contains(a.Group) && a.Group != ""))
      {
        groups.Add(a.Group);
        item = new ListItem();
        item.AdditionalProperties[Consts.KEY_GROUP] = a.Group;
        item.SetLabel(Consts.KEY_NAME, a.Group);
        _groupItems.Add(item);
      }
      _groupItems.FireChange();

      FillItems("");
    }

    private void FillItems(string group)
    {
      _items.Clear();

      foreach (var a in _apps.AppsList)
      {
        if (a.Group == group)
        {
          var item = new ListItem();
          item.AdditionalProperties[Consts.KEY_ID] = Convert.ToString(a.Id);
          item.SetLabel(Consts.KEY_ICON, a.IconPath);
          item.SetLabel(Consts.KEY_DESCRIPTION, a.Description);
          item.SetLabel(Consts.KEY_NAME, a.ShortName);
          _items.Add(item);
        }
      }
      _items.FireChange();
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
