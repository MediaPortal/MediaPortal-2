#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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

using MediaPortal.Core.General;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public class KeyBinding : FrameworkElement
  {
    #region Protected fields

    protected AbstractProperty _keyProperty;
    protected AbstractProperty _commandProperty;

    protected Screen _registeredScreen = null;
    protected Key _registeredKey = null;

    #endregion

    #region Ctor

    public KeyBinding()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _keyProperty = new SProperty(typeof(Key), null);
      _commandProperty = new SProperty(typeof(IExecutableCommand), null);
    }

    void Attach()
    {
      _keyProperty.Attach(OnBindingConcerningPropertyChanged);
      IsEnabledProperty.Attach(OnBindingConcerningPropertyChanged);
      ScreenProperty.Attach(OnBindingConcerningPropertyChanged);
    }

    void Detach()
    {
      _keyProperty.Detach(OnBindingConcerningPropertyChanged);
      IsEnabledProperty.Detach(OnBindingConcerningPropertyChanged);
      ScreenProperty.Detach(OnBindingConcerningPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      KeyBindingControl vs = (KeyBindingControl) source;
      Key = copyManager.GetCopy(vs.Key);
      Command = copyManager.GetCopy(vs.Command);
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

    protected void Execute()
    {
      if (Command != null)
        Command.Execute();
    }

    protected void RegisterKeyBinding()
    {
      if (Key == null)
        return;
      if (IsEnabled && Screen != null)
      {
        _registeredScreen = Screen;
        _registeredKey = Key;
        _registeredScreen.AddKeyBinding(_registeredKey, () =>
        {
          Execute();
          return true;
        });
      }
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

    public AbstractProperty CommandProperty
    {
      get { return _commandProperty; }
      set { _commandProperty = value; }
    }

    public IExecutableCommand Command
    {
      get { return (IExecutableCommand) _commandProperty.GetValue(); }
      set { _commandProperty.SetValue(value); }
    }

    #endregion
  }
}
