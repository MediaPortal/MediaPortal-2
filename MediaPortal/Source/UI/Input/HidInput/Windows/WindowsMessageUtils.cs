#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Windows.Forms;

namespace HidInput.Windows
{
  public static class WindowsMessageUtils
  {
    public const int WM_KEYDOWN = 0x100;
    public const int WM_KEYUP = 0x101;
    public const int WM_SYSKEYDOWN = 0x0104;
    public const int WM_SYSKEYUP = 0x0105;
    public const int WM_APPCOMMAND = 0x0319;

    /// <summary>
    /// Normalizes a Windows message id by returning the id of WM_KEYDOWN/UP for WM_SYSKEYDOWN/UP messages. 
    /// </summary>
    /// <param name="messageId">The message id to normalize.</param>
    /// <returns><see cref="WM_KEYDOWN"/> if <paramref name="messageId"/> was <see cref="WM_SYSKEYDOWN"/>,
    /// <see cref="WM_KEYUP"/> if <paramref name="messageId"/> was <see cref="WM_SYSKEYUP"/>;
    /// else returns the original <paramref name="messageId"/>.</returns>
    public static int NormalizeKeyMessageId(int messageId)
    {
      switch (messageId)
      {
        case WM_KEYDOWN:
        case WM_SYSKEYDOWN:
          return WM_KEYDOWN;
        case WM_KEYUP:
        case WM_SYSKEYUP:
          return WM_KEYUP;
        default:
          return messageId;
      }
    }

    public static int GetRepeatCount(ref Message message)
    {
      return (int)(message.LParam.ToInt64() & 0xFFFF);
    }

    public static void SetRepeatCount(ref Message message, int repeatCount)
    {
      message.LParam = new IntPtr((int)(message.LParam.ToInt64() & 0xFFFF0000) | (repeatCount & 0xFFFF));
    }

    public static AppCommand GetAppCommand(ref Message message)
    {
      return (AppCommand)((message.LParam.ToInt64() >> 16) & 0xFFFF);
    }
  }
}
