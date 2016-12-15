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
        int caretIndex = CaretIndex;
        if (caretIndex > 0)
        {
          Text = Text.Remove(caretIndex - 1, 1);
          CaretIndex = caretIndex - 1;
        }
        key = Key.None;
      }
      else if (key == Key.Delete)
      {
        string text = Text;
        int caretIndex = CaretIndex;
        if (caretIndex < text.Length)
          Text = text.Remove(caretIndex, 1);
      }
      else if (key == Key.Left)
      {
        int caretIndex = CaretIndex;
        if (caretIndex > 0)
        {
          CaretIndex = caretIndex - 1;
          // Only consume the key if we can move the caret - else the key can be used by
          // the focus management, for example
          key = Key.None;
        }
      }
      else if (key == Key.Right)
      {
        int caretIndex = CaretIndex;
        string text = Text;
        if (caretIndex < text.Length)
        {
          CaretIndex = caretIndex + 1;
          // Only consume the key if we can move the caret - else the key can be used by
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
        int caretIndex = CaretIndex;
        Text = Text.Insert(caretIndex, key.RawCode.Value.ToString());
        CaretIndex = caretIndex + 1;
        key = Key.None;
      }
    }
  }
}