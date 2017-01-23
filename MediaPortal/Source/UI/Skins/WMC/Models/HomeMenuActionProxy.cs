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

using HomeEditor.Groups;
using HomeEditor.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.Settings;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Models
{
  public class HomeMenuActionProxy
  {
    public static readonly Guid CUSTOM_HOME_STATE_ID = new Guid("B285DC02-AA8C-47F2-8795-0B13B6E66306");

    protected List<HomeMenuGroup> _groups;
    protected Dictionary<Guid, HomeMenuAction> _groupedActions;
    protected Dictionary<Guid, WorkflowAction> _availableActions;
    protected SettingsChangeWatcher<HomeEditorSettings> _settings;
    protected bool _groupsUpdated = true;

    public HomeMenuActionProxy()
    {
      _groups = new List<HomeMenuGroup>();
      _groupedActions = new Dictionary<Guid, HomeMenuAction>();
      _availableActions = new Dictionary<Guid, WorkflowAction>();
      _settings = new SettingsChangeWatcher<HomeEditorSettings>();
      _settings.SettingsChanged += OnSettingsChanged;
    }

    void OnSettingsChanged(object sender, EventArgs e)
    {
      _groupsUpdated = true;
    }

    public bool GroupsUpdated
    {
      get { return _groupsUpdated; }
    }

    public IList<HomeMenuGroup> Groups
    {
      get
      {
        UpdateGroups();
        return _groups;
      }
    }

    public IDictionary<Guid, HomeMenuAction> GroupedActions
    {
      get
      {
        UpdateGroups();
        return _groupedActions;
      }
    }

    public IDictionary<Guid, WorkflowAction> Actions
    {
      get { return _availableActions; }
    }

    public IResourceString OthersName
    {
      get { return LocalizationHelper.CreateResourceString(_settings.Settings.OthersGroupName); }
    }

    public void UpdateActions(IEnumerable<ListItem> menuItems)
    {
      UninitializeActions();
      _availableActions = new Dictionary<Guid, WorkflowAction>();
      foreach (ListItem item in menuItems)
      {
        WorkflowAction action = item.AdditionalProperties[Consts.KEY_ITEM_ACTION] as WorkflowAction;
        if (action != null)
          _availableActions[action.ActionId] = action;
      }

      var customActions = ServiceRegistration.Get<IWorkflowManager>().MenuStateActions.Values
        .Where(a => a.SourceStateIds != null && a.SourceStateIds.Contains(CUSTOM_HOME_STATE_ID));
      foreach (WorkflowAction action in customActions)
        _availableActions[action.ActionId] = action;
      InitializeActions();
    }

    public IList<WorkflowAction> GetGroupActions(HomeMenuGroup group)
    {
      var availableActions = _availableActions;
      List<WorkflowAction> actions;
      if (group == null)
      {
        actions = _availableActions.Values.Where(a => !_groupedActions.ContainsKey(a.ActionId)).ToList();
        actions.Sort(Compare);
      }
      else
      {
        actions = new List<WorkflowAction>();
        foreach (var actionItem in group.Actions)
        {
          WorkflowAction action;
          if (availableActions.TryGetValue(actionItem.ActionId, out action))
            actions.Add(action);
        }
      }
      return actions;
    }

    protected void UpdateGroups()
    {
      if (!_groupsUpdated)
        return;
      _groupsUpdated = false;
      _groups = new List<HomeMenuGroup>();
      _groupedActions = new Dictionary<Guid, HomeMenuAction>();

      var settingsGroups = _settings.Settings.Groups;
      if (settingsGroups != null && settingsGroups.Count > 0)
        _groups.AddRange(settingsGroups);
      else
        _groups.AddRange(DefaultGroups.Create());

      foreach (var group in _groups)
        foreach (var action in group.Actions)
          _groupedActions[action.ActionId] = action;
    }

    protected void InitializeActions()
    {
      foreach (WorkflowAction action in _availableActions.Values)
        action.AddRef();
    }

    protected void UninitializeActions()
    {
      foreach (WorkflowAction action in _availableActions.Values)
        action.RemoveRef();
    }

    protected static int Compare(string a, string b)
    {
      if (string.IsNullOrEmpty(a))
        if (b == null)
          return 0; // a == null, b == null
        else
          return 1; // a == null, b != null
      if (b == null)
        return -1; // a != null, b == null
      return a.CompareTo(b);
    }

    protected static int Compare(WorkflowAction a, WorkflowAction b)
    {
      int res = Compare(a.DisplayCategory, b.DisplayCategory);
      if (res != 0)
        return res;
      return Compare(a.SortOrder, b.SortOrder);
    }
  }
}