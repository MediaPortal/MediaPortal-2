#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.SkinEngine.Commands;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.ScreenManagement;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  /// <summary>
  /// Invisible element which provides a key binding which can be triggered with a <see cref="Key"/>. If the configured <see cref="Key"/> is pressed,
  /// the configured <see cref="Command"/> is executed.
  /// </summary>
  /// <remarks>
  /// This element just provides an "invisible" key handler for the screen where it is defined (or where it is copied to using styling).
  /// This element can be temporarily enabled and disabled using the <see cref="UIElement.IsEnabled"/> property.
  /// </remarks>
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

      IsVisible = false;
    }

    void Init()
    {
      _keyProperty = new SProperty(typeof(Key), null);
      _commandProperty = new SProperty(typeof(IExecutableCommand), null);
    }

    void Attach()
    {
      _keyProperty.Attach(OnBindingRelatedPropertyChanged);
      IsEnabledProperty.Attach(OnBindingRelatedPropertyChanged);
      ScreenProperty.Attach(OnBindingRelatedPropertyChanged);
    }

    void Detach()
    {
      _keyProperty.Detach(OnBindingRelatedPropertyChanged);
      IsEnabledProperty.Detach(OnBindingRelatedPropertyChanged);
      ScreenProperty.Detach(OnBindingRelatedPropertyChanged);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      KeyBinding kb = (KeyBinding) source;
      Key = kb.Key;
      Command = copyManager.GetCopy(kb.Command);
      Attach();
    }

    public override void Dispose()
    {
      UnregisterKeyBinding();
      MPF.TryCleanupAndDispose(Command);
      base.Dispose();
    }

    #endregion

    #region Private & protected members

    void OnBindingRelatedPropertyChanged(AbstractProperty prop, object oldValue)
    {
      UnregisterKeyBinding();
      RegisterKeyBinding();
    }

    protected bool Execute()
    {
      IExecutableCommand cmd = Command;
      if (!IsEnabled)
        return false;
      if (cmd != null)
        InputManager.Instance.ExecuteCommand(cmd.Execute);
      return true;
    }

    protected void RegisterKeyBinding()
    {
      if (_registeredScreen != null)
        return;
      if (Key == null)
        return;
      Screen screen = Screen;
      if (screen == null)
        return;
      _registeredScreen = screen;
      _registeredKey = Key;
      _registeredScreen.AddKeyBinding(_registeredKey, new KeyActionDlgt(Execute));
    }

    protected void UnregisterKeyBinding()
    {
      if (_registeredScreen != null && _registeredKey != null)
        _registeredScreen.RemoveKeyBinding(_registeredKey);
      _registeredScreen = null;
      _registeredKey = null;
    }

    public override void FireEvent(string eventName, RoutingStrategyEnum routingStrategy)
    {
      base.FireEvent(eventName, routingStrategy);
      if (eventName == LOADED_EVENT)
        RegisterKeyBinding();
    }

    #endregion

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
