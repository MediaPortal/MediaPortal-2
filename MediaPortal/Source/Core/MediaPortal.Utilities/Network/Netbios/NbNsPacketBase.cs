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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Name Service packet
  /// </summary>
  /// <remarks>
  /// All classes representing Netbios Name packets are derived from this base class.
  /// When creating a packet to be sent, the respective derived class must be instantiated.
  /// When parsing a received packet, the <see cref="TryParse"/> method of this base class must be used; depending on
  /// the <see cref="PacketType"/>, the returned <see cref="NbNsPacketBase"/>  object can be casted to the correct
  /// type of the received packet.
  /// Currently only Netbios Node Status Requests and Netbios Node Status Responses are supported. All other packet types
  /// are not implemented so that <see cref="TryParse"/> will return false when other packets are parsed.
  /// This class can be implicitly converted to a byte array representing the respective Netbios Name Service packet.
  /// Details on the Netbios protocol can be found in http://tools.ietf.org/html/rfc1001 and http://tools.ietf.org/html/rfc1002
  /// </remarks>
  /// <example>
  /// NbNsPacketBase packet;
  /// if (NbNsPacketBase.TryParse(buffer, out packet))
  /// {
  ///   if (PacketType == NbNsPacketBase.PacketTypes.NodeStatusResponse)
  ///   {
  ///     var response = (NbNsNodeStatusResponse)packet;
  ///     ...
  ///   }
  ///   ...
  /// }
  /// </example>
  public abstract class NbNsPacketBase
  {
    #region Constants

    public const int MAX_DATAGRAM_LENGTH = 576;

    #endregion

    #region Public enums

    public enum PacketTypes
    {
      NameRegistrationRequest,
      NameOverwriteRequestAndDemand,
      NameRefreshRequest,
      PositiveNameRegistrationResponse,
      NegativeNameRegistrationResponse,
      EndNodeChallengeRegistrationResponse,
      NameConflictDemand,
      NameReleaseRequestAndDemand,
      PositiveNameReleaseResponse,
      NegativeNameReleaseResponse,
      NameQueryRequest,
      PositiveNameQueryResponse,
      NegativeNameQueryResponse,
      RedirectNameQueryResponse,
      WackResponse,
      NodeStatusRequest,
      NodeStatusResponse
    }

    #endregion

    #region Protected fields

    protected readonly PacketTypes PacketType;
    protected readonly List<NbPacketSegmentBase> PacketSegments = new List<NbPacketSegmentBase>();

    #endregion

    #region Constructors

    /// <summary>
    /// Called by derived classes to initialize the <see cref="NbNsHeader"/> and the <see cref="PacketType"/> of a packet
    /// </summary>
    /// <param name="header"><see cref="NbNsHeader"/> of the packet</param>
    /// <param name="type"><see cref="PacketType"/> of the packet</param>
    protected NbNsPacketBase(NbNsHeader header, PacketTypes type)
    {
      PacketType = type;

      // The first packet segment is always the header; Further packet segments depend on the PacketType and are
      // added in the appropriate order by the constructors of derived classes.
      PacketSegments.Add(header);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Type of the Netbios Name Service packet
    /// </summary>
    public PacketTypes Type { get { return PacketType; } }
    
    /// <summary>
    /// Header of the Netbios Name Service packet
    /// </summary>
    public NbNsHeader Header { get { return (NbNsHeader)PacketSegments[0]; } }

    /// <summary>
    /// Byte array representation of the Netbios Name Service packet
    /// </summary>
    public byte[] ByteArray
    {
      get
      {
        var result = new byte[Length];

        var index = 0;
        foreach (var segment in PacketSegments)
        {
          segment.ByteArray.CopyTo(result, index);
          index += segment.Length;
        }

        // ToDo: Replcae Netbios Labels with label pointers?
        // ToDo: Check maximum Length?

        return result;
      }
    }

    /// <summary>
    /// Number of bytes this packet takes in byte array format
    /// </summary>
    public int Length { get { return PacketSegments.Sum(segment => segment.Length); }}

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a Netbios Name Service packet from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNs packet</param>
    /// <param name="packet">Parsed NbNs packet if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    /// <remarks>
    /// This method is the entry point for parsing Netbios Name Service packets. It returns an object of a class
    /// derived from <see cref="NbNsPacketBase"/>, which can then be casted based on <see cref="PacketType"/> to
    /// the respective derived class.
    /// </remarks>
    public static bool TryParse(byte[] buffer, out NbNsPacketBase packet)
    {
      packet = null;
      if (buffer == null || buffer.Length < NbNsHeader.NETBIOS_HEADER_LENGTH)
        return false;

      NbNsHeader header;
      if (!NbNsHeader.TryParse(buffer, out header))
        return false;

      if (header.Opcode == NbNsHeader.OpcodeSpecifier.Query && header.IsResponse == false && header.IsRecursionDesired == false)
      {
        // Must be a Netbios Node Status Request
        NbNsNodeStatusRequest result;
        if (!NbNsNodeStatusRequest.TryParse(header, buffer, out result))
          return false;
        packet = result;
      }
      else if (header.Opcode == NbNsHeader.OpcodeSpecifier.Query && header.IsRecursionDesired == false)
      {
        // Must be a Netbios Node Status Response
        NbNsNodeStatusResponse result;
        if (!NbNsNodeStatusResponse.TryParse(header, buffer, out result))
          return false;
        packet = result;
      }

      // ToDo: Parse Further Netbios Name Service packets
      return packet != null;
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine(String.Format("{0}:", PacketType));
      foreach (var segment in PacketSegments)
        builder.Append(segment);
      return builder.ToString().TrimEnd('\r', '\n');
    }

    #endregion

    #region Operators

    /// <summary>
    /// Enables implicit conversion of a <see cref="NbNsPacketBase"/> into a byte array
    /// </summary>
    /// <param name="packet">Netbios Name Service Packet to convert into a byte array</param>
    /// <returns></returns>
    public static implicit operator byte[](NbNsPacketBase packet)
    {
      return packet.ByteArray;
    }

    #endregion
  }
}
