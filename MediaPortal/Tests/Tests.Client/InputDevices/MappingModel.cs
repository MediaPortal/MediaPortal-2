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
using InputDevices.Common.Mapping;
using InputDevices.Models;
using InputDevices.Models.MappableItemProviders;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.DataObjects;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.Client.InputDevices
{
  public class MappingModel
  {
    [SetUp]
    public void Setup()
    {
      ServiceRegistration.Set<ILogger>(new NoLogger());
      ServiceRegistration.Set<ILocalization>(new NoLocalization());
    }

    [Test]
    public void ShouldInitItemLabelsFromMapping()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId", new[] 
      { 
        new MappedAction(InputAction.CreateKeyAction(Key.Enter), new[] { new Input("inputIdModifierEnter", "inputNameModifierEnter", true), new Input("inputIdEnter", "inputNameEnter") }),
        new MappedAction(InputAction.CreateKeyAction(Key.Escape), new[] { new Input("inputIdModifierEsc", "inputNameModifierEsc", true), new Input("inputIdEsc", "inputNameEsc") })
      });

      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());

      // Assert that the item labels have been correctly set from the mapping
      Assert.AreEqual("inputNameModifierEnter, inputNameEnter", GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), mappableActionsProxy.Items));
      Assert.AreEqual("inputNameModifierEsc, inputNameEsc", GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Escape), mappableActionsProxy.Items));
    }

    [Test]
    public void ShouldClearItemLabels()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId", new[]
      {
        new MappedAction(InputAction.CreateKeyAction(Key.Enter), new[] { new Input("inputIdModifierEnter", "inputNameModifierEnter", true), new Input("inputIdEnter", "inputNameEnter") }),
        new MappedAction(InputAction.CreateKeyAction(Key.Escape), new[] { new Input("inputIdModifierEsc", "inputNameModifierEsc", true), new Input("inputIdEsc", "inputNameEsc") })
      });
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      ItemsList items = mappableActionsProxy.Items;

      mappableActionsProxy.ResetMappings(null);

      // Assert that all item labels have been cleared after resetting the mapping
      Assert.AreEqual(string.Empty, GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), items));
      Assert.AreEqual(string.Empty, GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Escape), items));
    }

    [Test]
    public void ShouldUpdateItemLabelWithSameInput()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId", new[]
      {
        new MappedAction(InputAction.CreateKeyAction(Key.Enter), new[] { new Input("inputIdModifierEnter", "inputNameModifierEnter", true), new Input("inputIdEnter", "inputNameEnter") }),
        new MappedAction(InputAction.CreateKeyAction(Key.Escape), new[] { new Input("inputIdModifierEsc", "inputNameModifierEsc", true), new Input("inputIdEsc", "inputNameEsc") })
      });
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      ItemsList items = mappableActionsProxy.Items;

      mappableActionsProxy.BeginMapping(InputAction.CreateKeyAction(Key.Enter));
      mappableActionsProxy.HandleDeviceInput("deviceId", new[] { new Input("inputIdModifierEsc", "inputNameModifierEsc", true), new Input("inputIdEsc", "inputNameEsc") }, out _);

      // Assert that the item label for the new mapping has been updated and the item label previously mapped to the same input has been cleared
      Assert.AreEqual("inputNameModifierEsc, inputNameEsc", GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), items));
      Assert.AreEqual(string.Empty, GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Escape), items));
    }

    [Test]
    public void ShouldAddMappingForSingleInput()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId");
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      Input[] inputs = new[] { new Input("inputId", "inputName") };

      mappableActionsProxy.BeginMapping(InputAction.CreateKeyAction(Key.Enter));
      bool handled = mappableActionsProxy.HandleDeviceInput("deviceId", inputs, out bool isMappingComplete);

      // Assert that mapping succeeds for a single non-modifier input and the item label is updated
      Assert.IsTrue(handled);
      Assert.IsTrue(isMappingComplete);
      Assert.IsTrue(mapping.TryGetMappedInputs(InputAction.CreateKeyAction(Key.Enter), out var mappedInputs));
      CollectionAssert.AreEquivalent(inputs.Select(i => i.Id), mappedInputs.Select(i => i.Id));
      Assert.AreEqual("inputName", GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), mappableActionsProxy.Items));
    }

    [Test]
    public void ShouldAddMappingForMultipleInputsWithModifier()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId");
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      Input[] inputs = new[] { new Input("inputIdModifier", "inputNameModifier", true), new Input("inputId", "inputName") };

      mappableActionsProxy.BeginMapping(InputAction.CreateKeyAction(Key.Enter));
      bool handled = mappableActionsProxy.HandleDeviceInput("deviceId", inputs, out bool isMappingComplete);

      // Assert that mapping succeeds for multiple inputs with a modifier input and the item label is updated
      Assert.IsTrue(handled);
      Assert.IsTrue(isMappingComplete);
      Assert.IsTrue(mapping.TryGetMappedInputs(InputAction.CreateKeyAction(Key.Enter), out var mappedInputs));
      CollectionAssert.AreEquivalent(inputs.Select(i => i.Id), mappedInputs.Select(i => i.Id));
      Assert.AreEqual("inputNameModifier, inputName", GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), mappableActionsProxy.Items));
    }

    [Test]
    public void ShouldNotAddMappingForSingleModifierInput()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId");
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      Input[] inputs = new[] { new Input("inputIdModifier", "inputNameModifier", true) };

      mappableActionsProxy.BeginMapping(InputAction.CreateKeyAction(Key.Enter));
      bool handled = mappableActionsProxy.HandleDeviceInput("deviceId", inputs, out bool isMappingComplete);

      // Assert that mapping fails for a single modifier input and the item label is not updated
      Assert.IsTrue(handled);
      Assert.IsFalse(isMappingComplete);
      Assert.IsFalse(mapping.TryGetMappedInputs(InputAction.CreateKeyAction(Key.Enter), out var mappedInputs));
      Assert.AreEqual(string.Empty, GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), mappableActionsProxy.Items));
    }

    [Test]
    public void ShouldNotHandleOtherDeviceInput()
    {
      InputDeviceMapping mapping = new InputDeviceMapping("deviceId");
      IMappableActionsProxy mappableActionsProxy = new MappableActionsProxy(Guid.Empty, null, mapping, new KeyItemProvider());
      Input[] inputs = new[] { new Input("inputId", "inputName") };

      mappableActionsProxy.BeginMapping(InputAction.CreateKeyAction(Key.Enter));
      bool handled = mappableActionsProxy.HandleDeviceInput("deviceIdOther", inputs, out bool isMappingComplete);

      // Assert that input from another device is ignored
      Assert.IsFalse(handled);
      Assert.IsFalse(isMappingComplete);
      Assert.IsFalse(mapping.TryGetMappedInputs(InputAction.CreateKeyAction(Key.Enter), out var mappedInputs));
      Assert.AreEqual(string.Empty, GetItemInputLabelForAction(InputAction.CreateKeyAction(Key.Enter), mappableActionsProxy.Items));
    }

    protected static string GetItemInputLabelForAction(InputAction action, ItemsList items)
    {
      ListItem item = items.FirstOrDefault(i => i.AdditionalProperties[MappableActionsProxy.KEY_MAPPED_ACTION] as InputAction == action);
      return item?.Label(MappableActionsProxy.KEY_INPUT, string.Empty).Evaluate() ?? string.Empty;
    }
  }
}
