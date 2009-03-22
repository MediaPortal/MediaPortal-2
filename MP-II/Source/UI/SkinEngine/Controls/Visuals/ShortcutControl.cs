#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Control to provide a shortcut action which can be triggered with a <see cref="Key"/>. This control can also
  /// provide a visible feedback for the user; it shows the shortcut.
  /// It is similar to a <see cref="KeyBinding"/>, which doesn't provide a view.
  /// </summary>
  public class ShortcutControl : Button
  {
    #region Protected fields

    protected Property _shortcutKeyProperty;
    protected Property _descriptionProperty;

    protected Screen _registeredScreen = null;
    protected Key _registeredShortcutKey = null;

    #endregion

    #region Ctor

    public ShortcutControl()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _shortcutKeyProperty = new Property(typeof(Key), null);
      _descriptionProperty = new Property(typeof(string), null);
    }

    void Attach()
    {
      _shortcutKeyProperty.Attach(OnShortcutConcerningPropertyChanged);
      _descriptionProperty.Attach(OnShortcutConcerningPropertyChanged);
      IsEnabledProperty.Attach(OnShortcutConcerningPropertyChanged);
      ScreenProperty.Attach(OnShortcutConcerningPropertyChanged);
    }

    void Detach()
    {
      _shortcutKeyProperty.Detach(OnShortcutConcerningPropertyChanged);
      _descriptionProperty.Detach(OnShortcutConcerningPropertyChanged);
      IsEnabledProperty.Detach(OnShortcutConcerningPropertyChanged);
      ScreenProperty.Detach(OnShortcutConcerningPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      ShortcutControl vs = (ShortcutControl) source;
      ShortcutKey = copyManager.GetCopy(vs.ShortcutKey);
      Description = copyManager.GetCopy(vs.Description);
      Attach();
    }

    public override void Dispose()
    {
      base.Dispose();
      UnregisterShortcut();
    }

    #endregion

    void OnShortcutConcerningPropertyChanged(Property prop, object oldValue)
    {
      UnregisterShortcut();
      RegisterShortcut();
    }

    protected void RegisterShortcut()
    {
      if (ShortcutKey == null)
      {
        Content = "[Undefined shortcut]: " + Description;
        return;
      }
      if (IsEnabled && Screen != null)
      {
        _registeredScreen = Screen;
        _registeredShortcutKey = ShortcutKey;
        _registeredScreen.AddShortcut(_registeredShortcutKey, () =>
        {
          Execute();
          return true;
        });
      }
      Content = ShortcutKey.Name + ": " + Description;
    }

    protected void UnregisterShortcut()
    {
      if (_registeredScreen != null && _registeredShortcutKey != null)
        _registeredScreen.RemoveShortcut(_registeredShortcutKey);
      _registeredScreen = null;
      _registeredShortcutKey = null;
    }

    #region Public properties

    public Key ShortcutKey
    {
      get { return (Key) _shortcutKeyProperty.GetValue(); }
      set { _shortcutKeyProperty.SetValue(value); }
    }

    public Property ShortcutKeyProperty
    {
      get { return _shortcutKeyProperty; }
    }

    public string Description
    {
      get { return (string) _descriptionProperty.GetValue(); }
      set { _descriptionProperty.SetValue(value); }
    }

    public Property DescriptionProperty
    {
      get { return _descriptionProperty; }
    }

    #endregion
  }
}