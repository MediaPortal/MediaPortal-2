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

using System;
using System.Collections.Generic;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Screens;

namespace MediaPortal.SkinEngine.ScreenManagement
{
  public class DialogManager : IDialogManager
  {
    class DialogResultCommand : ICommand
    {
      #region Protected fields

      protected DialogResultDlgt _resultDlgt;
      protected DialogResult _dialogResult;

      #endregion

      internal DialogResultCommand(DialogResultDlgt resultDlgt, DialogResult dialogResult)
      {
        _resultDlgt = resultDlgt;
        _dialogResult = dialogResult;
      }

      #region ICommand implementation

      public void Execute()
      {
        _resultDlgt(_dialogResult);
      }

      #endregion
    }

    public class GenericDialogData
    {
      #region Protected fields

      protected Property _headerTextProperty;
      protected Property _textProperty;
      protected ItemsList _dialogButtonsList;
      protected DialogResultDlgt _resultDlgt;

      #endregion

      internal GenericDialogData(string headerText, string text, ItemsList dialogButtons,
          DialogResultDlgt resultDlgt)
      {
        _headerTextProperty = new Property(typeof(string), headerText);
        _textProperty = new Property(typeof(string), text);
        _dialogButtonsList = dialogButtons;
        _resultDlgt = resultDlgt;
      }

      public Property HeaderTextProperty
      {
        get { return _headerTextProperty; }
      }

      public string HeaderText
      {
        get { return (string) _headerTextProperty.GetValue(); }
        set { _headerTextProperty.SetValue(value); }
      }

      public Property TextProperty
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

      public DialogResultDlgt ResultDlgt
      {
        get { return _resultDlgt; }
      }
    }

    #region Constants

    public const string KEY_NAME = "Name";

    public const string OK_BUTTON_TEXT = "[System.Ok]";
    public const string YES_BUTTON_TEXT = "[System.Yes]";
    public const string NO_BUTTON_TEXT = "[System.No]";
    public const string CANCEL_BUTTON_TEXT = "[System.Cancel]";

    public const string GENERIC_DIALOG_SCREEN = "generic_dialog";

    #endregion

    #region Protected fields

    protected GenericDialogData _dialogData = null;

    #endregion

    public DialogManager()
    {
    }

    public GenericDialogData CurrentDialogData
    {
      get { return _dialogData; }
      set { _dialogData = value; }
    }

    #region Protected methods

    protected static ListItem CreateButtonListItem(string buttonText, DialogResultDlgt resultDlgt,
        DialogResult dialogResult)
    {
      ListItem result = new ListItem(KEY_NAME, buttonText);
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      ICommand command = new MethodDelegateCommand(screenManager.CloseDialog);
      if (resultDlgt != null)
      {
        IList<ICommand> commands = new List<ICommand>
          {
              new DialogResultCommand(resultDlgt, dialogResult),
              command
          };
        command = new CommandList(commands);
      }
      result.Command = command;
      return result;
    }

    protected void Cleanup()
    {
      _dialogData = null;
    }

    protected void OnDialogCancelled(string dialogName)
    {
      if (_dialogData != null && dialogName == GENERIC_DIALOG_SCREEN)
      {
        if (_dialogData.ResultDlgt != null)
          _dialogData.ResultDlgt(DialogResult.Cancel);
        _dialogData = null;
      }
    }

    #endregion

    #region IDialogManager implementation

    public void ShowDialog(string headerText, string text, DialogType type,
        bool showCancelButton, DialogResultDlgt resultDlgt)
    {
      ItemsList buttons = new ItemsList();
      switch (type)
      {
        case DialogType.OkDialog:
          buttons.Add(CreateButtonListItem(OK_BUTTON_TEXT, resultDlgt, DialogResult.Ok));
          break;
        case DialogType.YesNoDialog:
          buttons.Add(CreateButtonListItem(YES_BUTTON_TEXT, resultDlgt, DialogResult.Yes));
          buttons.Add(CreateButtonListItem(NO_BUTTON_TEXT, resultDlgt, DialogResult.No));
          break;
        default:
          throw new NotImplementedException(string.Format("DialogManager: DialogType {0} is not implemented yet", type));
      }
      if (showCancelButton)
        buttons.Add(CreateButtonListItem(CANCEL_BUTTON_TEXT, resultDlgt, DialogResult.Cancel));

      CurrentDialogData = new GenericDialogData(headerText, text, buttons, resultDlgt);
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(GENERIC_DIALOG_SCREEN, new DialogCloseCallbackDlgt(OnDialogCancelled));
    }

    #endregion
  }
}