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

using MediaPortal.UI.Control.InputManager;
using System;

namespace InputDevices.Common.Mapping
{
  /// <summary>
  /// Class that represents an action to be taken when a device input is received.
  /// This class is deliberately immutable and sealed so that it can be used as a
  /// dictionary key and allows equality comparisons based on the type and action
  /// between different references.
  /// </summary>
  public sealed class InputAction
  {
    /// <summary>
    /// Action type for an action that generates an MP2 key press.
    /// </summary>
    public const string KEY_ACTION_TYPE = "Key";

    public const string WORKFLOW_ACTION_TYPE = "WorkflowAction";

    public const string CONFIG_LOCATION_TYPE = "ConfigLocation";

    public InputAction(string type, string action)
    {
      Type = type ?? throw new ArgumentNullException(nameof(type));
      Action = action ?? throw new ArgumentNullException(nameof(action));
    }

    /// <summary>
    /// The type of the action. A type groups a collection of related actions.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// The specific action to take, this value will depend on the type specified in <see cref="Type"/>,
    /// for example an action with type <see cref="KEY_ACTION_TYPE"/> should set this to a string representation
    /// of the MP2 key to press.
    /// </summary>
    public string Action { get; }

    /// <summary>
    /// Convenience method to create an <see cref="InputAction"/> with type
    /// <see cref="KEY_ACTION_TYPE"/> for the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key that this <see cref="InputAction"/> should input.</param>
    /// <returns><see cref="InputAction"/> for the specified <paramref name="key"/>.</returns>
    public static InputAction CreateKeyAction(Key key)
    {
      return new InputAction(KEY_ACTION_TYPE, key.Name);
    }

    /// <summary>
    /// Convenience method to create an <see cref="InputAction"/> with type
    /// <see cref="WORKFLOW_ACTION_TYPE"/> for the <see cref="MediaPortal.UI.Presentation.Workflow.WorkflowAction"/>
    /// with the specified <paramref name="workflowActionId"/>..
    /// </summary>
    /// <param name="key">The id of the WorkflowAction that this <see cref="InputAction"/> should execute.</param>
    /// <returns><see cref="InputAction"/> for the specified <paramref name="workflowActionId"/>.</returns>
    public static InputAction CreateWorkflowAction(Guid workflowActionId)
    {
      return new InputAction(WORKFLOW_ACTION_TYPE, workflowActionId.ToString());
    }

    /// <summary>
    /// Convenience method to create an <see cref="InputAction"/> with type
    /// <see cref="CONFIG_LOCATION_TYPE"/> for the specified <paramref name="configLocation"/>.
    /// </summary>
    /// <param name="key">The config location that this <see cref="InputAction"/> should display.</param>
    /// <returns><see cref="InputAction"/> for the specified <paramref name="configLocation"/>.</returns>
    public static InputAction CreateConfigAction(string configLocation)
    {
      return new InputAction(CONFIG_LOCATION_TYPE, configLocation);
    }

    #region Overrides

    public override bool Equals(object obj)
    {
      InputAction other = obj as InputAction;
      return other != null && other.Type == Type && other.Action == Action;
    }

    public override int GetHashCode()
    {
      return ToString().GetHashCode();
    }

    public override string ToString()
    {
      return Type + "." + Action;
    }

    public static bool operator ==(InputAction a, InputAction b)
    {
      return ReferenceEquals(a, null) ? ReferenceEquals(b, null) : a.Equals(b);
    }

    public static bool operator !=(InputAction a, InputAction b)
    {
      return !(a == b);
    }

    #endregion
  }
}
