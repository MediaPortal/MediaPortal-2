#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Threading;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
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
      get { return (int) _caretIndexDataDescriptor.Value; }
      set { _parentElement.SetValueInRenderThread(_caretIndexDataDescriptor, value); }
    }

    public string Text
    {
      get
      {
        Screen screen = _parentElement.Screen;
        Animator animator = screen == null ? null : screen.Animator;
        object syncObj = animator == null ? null : animator.SyncObject;
        if (syncObj != null)
          Monitor.Enter(syncObj);
        try
        {
          object result;
          if (screen == null || !screen.Animator.TryGetPendingValue(_textDataDescriptor, out result))
            result = _textDataDescriptor.Value;
          return (string) result;
        }
        finally
        {
          if (syncObj != null)
            Monitor.Exit(syncObj);
        }
      }
      set
      {
        // In fact, this is the implementation of UIElement.SetValueInRenderThread(). We repeat it here to have the
        // code of the get and set methods in sync.
        Screen screen = _parentElement.Screen;
        if (screen == null || SkinContext.RenderThread == Thread.CurrentThread)
          _textDataDescriptor.Value = value;
        else
          screen.Animator.SetValue(_textDataDescriptor, value);
      }
    }

    public abstract void HandleInput(ref Key key);
  }
}