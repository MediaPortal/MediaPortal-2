#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public class DialogManager : IDialogManager
  {
    class DialogResultCommand : ICommand
    {
      #region Protected fields

      private readonly Guid _dialogHandle;
      private readonly DialogResult _dialogResult;

      #endregion

      internal DialogResultCommand(Guid dialogHandle, DialogResult dialogResult)
      {
        _dialogHandle = dialogHandle;
        _dialogResult = dialogResult;
      }

      #region ICommand implementation

      public void Execute()
      {
        DialogManagerMessaging.SendDialogManagerMessage(_dialogHandle, _dialogResult);
      }

      #endregion
    }

    public class GenericDialogData
    {
      #region Protected fields

      protected AbstractProperty _headerTextProperty;
      protected AbstractProperty _textProperty;
      protected ItemsList _dialogButtonsList;
      protected Guid _dialogHandle;

      #endregion

      internal GenericDialogData(string headerText, string text, ItemsList dialogButtons, Guid dialogHandle)
      {
        _headerTextProperty = new SProperty(typeof(string), headerText);
        _textProperty = new SProperty(typeof(string), text);
        _dialogButtonsList = dialogButtons;
        _dialogHandle = dialogHandle;
      }

      public AbstractProperty HeaderTextProperty
      {
        get { return _headerTextProperty; }
      }

      public string HeaderText
      {
        get { return (string) _headerTextProperty.GetValue(); }
        set { _headerTextProperty.SetValue(value); }
      }

      public AbstractProperty TextProperty
      {
        get { return _textProperty; }
      }

      public string Text
      {
        get { return (string) _textProperty.GetValue(); }
        set { _textProperty.SetValue(value); }
      }

      public ItemsList DialogButtons
      {
        get { return _dialogButtonsList; }
      }

      public Guid DialogHandle
      {
        get { return _dialogHandle; }
      }
    }

    #region Constants

    public const string KEY_NAME = "Name";

    public const string OK_BUTTON_TEXT = "[System.Ok]";
    public const string YES_BUTTON_TEXT = "[System.Yes]";
    public const string NO_BUTTON_TEXT = "[System.No]";
    public const string CANCEL_BUTTON_TEXT = "[System.Cancel]";

    public const string GENERIC_DIALOG_SCREEN = "GenericDialog";

    #endregion

    #region Protected fields

    protected GenericDialogData _dialogData = null;

    #endregion

    public GenericDialogData CurrentDialogData
    {
      get { return _dialogData; }
      set { _dialogData = value; }
    }

    #region Protected methods

    protected static ListItem CreateButtonListItem(string buttonText, Guid dialogHandle, DialogResult dialogResult,
        bool isDefault)
    {
      ListItem result = new ListItem(KEY_NAME, buttonText);
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      IList<ICommand> commands = new List<ICommand>
          {
              new DialogResultCommand(dialogHandle, dialogResult),
              new MethodDelegateCommand(screenManager.CloseDialog)
          };
      result.Command = new CommandList(commands);
      if (isDefault)
        result.AdditionalProperties["IsDefault"] = true;
      return result;
    }

    protected void Cleanup()
    {
      _dialogData = null;
    }

    protected void OnDialogClosed(string dialogName)
    {
      if (_dialogData != null && dialogName == GENERIC_DIALOG_SCREEN)
      {
        DialogManagerMessaging.SendDialogManagerMessage(_dialogData.DialogHandle, DialogResult.Cancel);
        _dialogData = null;
      }
    }

    #endregion

    #region IDialogManager implementation

    public Guid ShowDialog(string headerText, string text, DialogType type,
        bool showCancelButton, DialogButtonType? focusedButton)
    {
      Guid dialogHandle = Guid.NewGuid();
      ItemsList buttons = new ItemsList();
      switch (type)
      {
        case DialogType.OkDialog:
          buttons.Add(CreateButtonListItem(OK_BUTTON_TEXT, dialogHandle, DialogResult.Ok, focusedButton == DialogButtonType.Ok || !showCancelButton));
          break;
        case DialogType.YesNoDialog:
          buttons.Add(CreateButtonListItem(YES_BUTTON_TEXT, dialogHandle, DialogResult.Yes, focusedButton == DialogButtonType.Yes));
          buttons.Add(CreateButtonListItem(NO_BUTTON_TEXT, dialogHandle, DialogResult.No, focusedButton == DialogButtonType.No));
          break;
        default:
          throw new NotImplementedException(string.Format("DialogManager: DialogType {0} is not implemented yet", type));
      }
      if (showCancelButton)
        buttons.Add(CreateButtonListItem(CANCEL_BUTTON_TEXT, dialogHandle, DialogResult.Cancel, focusedButton == DialogButtonType.Cancel));

      CurrentDialogData = new GenericDialogData(headerText, text, buttons, dialogHandle);
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(GENERIC_DIALOG_SCREEN, OnDialogClosed);
      return dialogHandle;
    }

    #endregion
  }
}