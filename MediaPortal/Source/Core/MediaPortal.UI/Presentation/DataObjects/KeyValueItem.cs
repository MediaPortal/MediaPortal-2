#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.Common.General;
using System;

namespace MediaPortal.UI.Presentation.DataObjects
{
  /// <summary>
  /// Class that associates a single value with a specified key. The value can be bound to from the GUI and
  /// an optional change handler can be specified to be notified when the value changes.
  /// </summary>
  /// <typeparam name="TKey">The type of the key.</typeparam>
  /// <typeparam name="TValue">The type of the value.</typeparam>
  public class KeyValueItem<TKey, TValue>
  {
    protected TKey _key;
    protected AbstractProperty _valueProperty;
    protected Action<TKey, TValue> _valueChangedHandler;

    /// <summary>
    /// Creates a new instance of <see cref="KeyValueItem{TKey, TValue}"/> with the specified key and initial value.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="value">The intial value of the item.</param>
    /// <param name="valueChangedHandler">Optional change handler to call when the value changes, set to <c>null</c> to not be notified of changes.</param>
    public KeyValueItem(TKey key, TValue value, Action<TKey, TValue> valueChangedHandler)
    {
      _key = key;
      _valueProperty = new WProperty(typeof(TValue), value);
      _valueChangedHandler = valueChangedHandler;
      if (valueChangedHandler != null)
        _valueProperty.Attach(OnValueChanged);
    }

    private void OnValueChanged(AbstractProperty property, object oldValue)
    {
      _valueChangedHandler?.Invoke(_key, Value);
    }

    public TKey Key
    {
      get { return _key; }
    }

    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    public TValue Value
    {
      get { return (TValue)_valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }
  }
}
