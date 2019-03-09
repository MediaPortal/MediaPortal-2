#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Text;

namespace UPnP.Infrastructure.Utils
{
  public class HashGenerator
  {
    public static UInt32 CalculateHash(UInt32 seed, string str)
    {
      return CalculateHash(seed, Encoding.UTF8.GetBytes(str));
    }

    public static UInt32 CalculateHash(UInt32 seed, byte[] buffer)
    {
      return CalculateHash(seed, buffer, 0, buffer.Length);
    }

    public static UInt32 CalculateHash(UInt32 seed, byte[] buffer, int start, int size)
    {
      UInt32 hash = seed;

      for (int i = start; i < size; i++)
        unchecked {
          hash = (hash << 4) + buffer[i];
          UInt32 work = (hash & 0xf0000000);
          if (work != 0)
            hash ^= (work >> 24);
          hash &= ~work;
        }
      return hash;
    }
  }
}
