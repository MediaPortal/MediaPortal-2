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
using System.Threading;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Utilities;

namespace MediaPortal.UI.SkinEngine.InputManagement
{
  public class ClipboardManager : IClipboardManager
  {
    #region Consts

    protected const int MAX_WAIT_MS = 500;

    #endregion

    #region Static fields and instance

    private static readonly object _syncObj = new object();
    private static ClipboardManager _instance;

    public static ClipboardManager Instance
    {
      get
      {
        lock (_syncObj)
          if (_instance == null)
            _instance = new ClipboardManager();
        return _instance;
      }
    }

    #endregion

    /// <summary>
    /// Tries to get text contents from the Windows clipboard. If the clipboard contains any other type then text, the returned result will be <c>false</c>.
    /// </summary>
    /// <returns><c>true</c> if text could be retrieved.</returns>
    public bool GetClipboardText(out string text)
    {
      string result = null;
      ManualResetEvent finishedEvent = new ManualResetEvent(false);
      Thread thread = ThreadingUtils.RunSTAThreaded(() => GetClipboardText_STA(finishedEvent, ref result));
      if (!finishedEvent.WaitOne(MAX_WAIT_MS))
        thread.Abort();

      finishedEvent.Close();
      text = result;
      return !string.IsNullOrWhiteSpace(result);
    }

    /// <summary>
    /// Tries to set text contents into Windows clipboard.
    /// </summary>
    /// <param name="text">Text to copy.</param>
    /// <returns><c>true</c> if text was set.</returns>
    public bool SetClipboardText(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return false;

      ManualResetEvent finishedEvent = new ManualResetEvent(false);
      Thread thread = ThreadingUtils.RunSTAThreaded(() => SetClipboardText_STA(finishedEvent, text));

      if (!finishedEvent.WaitOne(MAX_WAIT_MS))
        thread.Abort();

      finishedEvent.Close();
      return true;
    }

    /// <summary>
    /// Copies the <paramref name="text"/> into the clipboard. This methods must be executed inside a STA thread!
    /// </summary>
    protected void SetClipboardText_STA(EventWaitHandle finishedEvent, string text)
    {
      try
      {
        if (!string.IsNullOrWhiteSpace(text))
          Clipboard.SetText(text);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ClipboardManager: Error getting text data from clipboard.", ex);
      }
      finally
      {
        finishedEvent.Set();
      }
    }

    /// <summary>
    /// Gets the contents of clipboard into <paramref name="text"/>. This methods must be executed inside a STA thread!
    /// </summary>
    protected void GetClipboardText_STA(EventWaitHandle finishedEvent, ref string text)
    {
      try
      {
        if (Clipboard.ContainsData(DataFormats.Text))
          text = Clipboard.GetText(TextDataFormat.Text);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("ClipboardManager: Error getting text data from clipboard.", ex);
      }
      finally
      {
        finishedEvent.Set();
      }
    }
  }
}
