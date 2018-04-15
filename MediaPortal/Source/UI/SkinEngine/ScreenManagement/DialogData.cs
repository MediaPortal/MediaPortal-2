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

using System;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.UI.SkinEngine.ScreenManagement
{
  public class DialogData : IDialogData
  {
    protected Screen _dialogScreen;
    protected DialogCloseCallbackDlgt _closeCallback;

    public DialogData(Screen dialogScreen, DialogCloseCallbackDlgt closeCallback)
    {
      _dialogScreen = dialogScreen;
      _closeCallback = closeCallback;
    }

    public Screen DialogScreen
    {
      get { return _dialogScreen; }
    }

    public string DialogName
    {
       get { return _dialogScreen.ResourceName; }
    }

    public Guid DialogInstanceId
    {
      get { return _dialogScreen.ScreenInstanceId; }
    }

    public DialogCloseCallbackDlgt CloseCallback
    {
      get { return _closeCallback; }
    }

    public override string ToString()
    {
      return _dialogScreen.ToString();
    }
  }
}