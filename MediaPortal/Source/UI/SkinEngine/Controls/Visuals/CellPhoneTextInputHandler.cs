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

using System.Collections.Generic;
using System.Text;
using System.Timers;
using MediaPortal.Common.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Xaml;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class CellPhoneTextInputHandler : AbstractTextInputHandler
  {
    #region Consts

    public static int CURSOR_ADVANCE_TIMESPAN_MS = 1000;

    public static IList<char> ADDITIONAL_CHARS = new List<char>
      {'.', ',', '\'', '?',
       '!', '\"', '-', '(',
       ')', '@', '/', ':',
       '_', ';', '+', '&',
       '%', '*', '=', '<',
       '>', '�', '�', '$',
       '�', '�', '[', ']',
       '{', '}', '\\', '~',
       '^', '�', '�', '�',
       '#', '|'};

    public static IDictionary<char, IList<char>> CELLPHONE_LAYOUT_LOWER = new Dictionary<char, IList<char>>
      {
          {'1', new List<char>{'1'}},
          {'2', new List<char>{'2', 'a', 'b', 'c'}},
          {'3', new List<char>{'3', 'd', 'e', 'f'}},
          {'4', new List<char>{'4', 'g', 'h', 'i'}},
          {'5', new List<char>{'5', 'j', 'k', 'l'}},
          {'6', new List<char>{'6', 'm', 'n', 'o'}},
          {'7', new List<char>{'7', 'p', 'q', 'r', 's'}},
          {'8', new List<char>{'8', 't', 'u', 'v'}},
          {'9', new List<char>{'9', 'w', 'x', 'y', 'z'}},
          {'0', new List<char>{'0', ' '}},
          {'*', ADDITIONAL_CHARS}
      };

    public const string LOWER_LAYOUT_NAME = "abc";

    public static IDictionary<char, IList<char>> CELLPHONE_LAYOUT_UPPER = new Dictionary<char, IList<char>>
      {
          {'1', new List<char>{'1'}},
          {'2', new List<char>{'2', 'A', 'B', 'C'}},
          {'3', new List<char>{'3', 'D', 'E', 'F'}},
          {'4', new List<char>{'4', 'G', 'H', 'I'}},
          {'5', new List<char>{'5', 'J', 'K', 'L'}},
          {'6', new List<char>{'6', 'M', 'N', 'O'}},
          {'7', new List<char>{'7', 'P', 'Q', 'R', 'S'}},
          {'8', new List<char>{'8', 'T', 'U', 'V'}},
          {'9', new List<char>{'9', 'W', 'X', 'Y', 'Z'}},
          {'0', new List<char>{'0', ' '}},
          {'*', ADDITIONAL_CHARS}
      };

    public const string UPPER_LAYOUT_NAME = "ABC";

    public static IList<IDictionary<char, IList<char>>> KEYBOARD_LAYOUTS = new List<IDictionary<char, IList<char>>>
      {
        CELLPHONE_LAYOUT_LOWER,
        CELLPHONE_LAYOUT_UPPER,
      };

    public static string[] KEYBOARD_LAYOUT_NAMES = new string[] {LOWER_LAYOUT_NAME, UPPER_LAYOUT_NAME};

    #endregion

    protected Timer _timer;
    protected char? _currentCharChoice = null;
    protected int _currentLayoutIndex = 0;
    protected AbstractProperty _currentLayoutNameProperty = new SProperty(typeof(string), string.Empty);
    protected AbstractProperty _currentCharacterChoiceListProperty = new SProperty(typeof(IList<char>), null);
    protected AbstractProperty _currentCharacterChoiceIndexProperty = new SProperty(typeof(int), 0);

    public CellPhoneTextInputHandler(UIElement parentElement, SimplePropertyDataDescriptor textDataDescriptor,
        SimplePropertyDataDescriptor caretIndexDataDescriptor) :
        base(parentElement, textDataDescriptor, caretIndexDataDescriptor)
    {
      _timer = new Timer(CURSOR_ADVANCE_TIMESPAN_MS);
      _timer.Elapsed += OnTimerElapsed;
      SetCurrentLayout(0);
    }

    public override void Dispose()
    {
      base.Dispose();
      _timer.Dispose();
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      AcceptPendingChoice();
    }

    public AbstractProperty CurrentLayoutNameProperty
    {
      get { return _currentLayoutNameProperty; }
    }

    public string CurrentLayoutName
    {
      get { return (string) _currentLayoutNameProperty.GetValue(); }
      set { _currentLayoutNameProperty.SetValue(value); }
    }

    public AbstractProperty CurrentCharacterChoiceListProperty
    {
      get { return _currentCharacterChoiceListProperty; }
    }

    public IList<char> CurrentCharacterChoiceList
    {
      get { return (IList<char>) _currentCharacterChoiceListProperty.GetValue(); }
      set { _currentCharacterChoiceListProperty.SetValue(value); }
    }

    public AbstractProperty CurrentCharacterChoiceIndexProperty
    {
      get { return _currentCharacterChoiceIndexProperty; }
    }

    public int CurrentCharacterChoiceIndex
    {
      get { return (int) _currentCharacterChoiceIndexProperty.GetValue(); }
      set { _currentCharacterChoiceIndexProperty.SetValue(value); }
    }

    public override void HandleInput(ref Key key)
    {
      if (key == Key.None)
        return;

      if (key.IsPrintableKey)
      {
        char character = key.RawCode.Value;
        if (char.IsNumber(character) || character == '*')
        {
          if (_currentCharChoice == character)
          {
            CurrentCharacterChoiceIndex = (CurrentCharacterChoiceIndex + 1)%CurrentCharacterChoiceList.Count;
            UpdateCurrentChoice(false);
            _timer.Stop();
            _timer.Start();
          }
          else
          {
            _currentCharChoice = character;
            CurrentCharacterChoiceList = KEYBOARD_LAYOUTS[_currentLayoutIndex][character];
            CurrentCharacterChoiceIndex = 0;
            _timer.Start();
            UpdateCurrentChoice(true);
          }
        }
        else if (character == '#')
        {
          AcceptPendingChoice();
          SetCurrentLayout((_currentLayoutIndex + 1)%KEYBOARD_LAYOUTS.Count);
        }
        else
          UpdateText(character);
        key = Key.None;
        return;
      }
      AcceptPendingChoice();

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
    }

    protected void SetCurrentLayout(int layoutIndex)
    {
      _currentLayoutIndex = layoutIndex;
      CurrentLayoutName = KEYBOARD_LAYOUT_NAMES[_currentLayoutIndex];
    }

    protected void UpdateCurrentChoice(bool isNewCharacter)
    {
      char newChar = CurrentCharacterChoiceList[CurrentCharacterChoiceIndex];
      if (CaretIndex > 0 && !isNewCharacter)
      {
        StringBuilder sb = new StringBuilder(Text);
        sb[CaretIndex - 1] = newChar;
        Text = sb.ToString();
      }
      else
        UpdateText(newChar);
    }

    protected void AcceptPendingChoice()
    {
      _currentCharChoice = null;
      _timer.Stop();
      CurrentCharacterChoiceList = null;
      CurrentCharacterChoiceIndex = 0;
    }

    protected void UpdateText(char character)
    {
      Text = Text.Insert(CaretIndex, character.ToString());
      CaretIndex++;
    }
  }
}