using System;
using UPnP.Infrastructure.CP.DeviceTree;

namespace UPnP.Infrastructure.CP.GENA
{
  public class EventSubscription
  {
    protected string _sid;
    protected ServiceDescriptor _serviceDescriptor;
    protected CpService _service;
    protected DateTime _expiration;
    protected uint _eventKey = 0;

    public EventSubscription(string sid, ServiceDescriptor serviceDescriptor, CpService service, DateTime expiration)
    {
      _sid = sid;
      _serviceDescriptor = serviceDescriptor;
      _service = service;
      _expiration = expiration;
    }

    public string Sid
    {
      get { return _sid; }
    }

    public CpService Service
    {
      get { return _service; }
    }

    public ServiceDescriptor ServiceDescriptor
    {
      get { return _serviceDescriptor; }
    }

    public DateTime Expiration
    {
      get { return _expiration; }
      set { _expiration = value; }
    }

    public uint EventKey
    {
      get { return _eventKey; }
    }

    public bool SetNewEventKey(uint value)
    {
      ulong seq = value;
      ulong max_gap = _eventKey + GENAClientController.EVENTKEY_GAP_THRESHOLD;
      if (seq < _eventKey)
        seq += 2 << 32;
      if (seq <= max_gap)
      {
        _eventKey = value;
        return true;
      }
      return false;
    }
  }
}