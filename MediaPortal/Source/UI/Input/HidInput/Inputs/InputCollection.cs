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
using System;
using System.Collections.Generic;

namespace HidInput.Inputs
{
  public class InputCollection
  {
    protected List<Input> _currentInputs;

    public InputCollection()
    {
      _currentInputs = new List<Input>();
    }

    public IList<Input> CurrentInputs
    {
      get { return new List<Input>(_currentInputs); }
    }

    public bool AddInput(Input input)
    {
      // Remove any existing input with same id
      RemoveInput(input);
      // Add the new input so it appears at the end of the list
      _currentInputs.Add(input);
      return true;
    }

    public bool RemoveInput(Input input)
    {
      return _currentInputs.RemoveAll(i => i.Id == input.Id) > 0;
    }

    public bool RemoveAll<T>() where T : Input
    {
      return _currentInputs.RemoveAll(i => i is T) > 0;
    }

    public bool RemoveAll(Type type)
    {
      return _currentInputs.RemoveAll(i => type.IsAssignableFrom(i.GetType())) > 0;
    }

    public void RemoveAll()
    {
      _currentInputs.Clear();
    }
  }
}
