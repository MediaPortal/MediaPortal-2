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
using MediaPortal.Common;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.General;
using MediaPortal.Utilities;

namespace HomeEditor.Models
{
  public class ActionProxy
  {
    public static readonly Guid HOME_STATE_ID = new Guid("7F702D9C-F2DD-42da-9ED8-0BA92F07787F");
    //actions that should only be displayed on supported skins register with this source state
    public static readonly Guid CUSTOM_HOME_STATE_ID = new Guid("B285DC02-AA8C-47F2-8795-0B13B6E66306");

    protected static readonly List<Guid> SOURCE_STATES = new List<Guid> { HOME_STATE_ID, CUSTOM_HOME_STATE_ID };

    protected AbstractProperty _displayNameProperty = new WProperty(typeof(string), null);
    protected AbstractProperty _actionIdProperty = new WProperty(typeof(Guid), Guid.Empty);
    protected HomeMenuAction _action;
    protected ItemsList _actionItems = new ItemsList();

    public ActionProxy(HomeMenuAction action)
    {
      _action = action;
      if (_action != null)
      {
        DisplayName = _action.DisplayName;
        ActionId = _action.ActionId;
      }
      UpdateActions();
    }

    public HomeMenuAction GroupAction
    {
      get { return _action; }
    }

    public AbstractProperty DisplayNameProperty
    {
      get { return _displayNameProperty; }
    }

    public string DisplayName
    {
      get { return (string)_displayNameProperty.GetValue(); }
      set { _displayNameProperty.SetValue(value); }
    }

    public AbstractProperty ActionIdProperty
    {
      get { return _actionIdProperty; }
    }

    public Guid ActionId
    {
      get { return (Guid)_actionIdProperty.GetValue(); }
      set { _actionIdProperty.SetValue(value); }
    }

    public ItemsList ActionItems
    {
      get { return _actionItems; }
    }

    public bool SaveAction()
    {
      bool created = false;
      if (_action == null)
      {
        created = true;
        _action = new HomeMenuAction();
      }
      _action.ActionId = ActionId;
      _action.DisplayName = DisplayName;
      return created;
    }

    protected void UpdateActions()
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      List<WorkflowAction> actions = new List<WorkflowAction>(wf.MenuStateActions.Values);
      SortedList<string, ListItem> sortedActionItems = new SortedList<string, ListItem>();
      AddActionItems(SOURCE_STATES, actions, sortedActionItems, true);

      _actionItems.Clear();
      CollectionUtils.AddAll(_actionItems, sortedActionItems.Values);
      _actionItems.FireChange();
    }

    protected void AddActionItems(ICollection<Guid> sourceStateIds, List<WorkflowAction> actions, SortedList<string, ListItem> items, bool allowNullSource)
    {
      Guid currentActionId = ActionId;
      for (int i = 0; i < actions.Count; i++)
      {
        WorkflowAction action = actions[i];
        if (!IsActionValid(action, sourceStateIds, allowNullSource))
          continue;

        ListItem item = new ListItem(Consts.KEY_NAME, action.DisplayTitle ?? action.HelpText);
        item.AdditionalProperties[Consts.KEY_ITEM_ACTION] = action;
        if (action.ActionId == currentActionId)
          item.Selected = true;
        item.SelectedProperty.Attach(OnActionItemSelected);
        items.Add(action.DisplayTitle.Evaluate(), item);
      }
    }

    protected bool IsActionValid(WorkflowAction action, ICollection<Guid> sourceStateIds, bool allowNullSource)
    {
      if (action.DisplayTitle == null || string.IsNullOrEmpty(action.DisplayTitle.Evaluate()))
        return false;
      if (action.SourceStateIds == null)
        return allowNullSource;
      return action.SourceStateIds.Intersect(sourceStateIds).Count() > 0;
    }

    protected void OnActionItemSelected(AbstractProperty property, object oldValue)
    {
      WorkflowAction selectedAction = _actionItems.Where(a => a.Selected).Select(i => (WorkflowAction)i.AdditionalProperties[Consts.KEY_ITEM_ACTION]).FirstOrDefault();
      if (selectedAction != null)
      {
        DisplayName = selectedAction.DisplayTitle.Evaluate();
        ActionId = selectedAction.ActionId;
      }
    }
  }
}