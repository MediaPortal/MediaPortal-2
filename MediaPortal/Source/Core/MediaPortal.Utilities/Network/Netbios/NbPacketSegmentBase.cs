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
using System.Linq;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a segment of a Netbios Nameservice packet
  /// </summary>
  /// <remarks>
  /// Every NbNs packet consists of
  ///   1 <see cref="NbNsQuestionEntry"/> segment
  ///   0-n <see cref="NbNsResourceRecordBase"/> segments
  ///   0-n <see cref="NbNsResourceRecordBase"/> segments, whereas the <see cref="NbNsQuestionEntry"/> segments can be
  ///     0-n AnswerResourceRecord segments,
  ///     0-n AuthorityResourceRecord segments and 
  ///     0-n AdditionalResourceRecord segments.
  /// For details see http://tools.ietf.org/html/rfc1002#section-4.2.1
  /// Although the spec as described above in theory allows for 0-n <see cref="NbNsPacketBase"/> segments, only 0 or 1
  /// are allowed in a valid NbNs packet. The same applies to AnswerResourceRecords, AuthorityResourceRecords and AdditionalResourceRecords.
  /// For the <see cref="Length"/> class to be able to create the packet, it needs to know the <see cref="ByteArray"/>
  /// of each NbNs packet segment and the <see cref="NbPacketSegmentBase"/> representation of each NbNs packet segment.
  /// Each segment implementation therefore implements the <see cref="NbNsHeader"/> base class.
  /// Additionally, this base class contains helper methods used by different NbNs packet segments.
  /// </remarks>
  public abstract class NbPacketSegmentBase
  {
    #region Abstract properties

    /// <summary>
    /// Length of the NbNs packet segment
    /// </summary>
    public abstract int Length { get; }

    /// <summary>
    /// Byte array representation of the NbNs packet segment
    /// </summary>
    public abstract byte[] ByteArray { get; }

    #endregion

    #region Protected methods

    /// <summary>
    /// Converts the two bytes in <see cref="buffer"/> starting at <see cref="offset"/> into an <see cref="UInt16"/>
    /// </summary>
    /// <param name="buffer">Buffer to take the bytes from</param>
    /// <param name="offset">Zero based offset in <see cref="buffer"/></param>
    /// <returns></returns>
    protected static UInt16 BufferToUInt16(byte[] buffer, int offset)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");

      if (offset < 0 || buffer.Length < offset + 2)
        throw new ArgumentOutOfRangeException("offset");

      var twoBytes = buffer.Skip(offset).Take(2);
      return BitConverter.ToUInt16(BitConverter.IsLittleEndian ? twoBytes.Reverse().ToArray() : twoBytes.ToArray(), 0);
    }

    /// <summary>
    /// Converts <see cref="value"/> into two bytes and copies them into <see cref="buffer"/> starting at <see cref="offset"/>
    /// </summary>
    /// <param name="value"><see cref="UInt16"/> value to copy</param>
    /// <param name="buffer">buffer into which <see cref="value"/> should be copied</param>
    /// <param name="offset">Zero based offset in <see cref="buffer"/> into which <see cref="value"/> should be copied</param>
    /// <remarks>
    /// The length of <see cref="buffer"/> remains unchanged. The two bytes in <see cref="buffer"/> at positions
    /// <see cref="offset"/> and <see cref="offset"/> + 1 are overwritten.
    /// </remarks>
    protected static void UInt16ToBuffer(UInt16 value, byte[] buffer, int offset)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");

      if (offset < 0 || buffer.Length < offset + 2)
        throw new ArgumentOutOfRangeException("offset");

      var twoBytes = BitConverter.IsLittleEndian ? BitConverter.GetBytes(value).Reverse().ToArray() : BitConverter.GetBytes(value);
      buffer[offset] = twoBytes[0];
      buffer[offset + 1] = twoBytes[1];
    }

    /// <summary>
    /// Converts the four bytes in <see cref="buffer"/> starting at <see cref="offset"/> into an <see cref="UInt32"/>
    /// </summary>
    /// <param name="buffer">Buffer to take the bytes from</param>
    /// <param name="offset">Zero based offset in <see cref="buffer"/></param>
    /// <returns></returns>
    protected static UInt32 BufferToUInt32(byte[] buffer, int offset)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");

      if (offset < 0 || buffer.Length < offset + 4)
        throw new ArgumentOutOfRangeException("offset");

      var fourBytes = buffer.Skip(offset).Take(4);
      return BitConverter.ToUInt32(BitConverter.IsLittleEndian ? fourBytes.Reverse().ToArray() : fourBytes.ToArray(), 0);
    }

    /// <summary>
    /// Converts <see cref="value"/> into four bytes and copies them into <see cref="buffer"/> starting at <see cref="offset"/>
    /// </summary>
    /// <param name="value"><see cref="UInt32"/> value to copy</param>
    /// <param name="buffer">buffer into which <see cref="value"/> should be copied</param>
    /// <param name="offset">Zero based offset in <see cref="buffer"/> into which <see cref="value"/> should be copied</param>
    /// <remarks>
    /// The length of <see cref="buffer"/> remains unchanged. The four bytes in <see cref="buffer"/> starting at position
    /// <see cref="offset"/> are overwritten.
    /// </remarks>
    protected static void UInt32ToBuffer(UInt32 value, byte[] buffer, int offset)
    {
      if (buffer == null)
        throw new ArgumentNullException("buffer");

      if (offset < 0 || buffer.Length < offset + 4)
        throw new ArgumentOutOfRangeException("offset");

      var fourBytes = BitConverter.IsLittleEndian ? BitConverter.GetBytes(value).Reverse().ToArray() : BitConverter.GetBytes(value);
      fourBytes.CopyTo(buffer, offset);
    }

    /// <summary>
    /// Extracts the bits indicated by <see cref="separatorMask"/> from <see cref="value"/> and checks wether
    /// these bits from <see cref="value"/> are identical to <see cref="mask"/>
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <param name="separatorMask">Relevant bits</param>
    /// <param name="mask">Bits to compare with</param>
    /// <returns><c>true</c> if respective bits are identical, else <c>false</c></returns>
    protected bool CheckMask(UInt16 value, UInt16 separatorMask, UInt16 mask)
    {
      return ((value & separatorMask) & mask) == mask;
    }

    /// <summary>
    /// Checks if the set bits in <see cref="mask"/> are also set in <see cref="value"/>
    /// </summary>
    /// <param name="value">Value to check</param>
    /// <param name="mask">Set bits to check</param>
    /// <returns><c>true</c> if respective bits are set, else <c>false</c></returns>
    protected bool CheckBits(UInt16 value, UInt16 mask)
    {
      return CheckMask(value, mask, mask);
    }

    /// <summary>
    /// Modifies the bits indicated by <see cref="separatorMask"/> from <see cref="value"/> so that they are
    /// equal to <see cref="mask"/>
    /// </summary>
    /// <param name="value">Value to modify</param>
    /// <param name="separatorMask">Relevant Bits</param>
    /// <param name="mask">Bits to set</param>
    /// <returns>Modified <see cref="value"/></returns>
    protected UInt16 SetMask(UInt16 value, UInt16 separatorMask, UInt16 mask)
    {
      return (UInt16)((value & ~separatorMask) | mask);
    }

    /// <summary>
    /// Sets the bits indicated by <see cref="mask"/> in <see cref="value"/> to 1 or 0 depending on <see cref="boolean"/>
    /// </summary>
    /// <param name="value">Value to modify</param>
    /// <param name="mask">Bits to set</param>
    /// <param name="boolean">If <c>true, bits are set to 1, else to 0</c></param>
    /// <returns>>Modified <see cref="value"/></returns>
    protected UInt16 SetBits(UInt16 value, UInt16 mask, bool boolean)
    {
      return SetMask(value, mask, boolean ? mask : (UInt16)0);
    }

    #endregion
  }
}
