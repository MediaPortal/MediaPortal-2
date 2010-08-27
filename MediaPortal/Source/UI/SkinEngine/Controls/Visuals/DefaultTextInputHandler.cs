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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class DefaultTextInputHandler : AbstractTextInputHandler
  {
    public DefaultTextInputHandler(UIElement parentElement, SimplePropertyDataDescriptor textDataDescriptor,
        SimplePropertyDataDescriptor caretIndexDataDescriptor) :
        base(parentElement, textDataDescriptor, caretIndexDataDescriptor) { }

    public override void HandleInput(ref Key key)
    {
      if (key == Key.None)
        return;
     
      if (key == Key.BackSpace)
      {
        if (CaretIndex > 0)
        {
          Text = Text.Remove(CaretIndex - 1, 1);
          CaretIndex--;
        }
        key = Key.None;
      }
      else if (key == Key.Left)
      {
        if (CaretIndex > 0)
        {
          CaretIndex--;
          // Only consume the key if we can move the cared - else the key can be used by
          // the focus management, for example
          key = Key.None;
        }
      }
      else if (key == Key.Right)
      {
        if (CaretIndex < Text.Length)
        {
          CaretIndex++;
          // Only consume the key if we can move the cared - else the key can be used by
          // the focus management, for example
          key = Key.None;
        }
      }
      else if (key == Key.Home)
      {
        CaretIndex = 0;
        key = Key.None;
      }
      else if (key == Key.End)
      {
        CaretIndex = Text.Length;
        key = Key.None;
      } 
      else if (key.IsPrintableKey)
      {
        Text = Text.Insert(CaretIndex, key.RawCode.Value.ToString());
        CaretIndex++;
        key = Key.None;
      }
    }
  }
}