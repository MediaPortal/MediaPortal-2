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

using InputDevices.Common.Inputs;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Common.Mapping
{
  /// <summary>
  /// Class that represents all mappings for a particular device.
  /// </summary>
  public class InputDeviceMapping
  {
    protected readonly object _syncObj = new object();
    protected string _deviceId;
    protected IDictionary<InputAction, MappedAction> _actionMap;
    protected IDictionary<string, MappedAction> _inputMap;

    /// <summary>
    /// Creates an empty mapping.
    /// </summary>
    /// <param name="deviceId">The id of the device that this is the mapping for.</param>
    public InputDeviceMapping(string deviceId)
      : this(deviceId, null)
    {
    }

    /// <summary>
    /// Creates a mapping containing the specified mappings.
    /// </summary>
    /// <param name="deviceId">The id of the device that this is the mapping for.</param>
    /// <param name="mappedActions">The existing mappings for the device.</param>
    public InputDeviceMapping(string deviceId, IEnumerable<MappedAction> mappedActions)
    {
      Init(deviceId, mappedActions);
    }

    /// <summary>
    /// The id of the device that this is the mapping for.
    /// </summary>
    public string DeviceId
    {
      get { return _deviceId; }
    }

    /// <summary>
    /// Gets all mapped actions contained in this mapping.
    /// </summary>
    public IReadOnlyList<MappedAction> MappedActions
    {
      get
      {
        lock (_syncObj)
          return _actionMap.Values.ToList().AsReadOnly();
      }
    }

    /// <summary>
    /// Adds or updates the mapping for the specified action.
    /// </summary>
    /// <param name="action">The action to update the mappings for.</param>
    /// <param name="inputs">The inputs to map to the action.</param>
    /// <param name="removedActionWithSameInput">The existing action that had it's mapping removed because it was mapped to the same inputs as the new mapping.</param>
    public void AddOrUpdateActionMapping(InputAction action, IEnumerable<Input> inputs, out InputAction removedActionWithSameInput)
    {
      lock (_syncObj)
        AddMappedActionToDictionaries(new MappedAction(action, inputs), out removedActionWithSameInput);
    }

    /// <summary>
    /// Removes the action from this device mapping.
    /// </summary>
    /// <param name="action">The action to remove the mappings for.</param>
    public void RemoveMapping(InputAction action)
    {
      lock (_syncObj)
      {
        // Try and get the existing inputs mapped to this action so it can be removed
        // from the input dictionary. If none found then the mapping doesn't exist in
        // either dictionary so there is nothing to remove.
        if (!_actionMap.TryGetValue(action, out MappedAction mapping))
          return;
        _actionMap.Remove(action);
        _inputMap.Remove(GetInputKey(mapping.Inputs));
      }
    }

    /// <summary>
    /// Removes any action mapped to the specified inputs from this device mapping.
    /// </summary>
    /// <param name="inputs">The inputs to remove the mapping for.</param>
    public void RemoveMapping(IEnumerable<Input> inputs)
    {
      lock (_syncObj)
      {
        string inputKey = GetInputKey(inputs);
        if (inputKey == null || !_inputMap.TryGetValue(inputKey, out MappedAction mapping))
          return;
        _actionMap.Remove(mapping.Action);
        _inputMap.Remove(inputKey);
      }
    }

    /// <summary>
    /// Resets this device mapping so that it only contains the mappings specified in <paramref name="initialMappings"/>;
    /// or empty if <paramref name="initialMappings"/> is <c>null</c>.
    /// </summary>
    /// <param name="initialMappings">The mappings that should be included in this device mapping after being reset; or <c>null</c> to reset the mapping to empty.</param>
    public void ResetMappings(IEnumerable<MappedAction> initialMappings)
    {
      lock (_syncObj)
      {
        _actionMap.Clear();
        _inputMap.Clear();
        if (initialMappings != null)
          foreach (MappedAction mappedAction in initialMappings)
            AddMappedActionToDictionaries(mappedAction, out _);
      }
    }

    /// <summary>
    /// Tries to get the inputs that are mapped to the specified action.
    /// </summary>
    /// <param name="action">The action to try and get the mapped inputs for.</param>
    /// <param name="inputs">If successful, contains the inputs mapped to <paramref name="action"/>.</param>
    /// <returns><c>true</c> if the mapping was found; else <c>false</c>.</returns>
    public bool TryGetMappedInputs(InputAction action, out IReadOnlyList<Input> inputs)
    {
      lock (_syncObj)
        inputs = action != null && _actionMap.TryGetValue(action, out MappedAction mappedAction) ? mappedAction.Inputs : null;

      return inputs != null;
    }

    /// <summary>
    /// Tries to get the action that is mapped to the specified inputs.
    /// </summary>
    /// <param name="inputs">The inputs to try and get the mapped actions for.</param>
    /// <param name="action">If successful, contains the action mapped to <paramref name="inputs"/>.</param>
    /// <returns><c>true</c> if the mapping was found; else <c>false</c>.</returns>
    public bool TryGetMappedAction(IEnumerable<Input> inputs, out InputAction action)
    {
      string key = GetInputKey(inputs);
      lock (_syncObj)
        action = key != null && _inputMap.TryGetValue(key, out MappedAction mappedAction) ? mappedAction.Action : null;

      return action != null;
    }

    protected void Init(string deviceId, IEnumerable<MappedAction> mappedActions)
    {
      lock (_syncObj)
      {
        _deviceId = deviceId;

        // It's tempting to use ConcurrentDictionaries here to avoid locking however
        // both dictionaries need to be kept in sync which can't be guranteed without
        // an additional lock so simpler to just use ordinary dictionaries and locking
        _actionMap = new Dictionary<InputAction, MappedAction>();
        _inputMap = new Dictionary<string, MappedAction>();

        if (mappedActions != null)
          foreach (MappedAction mappedAction in mappedActions)
            AddMappedActionToDictionaries(mappedAction, out _);
      }
    }

    protected void AddMappedActionToDictionaries(MappedAction mappedAction, out InputAction removedActionWithSameInput)
    {
      // Mapped input has changed so remove the old mapping from the input dictionary
      string previousInputKey = _actionMap.TryGetValue(mappedAction.Action, out MappedAction previousAction) ? GetInputKey(previousAction.Inputs) : null;
      if (previousInputKey != null)
        _inputMap.Remove(previousInputKey);

      // Get the key for the new mapped input
      string inputKey = GetInputKey(mappedAction.Inputs);

      // If null then no inputs are mapped and the action should be removed from
      // both dictionaries (it was already removed from the input dictionary above)
      if (inputKey == null)
      {
        _actionMap.Remove(mappedAction.Action);
        removedActionWithSameInput = null;
        return;
      }

      // See if a different action is mapped to the same input, if so remove it from the
      // action dictionary, it's entry in the input dictionary will be overwritten below when
      // adding the new mapping so doesn't need to be removed here.
      if (_inputMap.TryGetValue(inputKey, out MappedAction actionWithSameInput))
      {
        _actionMap.Remove(actionWithSameInput.Action);
        removedActionWithSameInput = actionWithSameInput.Action;
      }
      else // ensure that the out parameter has a defined value
        removedActionWithSameInput = null;

      _actionMap[mappedAction.Action] = mappedAction;
      _inputMap[inputKey] = mappedAction;
    }

    /// <summary>
    /// Gets a unique key for an enumeration of inputs, regardless of their order.
    /// </summary>
    /// <param name="inputs">Enumeration of inputs.</param>
    /// <returns>A string that uniquely identifies the inputs, or <c>null</c> if the enumerattion is null or empty.</returns>
    protected static string GetInputKey(IEnumerable<Input> inputs)
    {
      if (inputs == null)
        return null;
      string key = string.Join("|", inputs.Select(i => i.Id).OrderBy(i => i));
      return !string.IsNullOrEmpty(key) ? key : null;
    }
  }
}
