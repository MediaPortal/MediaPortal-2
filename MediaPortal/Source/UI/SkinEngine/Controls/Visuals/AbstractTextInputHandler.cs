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

using System;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public abstract class AbstractTextInputHandler : IDisposable
  {
    protected UIElement _parentElement;
    protected SimplePropertyDataDescriptor _textDataDescriptor;
    protected SimplePropertyDataDescriptor _caretIndexDataDescriptor;

    protected AbstractTextInputHandler(UIElement parentElement, SimplePropertyDataDescriptor textDataDescriptor,
        SimplePropertyDataDescriptor caretIndexDataDescriptor)
    {
      _parentElement = parentElement;
      _textDataDescriptor = textDataDescriptor;
      _caretIndexDataDescriptor = caretIndexDataDescriptor;
    }

    public virtual void Dispose() { }

    public int CaretIndex
    {
      get { return (int) _parentElement.GetPendingOrCurrentValue(_caretIndexDataDescriptor); }
      set { _parentElement.SetValueInRenderThread(_caretIndexDataDescriptor, value); }
    }

    public string Text
    {
      get { return (string) _parentElement.GetPendingOrCurrentValue(_textDataDescriptor); }
      set { _parentElement.SetValueInRenderThread(_textDataDescriptor, value); }
    }

    public abstract void HandleInput(ref Key key);
  }
}