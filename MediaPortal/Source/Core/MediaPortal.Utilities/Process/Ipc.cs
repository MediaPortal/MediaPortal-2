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
using System.Text;

namespace MediaPortal.Utilities.Process
{
  public static class Ipc
  {
    /// <summary>
    /// IPC command id's
    /// </summary>
    /// <remarks>
    /// If the highest bit is set, no response is expected
    /// </remarks>
    public enum Command : ushort
    {
      GetProcessId = 0x0001,
      Shutdown = 0x0002
    }

    /// <summary>
    /// IPC response codes
    /// </summary>
    /// <remarks>
    /// Negative (error) responses are indicated by the highest bit set.
    /// </remarks>
    public enum ResponseCode : ushort
    {
      Ok = 0x0000,
      False = 0x0001,

      UnknownCommand = 0x0001 | NEGATIVE_RESPONSE_CODE,
      InvalidData = 0x0002 | NEGATIVE_RESPONSE_CODE,
      ServerException = 0x0003 | NEGATIVE_RESPONSE_CODE
    }

    public const ushort NO_RESPONSE_COMMAND = 0x8000;

    public const ushort NEGATIVE_RESPONSE_CODE = 0x8000;

    public static readonly string PIPE_PREFIX = "MP2IPC_";

    /// <summary>
    /// Writes a string into an byte array in UTF8 encodung including 2 byte length information up front
    /// </summary>
    /// <param name="str">String to encode</param>
    /// <param name="bytes">byte array to write to</param>
    /// <param name="bytesOffset">Currefnt offset in <paramref name="bytes"/>. After the operation the offset is set to the 1st byte after the string.</param>
    public static void StringToBytes(string str, byte[] bytes, ref int bytesOffset)
    {
      if (String.IsNullOrEmpty(str))
      {
        BitConverter.GetBytes((ushort)0).CopyTo(bytes, bytesOffset);
        bytesOffset += 2;
      }
      else
      {
        BitConverter.GetBytes((ushort)Encoding.UTF8.GetByteCount(str)).CopyTo(bytes, bytesOffset);
        bytesOffset += 2;
        bytesOffset += Encoding.UTF8.GetBytes(str, 0, str.Length, bytes, bytesOffset);
      }
    }

    /// <summary>
    /// Reads a UTF8 endoced string with 2 byte length information up front from an byte array.
    /// </summary>
    /// <param name="bytes">Byte array to read from</param>
    /// <param name="bytesOffset">Offset to start reading from. After the operation the offset is set to the 1st byte after the string.</param>
    /// <returns>Returns the string.</returns>
    public static string BytesToString(byte[] bytes, ref int bytesOffset)
    {
      int length = BitConverter.ToUInt16(bytes, bytesOffset);
      bytesOffset += 2;
      if (length == 0)
        return String.Empty;
      try
      {
        return Encoding.UTF8.GetString(bytes, bytesOffset, length);
      }
      finally
      {
        bytesOffset += length;
      }
    }
  }
}