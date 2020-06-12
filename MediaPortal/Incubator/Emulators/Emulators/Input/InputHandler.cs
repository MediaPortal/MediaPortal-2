using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.SkinEngine.InputManagement;
using MediaPortal.UI.SkinEngine.SkinManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Emulators.Input
{
  public class InputHandler : IDisposable
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    protected KeyboardHook _keyboardHook;
    protected Key _mappedKey;

    public event EventHandler MappedKeyPressed;

    public InputHandler(int processId, Key mappedKey)
    {
      _mappedKey = mappedKey;
      SkinContext.Form.BeginInvoke((MethodInvoker)(() =>
      {
        _keyboardHook = new KeyboardHook(processId, OnKeyPressed);
      }));
    }

    protected void OnKeyPressed(object sender, KeyEventArgs e)
    {
      Key key = InputMapper.MapSpecialKey(e);
      if (key != _mappedKey)
        return;
      e.Handled = true;
      e.SuppressKeyPress = true;
      var handler = MappedKeyPressed;
      if (handler != null)
        handler(this, EventArgs.Empty);
    }

    public void CloseWindow(IntPtr windowHandle, uint? sendKey)
    {
      if (windowHandle == IntPtr.Zero)
        return;

      int Msg;
      uint wParam;
      if (sendKey.HasValue)
      {
        Msg = KeyboardHook.WM_KEYDOWN;
        wParam = sendKey.Value;
      }
      else
      {
        Msg = KeyboardHook.WM_CLOSE;
        wParam = 0;
      }

      try
      {
        KeyboardHook.PostMessage(windowHandle, Msg, wParam, 0);
      }
      catch { }
    }

    public void Dispose()
    {
      SkinContext.Form.BeginInvoke((MethodInvoker)(() =>
        {
          if (_keyboardHook != null)
          {
            _keyboardHook.Dispose();
            _keyboardHook = null;
          }
        }));
    }
  }
}