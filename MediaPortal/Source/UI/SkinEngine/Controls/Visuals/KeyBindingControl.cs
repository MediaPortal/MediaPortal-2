#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Control to provide a key binding which can be triggered with a <see cref="Key"/>. This control can also
  /// provide a visible feedback for the user; it shows the key and a description for the shortcut.
  /// It is similar to a <see cref="KeyBinding"/>, which doesn't provide a view element.
  /// </summary>
  // TODO Albert, 2012-04-01: Maybe this functionality should better be provided as an attached property which can be used with buttons?
  public class KeyBindingControl : Button
  {
    #region Protected fields

    protected AbstractProperty _keyProperty;
    protected AbstractProperty _descriptionProperty;

    protected Screen _registeredScreen = null;
    protected Key _registeredKey = null;

    #endregion

    #region Ctor

    public KeyBindingControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _keyProperty = new SProperty(typeof(Key), null);
      _descriptionProperty = new SProperty(typeof(string), null);
    }

    void Attach()
    {
      _keyProperty.Attach(OnBindingConcerningPropertyChanged);
      _descriptionProperty.Attach(OnBindingConcerningPropertyChanged);
      IsEnabledProperty.Attach(OnBindingConcerningPropertyChanged);
      ScreenProperty.Attach(OnBindingConcerningPropertyChanged);
    }

    void Detach()
    {
      _keyProperty.Detach(OnBindingConcerningPropertyChanged);
      _descriptionProperty.Detach(OnBindingConcerningPropertyChanged);
      IsEnabledProperty.Detach(OnBindingConcerningPropertyChanged);
      ScreenProperty.Detach(OnBindingConcerningPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      KeyBindingControl vs = (KeyBindingControl) source;
      Key = vs.Key;
      Description = vs.Description;
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      UnregisterKeyBinding();
    }

    #endregion

    void OnBindingConcerningPropertyChanged(AbstractProperty prop, object oldValue)
    {
      UnregisterKeyBinding();
      RegisterKeyBinding();
    }

    protected void RegisterKeyBinding()
    {
      if (Key == null)
      {
        Content = "[Undefined key binding]: " + Description;
        return;
      }
      Screen screen = Screen;
      if (IsEnabled && screen != null)
      {
        _registeredScreen = screen;
        _registeredKey = Key;
        _registeredScreen.AddKeyBinding(_registeredKey, new VoidKeyActionDlgt(Execute));
      }
      Content = Key.Name + ": " + Description;
    }

    protected void UnregisterKeyBinding()
    {
      if (_registeredScreen != null && _registeredKey != null)
        _registeredScreen.RemoveKeyBinding(_registeredKey);
      _registeredScreen = null;
      _registeredKey = null;
    }

    #region Public properties

    public Key Key
    {
      get { return (Key) _keyProperty.GetValue(); }
      set { _keyProperty.SetValue(value); }
    }

    public AbstractProperty KeyProperty
    {
      get { return _keyProperty; }
    }

    public string Description
    {
      get { return (string) _descriptionProperty.GetValue(); }
      set { _descriptionProperty.SetValue(value); }
    }

    public AbstractProperty DescriptionProperty
    {
      get { return _descriptionProperty; }
    }

    #endregion
  }
}
