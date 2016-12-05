using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.ServerStateService.Client.Models
{
  /// <summary>
  /// Model for exposing server states to the GUI
  /// </summary>
  public class ServerStateModel : BaseMessageControlledModel, IObservable
  {
    public static readonly Guid MODEL_ID = new Guid("B23D8DC1-405E-4564-92D0-E247C299FFAE");

    protected readonly object _syncObj = new object();
    protected bool _isInit = false;
    protected WeakEventMulticastDelegate _objectChanged = new WeakEventMulticastDelegate();
    protected IDictionary<Guid, object> _states = new SafeDictionary<Guid, object>();

    public ServerStateModel()
    {
      SubscribeToMessages();
    }

    /// <summary>
    /// Gets the state object with the given <paramref name="stateId"/>.
    /// </summary>
    /// <param name="stateId">The uniquie guid of the state.</param>
    /// <returns>The state object if found, otherwise null,</returns>
    public object this[Guid stateId]
    {
      get
      {
        lock (_syncObj)
        {
          object value;
          if (!_states.TryGetValue(stateId, out value))
            value = _states[stateId] = GetCurrentState(stateId);
          return value;
        }
      }
    }
    
    protected object GetCurrentState(Guid stateId)
    {
      object state;
      var ssm = ServiceRegistration.Get<IServerStateManager>();
      if (!ssm.TryGetState(stateId, out state))
        return null;
      return state;
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(ServerStateMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerStateMessaging.CHANNEL)
      {
        if (((ServerStateMessaging.MessageType)message.MessageType) == ServerStateMessaging.MessageType.StatesChanged)
        {
          var updatedStates = (IDictionary<Guid, object>)message.MessageData[ServerStateMessaging.STATES];
          bool fire;
          lock (_syncObj)
            fire = UpdateStates(updatedStates);
          if (fire)
            FireChange();
        }
      }
    }
    
    protected bool UpdateStates(IDictionary<Guid, object> states)
    {
      bool updated = false;
      foreach (var kvp in states)
        if (_states.ContainsKey(kvp.Key))
        {
          //only fire changed if its in our local cache, i.e. its been previously requested
          updated = true;
          _states[kvp.Key] = kvp.Value;
        }
      return updated;
    }

    #region IObservable

    /// <summary>
    /// Event which gets fired when the collection changes.
    /// </summary>
    public event ObjectChangedDlgt ObjectChanged
    {
      add { _objectChanged.Attach(value); }
      remove { _objectChanged.Detach(value); }
    }

    public void FireChange()
    {
      _objectChanged.Fire(new object[] { this });
    }

    #endregion
  }
}
