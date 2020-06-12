using SharpLib.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;
using MediaPortal.Common;
using System.Runtime.InteropServices;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.General;
using System.Windows.Forms;
using SharpLib.Hid;
using SharpLib.Hid.Usage;

namespace Emulators.LibRetro.Controllers.Hid
{
  public class HidStateEventArgs : EventArgs
  {
    public HidStateEventArgs(HidState state)
    {
      State = state;
    }
    public HidState State { get; private set; }
  }

  public class HidListener : IDisposable
  {
    #region Logger
    static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
    #endregion

    protected Handler _handler;
    protected AsynchronousMessageQueue _messageQueue;

    public event EventHandler<HidStateEventArgs> StateChanged;
    protected virtual void OnStateChanged(HidStateEventArgs e)
    {
      var handler = StateChanged;
      if (handler != null)
        handler(this, e);
    }

    #region Message handling
    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { WindowsMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected virtual void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WindowsMessaging.CHANNEL)
      {
        WindowsMessaging.MessageType messageType = (WindowsMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case WindowsMessaging.MessageType.WindowsBroadcast:
            Message msg = (Message)message.MessageData[WindowsMessaging.MESSAGE];
            Handler handler = _handler;
            if (handler != null)
              handler.ProcessInput(ref msg);
            break;
        }
      }
    }
    #endregion

    public void Register(IntPtr Hwnd)
    {
      RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
      rid[0].usUsagePage = (ushort)UsagePage.GenericDesktopControls;
      rid[0].usUsage = (ushort)SharpLib.Hid.UsageCollection.GenericDesktop.GamePad;
      rid[0].dwFlags = 0; // Const.RIDEV_EXINPUTSINK; //Handle background events
      rid[0].hwndTarget = Hwnd;

      int repeatDelay = -1;
      int repeatSpeed = -1;
      _handler = new Handler(rid, true, repeatDelay, repeatSpeed);
      if (!_handler.IsRegistered)
      {
        Logger.Warn("Failed to register raw input devices: " + Marshal.GetLastWin32Error().ToString());
      }
      _handler.OnHidEvent += OnHidEvent;
      SubscribeToMessages();
    }

    void OnHidEvent(object aSender, Event aHidEvent)
    {
      if (!aHidEvent.Device.IsGamePad)
        return;

#if DEBUG
      if (aHidEvent.IsRepeat)
      {
        Logger.Debug("HID: Repeat");
      }
#endif
      HashSet<ushort> buttons = new HashSet<ushort>();
      foreach (ushort usage in aHidEvent.Usages)
        buttons.Add(usage);

      //For each axis
      Dictionary<ushort, HidAxisState> axisStates = new Dictionary<ushort, HidAxisState>();
      foreach (KeyValuePair<HIDP_VALUE_CAPS, uint> entry in aHidEvent.UsageValues)
      {
        //HatSwitch is handled separately as direction pad state
        if (entry.Key.IsRange || entry.Key.NotRange.Usage == (ushort)GenericDesktop.HatSwitch)
          continue;
        //Get our usage type
        Type usageType = HidUtils.UsageType((UsagePage)entry.Key.UsagePage);
        if (usageType == null)
          continue;
        //Get the name of our axis
        string name = Enum.GetName(usageType, entry.Key.NotRange.Usage);
        ushort index = entry.Key.NotRange.DataIndex;
        axisStates[index] = new HidAxisState(name, index, entry.Value, entry.Key.BitSize);
      }

      DirectionPadState directionPadState = aHidEvent.GetDirectionPadState();

      HidState state = new HidState
      {
        VendorId = aHidEvent.Device.VendorId,
        ProductId = aHidEvent.Device.ProductId,
        Name = aHidEvent.Device.Name,
        FriendlyName = aHidEvent.Device.FriendlyName,
        Buttons = buttons,
        AxisStates = axisStates,
        DirectionPadState = directionPadState
      };
      OnStateChanged(new HidStateEventArgs(state));
    }

    public void Dispose()
    {
      UnsubscribeFromMessages();
      if (_handler != null)
      {
        _handler.Dispose();
        _handler = null;
      }
    }
  }
}