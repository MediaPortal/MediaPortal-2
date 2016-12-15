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

using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Players.Video
{
  public class BluRayEventBuffer
  {
    const int SIZE = 128;
    readonly AutoResetEvent _eventAvailableEvent = new AutoResetEvent(false);
    readonly BluRayAPI.BluRayEvent[] _buffer = new BluRayAPI.BluRayEvent[SIZE];
    int _readPos = 0;
    int _writePos = 0;
    public readonly object SyncObj = new object();

    public AutoResetEvent EventAvailable
    {
      get { return _eventAvailableEvent; }
    }

    public bool IsEmpty()
    {
      return _readPos == _writePos;
    }

    public int Count
    {
      get
      {
        int len = _writePos - _readPos;
        if (len < 0)
          len += SIZE;
        return len;
      }
    }

    public void Clear()
    {
      _writePos = 0;
      _readPos = 0;
    }

    public void Set(BluRayAPI.BluRayEvent data)
    {
      _buffer[_writePos] = data;
      _writePos = (_writePos + 1) % SIZE;
      if (_readPos == _writePos)
      {
        ServiceRegistration.Get<ILogger>().Warn("BluRayPlayer: Event buffer full");
      }
      _eventAvailableEvent.Set();
    }

    public BluRayAPI.BluRayEvent Peek()
    {
      return _buffer[_readPos];
    }

    public BluRayAPI.BluRayEvent Get()
    {
      int pos = _readPos;
      _readPos = (_readPos + 1) % SIZE;
      return _buffer[pos];
    }
  }
}
