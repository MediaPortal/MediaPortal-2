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
using MediaPortal.Common.General;
using System.Collections.Generic;
using System.Linq;

namespace InputDevices.Models
{
  public class MappingProxy
  {
    protected InputAction _inputAction;
    protected IList<Input> _inputs = new List<Input>();
    protected bool _isComplete = false;

    protected AbstractProperty _inputsDisplayNameProperty = new WProperty(typeof(string), null);

    public MappingProxy(InputAction inputAction)
    {
      _inputAction = inputAction;
    }

    public InputAction Action
    {
      get { return _inputAction; }
    }

    public IList<Input> Inputs
    {
      get { return _inputs; }
    }

    public bool IsComplete
    {
      get { return _isComplete; }
    }

    public AbstractProperty InputsDisplayNameProperty
    {
      get { return _inputsDisplayNameProperty; }
    }

    public string InputsDisplayName
    {
      get { return (string)_inputsDisplayNameProperty.GetValue(); }
      set { _inputsDisplayNameProperty.SetValue(value); }
    }

    public bool HandleInput(IEnumerable<Input> inputs)
    {
      if (inputs == null)
        return false;

      // First add any modifier inputs (Ctrl, Alt, etc)
      _inputs = inputs.Where(i => i.IsModifier).ToList();

      // Then add the first non-modifier key, only a single non-modifier key
      // is supported, e.g. Ctrl-A and Ctrl-Alt-A are supported, but Ctrl-A-B is not.
      // Mapping is considered complete if there is at least one non-modifier input.
      Input nonModifierInput = inputs.FirstOrDefault(i => !i.IsModifier);
      if (nonModifierInput == null)
        _isComplete = false;
      else
      {
        _inputs.Add(nonModifierInput);
        _isComplete = true;
      }

      InputsDisplayName = Input.GetInputString(_inputs);
      return true;
    }
  }
}
