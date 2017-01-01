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
using UPnP.Infrastructure.CP.Description;
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