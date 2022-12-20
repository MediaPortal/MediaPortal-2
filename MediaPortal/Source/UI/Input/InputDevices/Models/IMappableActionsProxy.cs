﻿#region Copyright (C) 2007-2021 Team MediaPortal

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
using InputDevices.Common.Mapping;
using MediaPortal.UI.Presentation.DataObjects;
using System;
using System.Collections.Generic;

namespace InputDevices.Models
{
  /// <summary>
  /// Interface for a class that provides a list of mappable actions.
  /// </summary>
  public interface IMappableActionsProxy
  {
    /// <summary>
    /// The id of the workflow state that displays the list of mappable actions. 
    /// </summary>
    Guid MainWorkflowStateId { get; }

    /// <summary>
    /// The mapping that will be displayed and modified.
    /// </summary>
    InputDeviceMapping DeviceMapping { get; }

    /// <summary>
    /// List of items for each mappable action.
    /// </summary>
    ItemsList Items { get; }

    /// <summary>
    /// Begins mapping the specified action. Subsequent calls to <see cref="HandleDeviceInput(string, IEnumerable{Input}, out bool)"/>
    /// should add the device input to the mapping.
    /// </summary>
    /// <param name="inputAction">The action to map.</param>
    void BeginMapping(InputAction inputAction);

    /// <summary>
    /// Cancels any mapping of the action specified in a previous call to <see cref="BeginMapping(InputAction)"/>.
    /// </summary>
    void CancelMapping();

    /// <summary>
    /// Tries to handle the specified inputs by determining whether they can be mapped to the action specified in a previous
    /// call to <see cref="BeginMapping(InputAction)"/>. This can be called repeatedly until <paramref name="isMappingComplete"/> returns <c>true</c>,
    /// at which point the mapping will be updated and no further inputs will be handled until after <see cref="BeginMapping(InputAction)"/>
    /// is called again.
    /// </summary>
    /// <param name="deviceId">The id of the device that generated the inputs.</param>
    /// <param name="inputs">All currently pressed inputs.</param>
    /// <param name="isMappingComplete">Returns <c>true</c> if the inputs were mapped to the action specified in a previous call to <see cref="BeginMapping(InputAction)"/>.</param>
    /// <returns><c>true</c> if the inputs were generated by the device being mapped and are a partial or full mapping for the action currently being mapped.</returns>
    bool HandleDeviceInput(string deviceId, IEnumerable<Input> inputs, out bool isMappingComplete);

    /// <summary>
    /// Deletes the mapping for the specified action.
    /// </summary>
    /// <param name="inputAction">The action to delete the mapping for.</param>
    void DeleteMapping(InputAction inputAction);

    /// <summary>
    /// Resets the device mapping so that it only contains the mappings specified in <paramref name="initialMappings"/>;
    /// or empty if <paramref name="initialMappings"/> is <c>null</c>.
    /// </summary>
    /// <param name="initialMappings">The mappings that should be included in the device mapping after being reset; or <c>null</c> to reset the mapping to empty.</param>
    void ResetMappings(IEnumerable<MappedAction> initialMappings);
  }
}