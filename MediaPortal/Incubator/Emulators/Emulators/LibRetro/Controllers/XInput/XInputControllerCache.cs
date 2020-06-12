using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.LibRetro.Controllers.XInput
{
  /// <summary>
  /// Helper class to cache the connected state of controllers.
  /// Polling the connected state of disconnected controllers causes high CPU load if done repeatedly.
  /// This class only updates the connected state every cacheTimeoutMs milliseconds
  /// </summary>
  class XInputControllerCache
  {
    protected Controller _controller;
    protected bool _isConnected;
    protected int _cacheTimeoutMs;
    protected DateTime _lastCheck = DateTime.MinValue;

    public XInputControllerCache(Controller controller, int cacheTimeoutMs)
    {
      _controller = controller;
      _cacheTimeoutMs = cacheTimeoutMs;
    }

    public Controller Controller { get { return _controller; } }

    public bool IsConnected()
    {
      State state;
      return GetState(out state);
    }

    public bool GetState(out State state)
    {
      DateTime now = DateTime.Now;
      if (!_isConnected && (now - _lastCheck).TotalMilliseconds < _cacheTimeoutMs)
      {
        state = default(State);
        return false;
      }
      _lastCheck = now;
      _isConnected = _controller.GetState(out state);
      return _isConnected;
    }
  }
}
