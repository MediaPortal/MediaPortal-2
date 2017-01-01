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

using MediaPortal.UI.Presentation.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Presentation.DataObjects;
using HomeEditor.Settings;
using MediaPortal.Common;
using MediaPortal.Common.Settings;
using HomeEditor.Groups;
using MediaPortal.UiComponents.SkinBase.General;

namespace HomeEditor.Models
{
  public class HomeEditorModel : AbstractItemProxy<HomeMenuGroup>, IWorkflowModel
  {
    public static readonly Guid MODEL_ID = new Guid("31D0D607-4610-43C9-B9F1-F7E384C74EBA");
    public static readonly Guid STATE_GROUPS = new Guid("BF7E11F6-87DE-4CB6-9A94-1A738CC52710");
    public static readonly Guid STATE_GROUP_EDIT = new Guid("BE8E74DE-2008-4398-831A-626A5DC074AA");
    public static readonly Guid STATE_GROUP_REMOVE = new Guid("0D4B781F-700D-4F23-BEE5-816D0FE2E3CB");
    public static readonly Guid STATE_ACTION_EDIT = new Guid("08673705-C9E8-4E82-AFC0-AC7A94A95F32");
    public static readonly Guid STATE_ACTION_REMOVE = new Guid("94D9D1D3-6E96-46AC-BD9B-25D58C50913D");
    
    protected GroupProxy _groupProxy;
    protected ActionProxy _actionProxy;
    protected bool _needsUpdate;

    public static HomeEditorModel Instance()
    {
      return (HomeEditorModel)ServiceRegistration.Get<IWorkflowManager>().GetModel(MODEL_ID);
    }

    public GroupProxy GroupProxy
    {
      get { return _groupProxy; }
    }

    public ActionProxy ActionProxy
    {
      get { return _actionProxy; }
    }

    public void RestoreDefaults()
    {
      _items.Clear();
      _items.AddRange(DefaultGroups.Create());
      UpdateItems();
      _needsUpdate = true;
    }

    public void AddGroup()
    {
      _groupProxy = new GroupProxy(null);
      NavigatePush(STATE_GROUP_EDIT);
    }

    public void EditGroup(GroupListItem item)
    {
      _groupProxy = new GroupProxy(item.Group);
      NavigatePush(STATE_GROUP_EDIT);
    }

    public void MoveGroupUp(GroupListItem item)
    {
      MoveItem(item.Group, -1);
      _needsUpdate = true;
    }

    public void MoveGroupDown(GroupListItem item)
    {
      MoveItem(item.Group, 1);
      _needsUpdate = true;
    }

    public void SaveGroup()
    {
      if (_groupProxy == null)
        return;
      if (_groupProxy.SaveGroup())
        _items.Add(_groupProxy.Group);
      UpdateItems();
      _needsUpdate = true;
    }

    public void AddAction()
    {
      _actionProxy = new ActionProxy(null);
      NavigatePush(STATE_ACTION_EDIT);
    }

    public void EditAction(ActionListItem item)
    {
      _actionProxy = new ActionProxy(item.GroupAction);
      NavigatePush(STATE_ACTION_EDIT);
    }

    public void SaveAction()
    {
      if (_groupProxy == null || _actionProxy == null || _actionProxy.ActionId == Guid.Empty)
        return;
      if (_actionProxy.SaveAction())
        _groupProxy.GroupActions.Add(_actionProxy.GroupAction);
      _groupProxy.UpdateItems();
    }

    public override void UpdateItems()
    {
      _itemsList.Clear();
      for (int i = 0; i < _items.Count; i++)
      {
        GroupListItem item = new GroupListItem(_items[i]);
        item.AdditionalProperties[KEY_IS_UP_BUTTON_FOCUSED] = i == _lastUpIndex;
        item.AdditionalProperties[KEY_IS_DOWN_BUTTON_FOCUSED] = i == _lastDownIndex;
        _itemsList.Add(item);
      }
      _itemsList.FireChange();
    }

    protected void LoadGroups()
    {
      _items.Clear();
      HomeEditorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<HomeEditorSettings>();
      if (settings.Groups != null && settings.Groups.Count > 0)
        _items.AddRange(settings.Groups);
      else
        _items.AddRange(DefaultGroups.Create());
    }

    protected void SaveGroups()
    {
      if (!_needsUpdate)
        return;
      var sm = ServiceRegistration.Get<ISettingsManager>();
      HomeEditorSettings settings = sm.Load<HomeEditorSettings>();
      settings.Groups = new List<HomeMenuGroup>(_items);
      sm.Save(settings);
      _needsUpdate = false;
    }

    protected override void UpdateItemsToRemove()
    {
      _itemsToRemoveList.Clear();
      foreach (HomeMenuGroup item in _items)
      {
        ListItem listItem = new ListItem(Consts.KEY_NAME, item.DisplayName);
        listItem.AdditionalProperties[KEY_ITEM_TO_REMOVE] = item;
        listItem.SelectedProperty.Attach(ItemToRemoveSelectionChanged);
        _itemsToRemoveList.Add(listItem);
      }
      _itemsToRemoveList.FireChange();
    }

    public override void RemoveSelectedItems()
    {
      base.RemoveSelectedItems();
      _needsUpdate = true;
    }

    protected void Update(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      Guid newState = newContext.WorkflowState.StateId;
      if (newState == STATE_GROUPS)
      {
        UpdateItems();
      }
      else if (newState == STATE_GROUP_REMOVE)
      {
        BeginRemoveItems();
      }
      else if (newState == STATE_ACTION_REMOVE)
      {
        if (_groupProxy != null)
          _groupProxy.BeginRemoveItems();
      }
    }

    protected void NavigatePush(Guid stateId)
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      wf.NavigatePush(stateId);
    }

    #region IWorkflow
    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      Update(oldContext, newContext, push);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      LoadGroups();
      Update(oldContext, newContext, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SaveGroups();
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      
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
