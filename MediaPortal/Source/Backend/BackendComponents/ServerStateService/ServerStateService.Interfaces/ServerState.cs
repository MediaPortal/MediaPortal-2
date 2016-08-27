using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.ServerStateService.Interfaces
{
  public class ServerState
  {
    public ServerState(Guid id, object state)
    {
      Id = id;
      SerializeState(state);
    }
    
    internal ServerState() { }

    public Guid Id { get; set; }
    public string StateTypeName { get; set; }
    public string SerializedState { get; set; }

    public void SerializeState(object state)
    {
      StateTypeName = state.GetType().AssemblyQualifiedName;
      SerializedState = ServerStateSerializer.Serialize(state);
    }

    public object DeserializeState()
    {
      return ServerStateSerializer.Deserialize(StateTypeName, SerializedState);
    }
  }
}
