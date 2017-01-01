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
using System.Globalization;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using System.Linq;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.Controls.Visuals.Templates;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.SkinManagement;
using MediaPortal.Utilities;
using SharpDX;
using Size = SharpDX.Size2;
using SizeF = SharpDX.Size2F;
using PointF = SharpDX.Vector2;

namespace MediaPortal.UI.SkinEngine.Controls.Visuals
{
  public enum VirtualKeyboardTextStyle
  {
    /// <summary>
    /// The virtual keyboard doesn't show the edited text.
    /// </summary>
    None,

    /// <summary>
    /// The virtual keyboard shows the edited text.
    /// </summary>
    Text,

    /// <summary>
    /// The virtual keyboard shows the characters which are edited as stars/bullets.
    /// </summary>
    PasswordText,
  }

  public delegate void VirtualKeyboardCloseDlgt(VirtualKeyboardControl virtualKeyboardControl);

  public class VirtualKeyboardSettings
  {
    protected VirtualKeyboardTextStyle _textStyle = VirtualKeyboardTextStyle.None;
    protected RectangleF? _elementArrangeBounds = null;

    /// <summary>
    /// Gets or sets a value indicating how the virtual keyboard shows the edited text.
    /// </summary>
    public VirtualKeyboardTextStyle TextStyle
    {
      get { return _textStyle; }
      set { _textStyle = value; }
    }

    /// <summary>
    /// Gets or sets the bounds of the UI element whose text the virtual keyboard will edit.
    /// The virtual keyboard will arrange around the given rectangle, if possible.
    /// </summary>
    public RectangleF? ElementArrangeBounds
    {
      get { return _elementArrangeBounds; }
      set { _elementArrangeBounds = value; }
    }
  }

  /// <summary>
  /// Control which shows a virtual keyboard.
  /// </summary>
  /// <remarks>
  /// <para>
  /// The virtual keyboard control shows a complete keyboard with several virtual keys for numbers, letters and special characters on the screen.
  /// If <see cref="ShowVisibleText"/> is set to <c>true</c>, the control also shows the currently edited text.
  /// </para>
  /// <para>
  /// The virtual keyboard control provides the handling for normal text and password text. If the
  /// <see cref="VirtualKeyboardTextStyle.PasswordText"/> is set as text style in the settings which have been provided by the caller,
  /// the visible text consists always of <c>'*'</c> characters.
  /// </para>
  /// <para>
  /// Internally, the virtual keyboard only assembles the text characters which have been given by the virtual keyboard screen. Furthermore,
  /// it maintains some boolean variables to control the state of modificator keys like Shift, Control, AltGr and some more keys for diacritical signs.
  /// </para>
  /// <para>
  /// The "outer part" of the virtual keyboard, that means the VK frame, can be styled like other controls im MP2.
  /// The "inner part", which contains the keys, cannot be styled.
  /// Instead, the layouting of the actual keyboard part and the complete set of available keys is controlled by the localized virtual keyboard control
  /// which is provided by localization plugins. This class loads the <see cref="ControlTemplate"/> of name <c>VirtualKeyboardLayout_XX</c>,
  /// where XX is replaced by the two letter ISO language name of the current language.
  /// If that resource is not found, we fall back to the <see cref="ControlTemplate"/> resource of name <c>VirtualKeyboardLayout</c>.
  /// </para>
  /// <para>
  /// This control contains the boolean state variables for the modificator keys and the logic to control the interaction between them. For example
  /// the shift key cannot be pressed at the same time as the caps lock key. And only one diacritical sign can be active at a time and can only be
  /// combined with shift or caps lock. The diacritic state will be automatically be reset after a character is typed at the keyboard.
  /// </para>
  /// <para>
  /// See the default virtual keyboard layout control template as an example how this control and the UI play together.
  /// </para>
  /// </remarks>
  public class VirtualKeyboardControl : Control
  {
    #region Consts

    public const string VIRTUAL_KEYBOARD_RESOURCE_PREFIX = "VirtualKeyboardLayout";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected AbstractProperty _textProperty = null;
    protected VirtualKeyboardSettings _settings = null;
    protected bool _updateKeyboardControl = true;

    protected AbstractProperty _visibleTextProperty = new SProperty(typeof(string), string.Empty);
    protected AbstractProperty _shiftStateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _capsLockStateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _altGrStateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic1StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic2StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic3StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic4StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic5StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic6StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic7StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic8StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic9StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic10StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic11StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic12StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic13StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic14StateProperty = new SProperty(typeof(bool), false);
    protected AbstractProperty _diacritic15StateProperty = new SProperty(typeof(bool), false);

    // Will be automtically set if one diacritic is active
    protected AbstractProperty _diacriticActiveProperty = new SProperty(typeof(bool), false);

    protected ICollection<AbstractProperty> _diacriticProperties;

    #endregion

    #region Ctor

    public VirtualKeyboardControl()
    {
      _diacriticProperties = new List<AbstractProperty>
        {
            _diacritic1StateProperty,
            _diacritic2StateProperty,
            _diacritic3StateProperty,
            _diacritic4StateProperty,
            _diacritic5StateProperty,
            _diacritic6StateProperty,
            _diacritic7StateProperty,
            _diacritic8StateProperty,
            _diacritic9StateProperty,
            _diacritic10StateProperty,
            _diacritic11StateProperty,
            _diacritic12StateProperty,
            _diacritic13StateProperty,
            _diacritic14StateProperty,
            _diacritic15StateProperty,
        };
      IsVisible = false;
      Attach();
      SubscribeToMessages();
    }

    public override void Dispose()
    {
      UnsubscribeFromMessages();
      base.Dispose();
    }

    void Attach()
    {
      _shiftStateProperty.Attach(OnShiftStateChanged);
      _capsLockStateProperty.Attach(OnCapsLockStateChanged);
      _altGrStateProperty.Attach(OnAltGrStateChanged);
      foreach (AbstractProperty property in _diacriticProperties)
        property.Attach(OnDiacriticStateChanged);
    }

    // Not used
    //void Detach()
    //{
    //  _shiftStateProperty.Detach(OnShiftStateChanged);
    //  _capsLockStateProperty.Detach(OnCapsLockStateChanged);
    //  foreach (AbstractProperty property in _diacriticProperties)
    //    property.Detach(OnDiacriticStateChanged);
    //}

    #endregion

    void OnShiftStateChanged(AbstractProperty prop, object oldVal)
    {
      if (ShiftState)
        CapsLockState = false;
    }

    void OnCapsLockStateChanged(AbstractProperty prop, object oldVal)
    {
      if (CapsLockState)
        ShiftState = false;
    }

    void OnAltGrStateChanged(AbstractProperty prop, object oldVal)
    {
      // Diacritics and Alt Gr cannot be set at the same time
      if (AltGrState)
        ResetDiacritics(null);
    }

    void OnDiacriticStateChanged(AbstractProperty prop, object oldVal)
    {
      if ((bool) prop.GetValue())
      {
        // Only one diacritic can be active at a time (is this correct for all languages? -> if not, this algorithm has to be changed)
        ResetDiacritics(prop);
        // Diacritics and Alt Gr cannot be set at the same time
        AltGrState = false;
        DiacriticActive = true;
      }
      else
        DiacriticActive = CheckDiacriticActive();
    }

    void SubscribeToMessages()
    {
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            LocalizationMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == LocalizationMessaging.CHANNEL)
      {
        if (((LocalizationMessaging.MessageType) message.MessageType) ==
            LocalizationMessaging.MessageType.LanguageChanged)
          _updateKeyboardControl = true; // Discard former cached keyboard layout controls because we now need controls for another language
      }
    }

    #region Public members

    public event VirtualKeyboardCloseDlgt Closed;

    /// <summary>
    /// Gets or sets the text which is edited by the virtual keyboard. This property can also be bound
    /// to a text box, for example.
    /// </summary>
    public string Text
    {
      get
      {
        AbstractProperty textProperty = _textProperty;
        if (textProperty == null)
          return string.Empty;
        return (string) textProperty.GetValue();
      }
      set
      {
        AbstractProperty textProperty = _textProperty;
        if (textProperty == null)
          return;
        string val = value ?? string.Empty;
        textProperty.SetValue(val);
        if (_settings == null)
          VisibleText = string.Empty;
        else if (_settings.TextStyle == VirtualKeyboardTextStyle.PasswordText)
          VisibleText = StringUtils.Repeat("*", val.Length);
        else
          VisibleText = val;
      }
    }

    public AbstractProperty VisibleTextProperty
    {
      get { return _visibleTextProperty; }
    }

    /// <summary>
    /// Gets or sets the text to be displayed in the UI, if <see cref="ShowVisibleText"/> is <c>true</c>.
    /// This property contains <c>'*'</c> characters if the text style is set to <see cref="VirtualKeyboardTextStyle.PasswordText"/>
    /// in the settings.
    /// </summary>
    public string VisibleText
    {
      get { return (string) _visibleTextProperty.GetValue(); }
      protected set { _visibleTextProperty.SetValue(value); }
    }

    /// <summary>
    /// Returns <c>true</c> if the virtual keyboard UI shoud show the <see cref="VisibleText"/>.
    /// </summary>
    public bool ShowVisibleText
    {
      get { return _settings == null ? false : _settings.TextStyle != VirtualKeyboardTextStyle.None; }
    }

    public AbstractProperty ShiftStateProperty
    {
      get { return _shiftStateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the "Shift" key. This property doesn't change any internal processing,
    /// it is only an indicator for the GUI to display other keys. The shift state will automatically be reset
    /// when a key is pressed.
    /// </summary>
    public bool ShiftState
    {
      get { return (bool) _shiftStateProperty.GetValue(); }
      set { _shiftStateProperty.SetValue(value); }
    }

    public AbstractProperty CapsLockStateProperty
    {
      get { return _capsLockStateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the "Caps lock" key. This property doesn't change any internal processing,
    /// it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool CapsLockState
    {
      get { return (bool) _capsLockStateProperty.GetValue(); }
      set { _capsLockStateProperty.SetValue(value); }
    }

    public AbstractProperty AltGrStateProperty
    {
      get { return _altGrStateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the "Alt Gr" key. This property doesn't change any internal processing,
    /// it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool AltGrState
    {
      get { return (bool) _altGrStateProperty.GetValue(); }
      set { _altGrStateProperty.SetValue(value); }
    }

    #region Diacritics

    public AbstractProperty Diacritic1StateProperty
    {
      get { return _diacritic1StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 1.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic1State
    {
      get { return (bool) _diacritic1StateProperty.GetValue(); }
      set { _diacritic1StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic2StateProperty
    {
      get { return _diacritic2StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 2.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic2State
    {
      get { return (bool) _diacritic2StateProperty.GetValue(); }
      set { _diacritic2StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic3StateProperty
    {
      get { return _diacritic3StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 3.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic3State
    {
      get { return (bool) _diacritic3StateProperty.GetValue(); }
      set { _diacritic3StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic4StateProperty
    {
      get { return _diacritic4StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 4.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic4State
    {
      get { return (bool) _diacritic4StateProperty.GetValue(); }
      set { _diacritic4StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic5StateProperty
    {
      get { return _diacritic5StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 5.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic5State
    {
      get { return (bool) _diacritic5StateProperty.GetValue(); }
      set { _diacritic5StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic6StateProperty
    {
      get { return _diacritic6StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 6.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic6State
    {
      get { return (bool) _diacritic6StateProperty.GetValue(); }
      set { _diacritic6StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic7StateProperty
    {
      get { return _diacritic7StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 7.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic7State
    {
      get { return (bool) _diacritic7StateProperty.GetValue(); }
      set { _diacritic7StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic8StateProperty
    {
      get { return _diacritic8StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 8.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic8State
    {
      get { return (bool) _diacritic8StateProperty.GetValue(); }
      set { _diacritic8StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic9StateProperty
    {
      get { return _diacritic9StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 9.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic9State
    {
      get { return (bool) _diacritic9StateProperty.GetValue(); }
      set { _diacritic9StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic10StateProperty
    {
      get { return _diacritic10StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 10.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic10State
    {
      get { return (bool) _diacritic10StateProperty.GetValue(); }
      set { _diacritic10StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic11StateProperty
    {
      get { return _diacritic11StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 11.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic11State
    {
      get { return (bool) _diacritic11StateProperty.GetValue(); }
      set { _diacritic11StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic12StateProperty
    {
      get { return _diacritic12StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 12.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic12State
    {
      get { return (bool) _diacritic12StateProperty.GetValue(); }
      set { _diacritic12StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic13StateProperty
    {
      get { return _diacritic13StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 13.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic13State
    {
      get { return (bool) _diacritic13StateProperty.GetValue(); }
      set { _diacritic13StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic14StateProperty
    {
      get { return _diacritic14StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 14.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic14State
    {
      get { return (bool) _diacritic14StateProperty.GetValue(); }
      set { _diacritic14StateProperty.SetValue(value); }
    }

    public AbstractProperty Diacritic15StateProperty
    {
      get { return _diacritic15StateProperty; }
    }

    /// <summary>
    /// Gets or sets the current state of the keyboard modificator key for the diacritic 15.
    /// This property doesn't change any internal processing, it is only an indicator for the GUI to display other keys.
    /// </summary>
    public bool Diacritic15State
    {
      get { return (bool) _diacritic15StateProperty.GetValue(); }
      set { _diacritic15StateProperty.SetValue(value); }
    }

    public AbstractProperty DiacriticActiveProperty
    {
      get { return _diacriticActiveProperty; }
    }

    /// <summary>
    /// Gets or sets an indicator which is <c>true</c> if and only if a diacritic state is set.
    /// This is a convenience property to make the styling easier. It's state is automatically updated by this class.
    /// </summary>
    public bool DiacriticActive
    {
      get { return (bool) _diacriticActiveProperty.GetValue(); }
      internal set { _diacriticActiveProperty.SetValue(value); }
    }

    #endregion

    /// <summary>
    /// Adds a character at the end of the edited text.
    /// </summary>
    /// <param name="character">Character to be added.</param>
    public void Character(char character)
    {
      Text += character;
      ShiftState = false;
      ResetDiacritics(null);
    }

    /// <summary>
    /// Adds a space at the end of the edited text.
    /// </summary>
    public void Space()
    {
      Text += ' ';
    }

    /// <summary>
    /// Removes the last character from the end of the edited text.
    /// </summary>
    public void Backspace()
    {
      string oldText = Text;
      if (string.IsNullOrEmpty(oldText))
        return;
      Text = oldText.Substring(0, oldText.Length - 1);
    }

    public void Cut()
    {
      ServiceRegistration.Get<IClipboardManager>().SetClipboardText(Text);
      Text = string.Empty;
    }

    public void Copy()
    {
      ServiceRegistration.Get<IClipboardManager>().SetClipboardText(Text);
    }

    public void Paste()
    {
      string text;
      if (ServiceRegistration.Get<IClipboardManager>().GetClipboardText(out text))
        Text = text;
    }

    public void Show(AbstractProperty textProperty, VirtualKeyboardSettings settings)
    {
      InitializeStates();
      lock (_renderLock)
      {
        _settings = settings;
        _textProperty = textProperty;
      }
      if (_updateKeyboardControl)
      {
        _updateKeyboardControl = false;
        UpdateKeyboardControl();
      }
      IsVisible = true;
      TrySetFocus(true);
    }

    public void Close()
    {
      _settings = null;
      _textProperty = null;
      IsVisible = false;
      FireClosed();
    }

    protected bool CheckDiacriticActive()
    {
      return _diacriticProperties.Any(property => (bool) property.GetValue());
    }

    protected void InitializeStates()
    {
      ShiftState = false;
      AltGrState = false;
      CapsLockState = false;
      ResetDiacritics(null);
    }

    protected void ResetDiacritics(AbstractProperty exceptThis)
    {
      foreach (AbstractProperty property in _diacriticProperties)
        if (!ReferenceEquals(property, exceptThis))
          property.SetValue(false);
    }

    protected void FireClosed()
    {
      VirtualKeyboardCloseDlgt dlgt = Closed;
      if (dlgt != null)
        dlgt(this);
    }

    protected ControlTemplate FindKeyboardLayout()
    {
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      return FindKeyboardLayout(localization.CurrentCulture);
    }

    protected ControlTemplate FindKeyboardLayout(CultureInfo culture)
    {
      // Try culture specific keyboard layouts
      object o = FindResourceInTheme(VIRTUAL_KEYBOARD_RESOURCE_PREFIX + '_' + culture.TwoLetterISOLanguageName, false);
      ControlTemplate result = o as ControlTemplate;
      if (result != null)
        return result;
      if (o != null)
        MPF.TryCleanupAndDispose(o);
      if (!culture.IsNeutralCulture)
        return FindKeyboardLayout(culture.Parent);
      // Fallback: Take default keyboard
      o = FindResourceInTheme(VIRTUAL_KEYBOARD_RESOURCE_PREFIX, false);
      result = o as ControlTemplate;
      if (result == null && o != null)
        MPF.TryCleanupAndDispose(o);
      return result;
    }

    protected object FindResourceInTheme(string resourceKey, bool activateBindings)
    {
      object result = SkinContext.SkinResources.FindStyleResource(resourceKey);
      if (result == null)
        return null;
      // See comment about the copying in ResourceDictionary.FindResourceInParserContext()
      result = MpfCopyManager.DeepCopyCutLVPs(result);
      return result;
    }

    protected VirtualKeyboardPresenter FindVirtualKeyboardPresenter()
    {
      return TemplateControl == null ? null : TemplateControl.FindElement(
          new TypeMatcher(typeof(VirtualKeyboardPresenter))) as VirtualKeyboardPresenter;
    }

    protected void UpdateKeyboardControl()
    {
      VirtualKeyboardPresenter presenter = FindVirtualKeyboardPresenter();
      if (presenter == null)
        return;
      ControlTemplate keyboardLayout = FindKeyboardLayout();
      if (keyboardLayout == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("VirtualKeyboardControl: Could not find style resource for virtual keyboard");
        return;
      }
      FrameworkElement keyboardControl = keyboardLayout.LoadContent(this) as FrameworkElement;
      presenter.SetKeyboardLayoutControl(this, keyboardControl);
    }

    protected override void ArrangeTemplateControl()
    {
      FrameworkElement templateControl = _initializedTemplateControl;
      if (templateControl == null)
        return;
      lock (_renderLock)
        if (_settings == null)
          return;
      RectangleF? elementArrangeBounds = _settings.ElementArrangeBounds;
      SizeF keyboardSize = templateControl.DesiredSize;
      RectangleF actualBounds = ActualBounds;
      if (actualBounds.Size.Width < keyboardSize.Width)
        keyboardSize.Width = actualBounds.Size.Width;
      if (actualBounds.Size.Height < keyboardSize.Height)
        keyboardSize.Height = actualBounds.Size.Height;
      RectangleF keyboardRect;
      if (elementArrangeBounds.HasValue)
        // Arrange above or below elementArrangeBounds, horizontally centered in elementArrangeBounds
        keyboardRect = SharpDXExtensions.CreateRectangleF(new PointF(
            elementArrangeBounds.Value.Left + elementArrangeBounds.Value.Width / 2 - keyboardSize.Width / 2,
            elementArrangeBounds.Value.Bottom + keyboardSize.Height > actualBounds.Bottom ?
            elementArrangeBounds.Value.Top - keyboardSize.Height : elementArrangeBounds.Value.Bottom),
            keyboardSize);
      else
        // Center in actualBounds
        keyboardRect = new RectangleF(
            actualBounds.Left + (actualBounds.Width - keyboardSize.Width) / 2,
            actualBounds.Top + (actualBounds.Height - keyboardSize.Height) / 2,
            keyboardSize.Width, keyboardSize.Height);
      if (keyboardRect.Left < actualBounds.Left)
        keyboardRect.X = actualBounds.Left;
      if (keyboardRect.Right > actualBounds.Right)
        keyboardRect.X = actualBounds.Right - keyboardSize.Width;
      templateControl.Arrange(keyboardRect);
    }

    internal override void OnKeyPressed(ref Key key)
    {
      base.OnKeyPressed(ref key);
      FrameworkElement templateControl = TemplateControl;
      if (IsVisible && key == Key.Ok && templateControl != null && !templateControl.IsMouseOver)
        Close();
    }

    #endregion
  }
}
