/*
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

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaPortal.Frontend.Api;

namespace MediaPortal.Frontend.ViewModels
{
  /// <summary>
  /// One of the names a choice offers, as a press of its own. The row of names is the editor: a viewer
  /// walks to the name they want and presses it, which is what a television can do and a dropdown cannot.
  /// </summary>
  /// <remarks>
  /// A name does not know anything about the others - releasing them is the row's answer, because "one of
  /// these, and only one" is the row's promise rather than any single name's business.
  /// </remarks>
  public sealed partial class SettingOptionViewModel : ObservableObject
  {
    private readonly Action<string> _picked;
    private bool _isOn;

    public SettingOptionViewModel(string name, Action<string> picked)
    {
      Name = name;
      _picked = picked;
    }

    /// <summary>The name as the server spells it, since that name is what the server stores.</summary>
    public string Name { get; }

    /// <summary>Whether this is the name the row holds. Exactly one name of a choice is on.</summary>
    public bool IsOn
    {
      get => _isOn;
      private set => SetProperty(ref _isOn, value);
    }

    /// <summary>The press. It hands the name over and lets the row move the mark.</summary>
    [RelayCommand]
    public void Pick()
    {
      _picked(Name);
    }

    /// <summary>Moves the mark onto this name, or off it, without anybody pressing.</summary>
    internal void Show(bool on)
    {
      IsOn = on;
    }
  }

  /// <summary>
  /// One field of a section, drawn the way its kind says it is drawn: a switch for a switch, a typed
  /// number for a number, a row of presses for a choice.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The row remembers the value the server answered with, because that is the only honest yardstick it
  /// has: "did the viewer change this" is asked against the server's own last answer, and it is that
  /// answer - not a guess made at load time - that decides what a save sends. Nothing untouched travels
  /// back over the wire.
  /// </para>
  /// <para>
  /// A number is typed, so it is a string until somebody asks what it means; the asking is
  /// <see cref="TryGetValue"/>, and a number that is not a whole number or falls outside the range the
  /// server named says so here, in the row, rather than travelling to the server to be refused there. A
  /// kind the client has never heard of draws nothing and sends nothing - it is reported by the section,
  /// because a screen that drew a box around an unknown value would be lying about understanding it.
  /// </para>
  /// </remarks>
  public sealed partial class SettingFieldViewModel : ObservableObject
  {
    private readonly int? _min;
    private readonly int? _max;
    private object? _serverValue;
    private bool _toggle;
    private string _numberText = "";
    private string _chosen = "";
    private string _error = "";

    public SettingFieldViewModel(SettingFieldState state)
    {
      Key = state.Key;
      Label = state.Label;
      Help = state.Help;
      Kind = state.Kind;
      RestartNeeded = state.RestartNeeded;
      _min = state.Min;
      _max = state.Max;

      IsToggle = state.Kind == SettingSectionViewModelKinds.Toggle;
      IsNumber = state.Kind == SettingSectionViewModelKinds.Number;
      IsChoice = state.Kind == SettingSectionViewModelKinds.Choice;

      if (IsChoice)
      {
        foreach (string name in state.Options)
          Options.Add(new SettingOptionViewModel(name, Choose));
      }

      RangeHint = _min.HasValue && _max.HasValue
          ? string.Format(CultureInfo.InvariantCulture, "Between {0} and {1}.", _min.Value, _max.Value)
          : "";

      Adopt(state);
    }

    /// <summary>How the field is named in the change. Never shown.</summary>
    public string Key { get; }

    /// <summary>What the row is called, in the server's own words.</summary>
    public string Label { get; }

    /// <summary>The quiet sentence under the row. Empty when the label says everything.</summary>
    public string Help { get; }

    /// <summary>The word the server used for this field's kind.</summary>
    public string Kind { get; }

    /// <summary>Whether the editor is a switch.</summary>
    public bool IsToggle { get; }

    /// <summary>Whether the editor is a typed number.</summary>
    public bool IsNumber { get; }

    /// <summary>Whether the editor is a row of presses.</summary>
    public bool IsChoice { get; }

    /// <summary>
    /// Whether this screen has no control for the kind the server named. Such a row draws nothing, changes
    /// nothing and sends nothing; the section says the sentence out loud.
    /// </summary>
    public bool IsUndrawable => !IsToggle && !IsNumber && !IsChoice;

    /// <summary>Whether the server would have to start again for a new value here to take effect.</summary>
    public bool RestartNeeded { get; private set; }

    /// <summary>The words the row shows when <see cref="RestartNeeded"/>, so the note stands where it is.</summary>
    public string RestartNote => RestartNeeded ? "Needs the server to start again." : "";

    /// <summary>The range the server named, in words, next to the box. Empty when no range was named.</summary>
    public string RangeHint { get; }

    /// <summary>The names a choice offers, in the order the server stores them. Empty for the other kinds.</summary>
    public ObservableCollection<SettingOptionViewModel> Options { get; } =
        new ObservableCollection<SettingOptionViewModel>();

    /// <summary>The switch. Only a toggle answers this.</summary>
    public bool Toggle
    {
      get => _toggle;
      set
      {
        if (SetProperty(ref _toggle, value))
          OnPropertyChanged(nameof(IsChanged));
      }
    }

    /// <summary>
    /// What stands in the number box, as text: the on-screen keyboard hands over text, and a half-typed
    /// number is a thing a viewer sees on the way to a whole one rather than an accident.
    /// </summary>
    public string NumberText
    {
      get => _numberText;
      set
      {
        string text = value ?? "";
        if (SetProperty(ref _numberText, text))
        {
          ValidateNumber();
          OnPropertyChanged(nameof(IsChanged));
        }
      }
    }

    /// <summary>The name the row holds. Only a choice answers this.</summary>
    public string Chosen
    {
      get => _chosen;
      private set
      {
        if (SetProperty(ref _chosen, value ?? ""))
        {
          foreach (SettingOptionViewModel option in Options)
            option.Show(option.Name == _chosen);
          OnPropertyChanged(nameof(IsChanged));
        }
      }
    }

    /// <summary>Why this row will not send what it holds. Empty when the row is worth sending.</summary>
    public string Error
    {
      get => _error;
      private set => SetProperty(ref _error, value);
    }

    /// <summary>
    /// Whether the row holds something other than what the server answered. This is the whole of the
    /// difference a save sends - and the whole of the promise that nothing else is touched.
    /// </summary>
    public bool IsChanged
    {
      get
      {
        if (IsUndrawable || _serverValue == null)
          return false;

        if (IsToggle)
          return _toggle != (bool)_serverValue;

        if (IsNumber)
          return !int.TryParse(_numberText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
              out int number) || number != (int)_serverValue;

        return !string.Equals(_chosen, (string?)_serverValue, StringComparison.Ordinal);
      }
    }

    /// <summary>Take the server's answer as what the row holds and as what it holds by right.</summary>
    public void Adopt(SettingFieldState answer)
    {
      RestartNeeded = answer.RestartNeeded;
      OnPropertyChanged(nameof(RestartNeeded));
      OnPropertyChanged(nameof(RestartNote));
      _serverValue = ReadValue(answer);

      if (IsToggle)
        _toggle = _serverValue as bool? ?? false;
      else if (IsNumber)
        _numberText = _serverValue is int number
            ? number.ToString(CultureInfo.InvariantCulture)
            : "";
      else if (IsChoice)
        _chosen = _serverValue as string ?? "";

      foreach (SettingOptionViewModel option in Options)
        option.Show(option.Name == _chosen);

      Error = "";
      OnPropertyChanged(nameof(Toggle));
      OnPropertyChanged(nameof(NumberText));
      OnPropertyChanged(nameof(Chosen));
      OnPropertyChanged(nameof(IsChanged));
    }

    /// <summary>
    /// What the row would send, if it would send anything. A refused row says why in
    /// <see cref="Error"/>, and the section keeps that sentence rather than inventing its own.
    /// </summary>
    public bool TryGetValue(out object value)
    {
      if (IsToggle)
      {
        Error = "";
        value = _toggle;
        return true;
      }

      if (IsNumber)
      {
        if (!int.TryParse(_numberText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture,
                out int number))
        {
          Error = NotANumber(_numberText);
          value = 0;
          return false;
        }

        if (IsOutside(number))
        {
          Error = OutOfRange(number);
          value = 0;
          return false;
        }

        Error = "";
        value = number;
        return true;
      }

      if (IsChoice)
      {
        if (_chosen.Length == 0)
        {
          Error = "Nothing is picked yet.";
          value = "";
          return false;
        }

        Error = "";
        value = _chosen;
        return true;
      }

      Error = string.Format(CultureInfo.InvariantCulture,
          "This screen cannot draw a value of the kind \"{0}\".", Kind);
      value = "";
      return false;
    }

    /// <summary>What the server answered, in the shape its kind implies: a bool, a whole number or a name.</summary>
    private static object? ReadValue(SettingFieldState answer)
    {
      JsonElement value = answer.Value;
      try
      {
        if (answer.Kind == SettingSectionViewModelKinds.Toggle)
          return value.ValueKind == JsonValueKind.True;

        if (answer.Kind == SettingSectionViewModelKinds.Number)
          return value.TryGetInt32(out int number) ? number : (object?)null;

        if (answer.Kind == SettingSectionViewModelKinds.Choice)
          return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
      }
      catch (InvalidOperationException)
      {
        // A value that is not the shape its kind promised is the server's mistake, and the honest answer
        // here is "I hold nothing" - the row then shows its kind's empty editor and sends nothing back.
        return null;
      }

      return null;
    }

    /// <summary>The press of one name: the row moves the mark, which releases every other name.</summary>
    private void Choose(string name)
    {
      Chosen = name;
    }

    /// <summary>Whether a number sits outside the bounds the server named. No bounds, no refusal.</summary>
    private bool IsOutside(int number)
    {
      return (_min.HasValue && number < _min.Value) || (_max.HasValue && number > _max.Value);
    }

    /// <summary>The sentence for text that is not a whole number, with the range in it.</summary>
    private string NotANumber(string text)
    {
      string said = text.Trim().Length == 0
          ? "Nothing is typed"
          : "\"" + text.Trim() + "\" is not a whole number";
      return string.Format(CultureInfo.InvariantCulture, "{0}, and a whole number is asked for.{1}", said,
          RangeHint.Length == 0 ? "" : " " + RangeHint);
    }

    /// <summary>The sentence for a whole number the server would not accept, naming the range it named.</summary>
    private string OutOfRange(int number)
    {
      return string.Format(CultureInfo.InvariantCulture, "{0} is out of range.{1}", number,
          RangeHint.Length == 0 ? " This field accepts no bounds the screen was told about." : " " + RangeHint);
    }

    /// <summary>Says straight away what a number box holds, so the row does not wait for a save to complain.</summary>
    private void ValidateNumber()
    {
      if (!IsNumber)
        return;

      string text = _numberText.Trim();
      if (text.Length == 0)
      {
        // An empty box is somebody mid-edit rather than a wrong answer, so nothing is said about it yet.
        Error = "";
        return;
      }

      if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number))
      {
        Error = NotANumber(text);
        return;
      }

      Error = IsOutside(number) ? OutOfRange(number) : "";
    }
  }
}
