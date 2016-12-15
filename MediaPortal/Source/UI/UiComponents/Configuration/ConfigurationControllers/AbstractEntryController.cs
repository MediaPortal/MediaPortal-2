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

using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="Entry"/> configuration setting.
  /// </summary>
  public abstract class AbstractEntryController : DialogConfigurationController
  {
    #region Protected fields

    protected AbstractProperty _valueProperty;
    protected AbstractProperty _errorTextProperty;
    protected AbstractProperty _displayLengthProperty;
    protected AbstractProperty _isValueValidProperty;

    #endregion

    protected AbstractEntryController()
    {
      _valueProperty = new WProperty(typeof(string), string.Empty);
      _isValueValidProperty = new WProperty(typeof(bool), true);
      _errorTextProperty = new WProperty(typeof(string), string.Empty);
      _displayLengthProperty = new WProperty(typeof(int), 0);
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public string Value
    {
      get { return (string) _valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    public AbstractProperty IsValueValidProperty
    {
      get { return _isValueValidProperty; }
    }

    public bool IsValueValid
    {
      get { return (bool) _isValueValidProperty.GetValue(); }
      set { _isValueValidProperty.SetValue(value); }
    }

    public AbstractProperty ErrorTextProperty
    {
      get { return _errorTextProperty; }
    }

    public string ErrorText
    {
      get { return (string) _errorTextProperty.GetValue(); }
      set { _errorTextProperty.SetValue(value); }
    }

    public AbstractProperty DisplayLengthProperty
    {
      get { return _displayLengthProperty; }
      internal set { _displayLengthProperty.SetValue(value); }
    }

    public int DisplayLength
    {
      get { return (int) _displayLengthProperty.GetValue(); }
      set { _displayLengthProperty.SetValue(value); }
    }
  }
}
