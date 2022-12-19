#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using InputDevices.Common.Devices;
using InputDevices.Common.Inputs;
using InputDevices.Common.Mapping;
using InputDevices.Models.MappableItemProviders;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.SkinBase.General;
using System;
using System.Collections.Generic;

namespace InputDevices.Models
{
  /// <summary>
  /// Implementation of <see cref="IMappableActionsProxy"/>.
  /// </summary>
  public class MappableActionsProxy : IMappableActionsProxy
  {
    public const string KEY_MAPPED_ACTION = "MappedAction";
    public const string KEY_INPUT = "Input";

    protected Guid _mainWorkflowStateId;
    protected DeviceMetadata _device;
    protected InputDeviceMapping _mapping;
    protected IMappableItemProvider _mappableItemProvider;
    protected ItemsList _items;
    protected bool _isItemsInit;

    protected AbstractProperty _mappingProxyProperty = new WProperty(typeof(MappingProxy), null);

    public MappableActionsProxy(Guid mainWorkflowStateId, DeviceMetadata device, InputDeviceMapping deviceMapping, IMappableItemProvider mappableItemProvider)
    {
      _mainWorkflowStateId = mainWorkflowStateId;
      _device = device;
      _mapping = deviceMapping;
      _mappableItemProvider = mappableItemProvider;
      _items = new ItemsList();
    }

    public Guid MainWorkflowStateId
    {
      get { return _mainWorkflowStateId; }
    }

    public DeviceMetadata Device
    {
      get { return _device; }
    }

    public InputDeviceMapping DeviceMapping
    {
      get { return _mapping; }
    }

    public ItemsList Items
    {
      get
      {
        InitItems();
        return _items;
      }
    }

    public AbstractProperty MappingProxyProperty
    {
      get { return _mappingProxyProperty; }
    }

    /// <summary>
    /// Proxy class containing GUI properties for the action currently being mapped.
    /// </summary>
    public MappingProxy MappingProxy
    {
      get { return (MappingProxy)_mappingProxyProperty.GetValue(); }
      set { _mappingProxyProperty.SetValue(value); }
    }

    /// <summary>
    /// Initializes the items list, called lazily in the <see cref="Items"/> getter.
    /// </summary>
    protected void InitItems()
    {
      lock (_items.SyncRoot)
      {
        if (_isItemsInit)
          return;
        _isItemsInit = true;
        foreach (MappableItem item in _mappableItemProvider.GetMappableItems())
          _items.Add(CreateActionItem(item));
      }
    }

    protected ListItem CreateActionItem(MappableItem mappableItem)
    {
      ListItem item = new ListItem(Consts.KEY_NAME, mappableItem.DisplayName);
      item.AdditionalProperties[KEY_MAPPED_ACTION] = mappableItem.MappableAction;
      string mappedInput = _mapping.TryGetMappedInputs(mappableItem.MappableAction, out IReadOnlyList<Input> inputs) ? Input.GetInputString(inputs) : string.Empty;
      item.SetLabel(KEY_INPUT, mappedInput);
      return item;
    }

    public void BeginMapping(InputAction inputAction)
    {
      MappingProxy = new MappingProxy(inputAction);
    }

    public void CancelMapping()
    {
      MappingProxy = null;
    }

    public bool HandleDeviceInput(string deviceId, IEnumerable<Input> inputs, out bool isMappingComplete)
    {
      isMappingComplete = false;

      if (deviceId != _mapping.DeviceId)
        return false;

      MappingProxy mappingProxy = MappingProxy;
      if (mappingProxy == null || mappingProxy.IsComplete)
        return false;

      if (!mappingProxy.HandleInput(inputs))
        return false;

      isMappingComplete = mappingProxy.IsComplete;
      if (isMappingComplete)
      {
        _mapping.AddOrUpdateActionMapping(mappingProxy.Action, mappingProxy.Inputs, out InputAction removedActionWithSameInput);
        UpdateMappingItem(mappingProxy.Action, mappingProxy.Inputs, removedActionWithSameInput);
      }
      return true;
    }

    public void DeleteMapping(InputAction inputAction)
    {
      _mapping.RemoveMapping(inputAction);
      UpdateMappingItem(inputAction, null, null);
    }

    public void ResetMappings(IEnumerable<MappedAction> initialMappings)
    {
      _mapping.ResetMappings(initialMappings);

      List<ListItem> changedItems;
      lock (_items.SyncRoot)
      {
        // if items aren't initialized yet then simply return, the items
        // will use the updated mapping when they get intitialized
        if (!_isItemsInit)
          return;

        changedItems = new List<ListItem>();
        foreach (ListItem item in _items)
        {
          InputAction itemAction = GetItemInputAction(item);
          if (itemAction == null)
            continue;
          // See if the mapping has actually changed
          string currentInput = item.Label(KEY_INPUT, string.Empty).Evaluate();
          string newInput = _mapping.TryGetMappedInputs(itemAction, out var inputs) ? Input.GetInputString(inputs) : string.Empty;
          if (currentInput != newInput)
          {
            item.SetLabel(KEY_INPUT, newInput);
            changedItems.Add(item);
          }
        }
      }

      // Fire change handlers outside of the lock
      foreach (ListItem item in changedItems)
        item.FireChange();
    }

    protected void UpdateMappingItem(InputAction inputAction, IEnumerable<Input> inputs, InputAction removedActionWithSameInput)
    {
      ListItem updatedActionItem = null;
      ListItem existingMappingItem = null;
      lock (_items.SyncRoot)
      {
        // if items aren't initialized yet then simply return, the items
        // will use the updated mapping when they get intitialized
        if (!_isItemsInit)
          return;

        foreach (ListItem item in _items)
        {
          InputAction itemAction = GetItemInputAction(item);
          if (itemAction == null)
            continue;

          // If the item containing the action that has had it's mapping updated hasn't been found yet, see if this is it
          if (updatedActionItem == null && itemAction == inputAction)
          {
            item.SetLabel(KEY_INPUT, Input.GetInputString(inputs));
            updatedActionItem = item;
          }
          // Else if an action with the same input was removed from the mapping, see if this is it
          else if (removedActionWithSameInput != null && existingMappingItem == null && itemAction == removedActionWithSameInput)
          {
            item.SetLabel(KEY_INPUT, string.Empty);
            existingMappingItem = item;
          }

          // If all required items have been updated, break
          if (updatedActionItem != null && (removedActionWithSameInput == null || existingMappingItem != null))
            break;
        }
      }

      // Fire change handlers outside of the lock
      updatedActionItem?.FireChange();
      existingMappingItem?.FireChange();
    }

    protected InputAction GetItemInputAction(ListItem item)
    {
      return item.AdditionalProperties.TryGetValue(KEY_MAPPED_ACTION, out object actionObj) ? actionObj as InputAction : null;
    }
  }
}
