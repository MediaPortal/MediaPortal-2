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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Nameservice Header
  /// </summary>
  /// <remarks>
  /// A Netbios Nameservice Header has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         NAME_TRN_ID           | OPCODE  |   NM_FLAGS  | RCODE |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          QDCOUNT              |           ANCOUNT             |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          NSCOUNT              |           ARCOUNT             |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.1.1
  /// </remarks>
  public class NbNsHeader : NbPacketSegmentBase
  {
    #region Consts and enums

    public const int NETBIOS_HEADER_LENGTH = 12;

    private const int OFFSET_NAME_TRN_ID = 0;
    private const int OFFSET_OPCODE_NM_FLAGS_RCODE = 2;
    private const int OFFSET_QDCOUNT = 4;
    private const int OFFSET_ANCOUNT = 6;
    private const int OFFSET_NSCOUNT = 8;
    private const int OFFSET_ARCOUNT = 10;

    #region OPCODE enum and consts

    // The OPCODE field is defined as::
    //   0   1   2   3   4
    // +---+---+---+---+---+
    // | R |    OPCODE     |
    // +---+---+---+---+---+
    //
    // R: RESPONSE flag
    // OPCODE: Operation specifier

    public enum OpcodeSpecifier
    {
      Query = 0,
      Registration = 5,
      Release = 6,
      Wack = 7,
      Refresh = 8
    }

    private static readonly UInt16 MASK_OPCODE_SEPARATOR =    Convert.ToUInt16("0111100000000000", 2);
    private static readonly UInt16 MASK_OPCODE_QUERY =        Convert.ToUInt16("0000000000000000", 2);
    private static readonly UInt16 MASK_OPCODE_REGISTRATION = Convert.ToUInt16("0010100000000000", 2);
    private static readonly UInt16 MASK_OPCODE_RELEASE =      Convert.ToUInt16("0011000000000000", 2);
    private static readonly UInt16 MASK_OPCODE_WACK =         Convert.ToUInt16("0011100000000000", 2);
    private static readonly UInt16 MASK_OPCODE_REFRESH =      Convert.ToUInt16("0100000000000000", 2);

    private static readonly UInt16 MASK_R_SEPARATOR_BOOL =    Convert.ToUInt16("1000000000000000", 2);

    #endregion

    #region NM_FLAGS consts

    // The NM_FLAGS field is defined as:
    //   0   1   2   3   4   5   6
    // +---+---+---+---+---+---+---+
    // |AA |TC |RD |RA | 0 | 0 | B |
    // +---+---+---+---+---+---+---+
    //
    // B: Broadcast Flag
    // RA: Recursion Available Flag
    // RD: Recursion Desired Flag
    // TC: Truncation Flag
    // AA: Authoritative Answer Flag

    private static readonly UInt16 MASK_B_SEPARATOR_BOOL =  Convert.ToUInt16("0000000000010000", 2);
    private static readonly UInt16 MASK_RA_SEPARATOR_BOOL = Convert.ToUInt16("0000000010000000", 2);
    private static readonly UInt16 MASK_RD_SEPARATOR_BOOL = Convert.ToUInt16("0000000100000000", 2);
    private static readonly UInt16 MASK_TC_SEPARATOR_BOOL = Convert.ToUInt16("0000001000000000", 2);
    private static readonly UInt16 MASK_AA_SEPARATOR_BOOL = Convert.ToUInt16("0000010000000000", 2);

    #endregion

    #region RCODE enum and consts

    public enum RcodeSpecifier
    {
      Ok = 0x0,
      FmtErr = 0x1, // Format Error. Request was invalidly formatted.
      SvrErr = 0x2, // Server failure. Problem with NbNs, cannot process name.
      NamErr = 0x3, // Name Error. The name requested does not exist.
      ImpErr = 0x4, // Unsupported request error. Allowable only for challenging NBNS when gets an Update type registration request.
      RfsErr = 0x5, // Refused error. For policy reasons server will not register / release this name from this host.
      ActErr = 0x6, // Active error. Name is owned by another node.
      CftErr = 0x7, // Name in conflict error. A UNIQUE name is owned by more than one node.
    }

    private static readonly UInt16 MASK_RCODE_SEPARATOR = Convert.ToUInt16("0000000000001111", 2);
    private static readonly UInt16 MASK_RCODE_OK =        Convert.ToUInt16("0000000000000000", 2);
    private static readonly UInt16 MASK_RCODE_FMT_ERR =   Convert.ToUInt16("0000000000000001", 2);
    private static readonly UInt16 MASK_RCODE_SVR_ERR =   Convert.ToUInt16("0000000000000010", 2);
    private static readonly UInt16 MASK_RCODE_NAM_ERR =   Convert.ToUInt16("0000000000000011", 2);
    private static readonly UInt16 MASK_RCODE_IMP_ERR =   Convert.ToUInt16("0000000000000100", 2);
    private static readonly UInt16 MASK_RCODE_RFS_ERR =   Convert.ToUInt16("0000000000000101", 2);
    private static readonly UInt16 MASK_RCODE_ACT_ERR =   Convert.ToUInt16("0000000000000110", 2);
    private static readonly UInt16 MASK_RCODE_CFT_ERR =   Convert.ToUInt16("0000000000000111", 2);

    #endregion

    #endregion

    #region Private fields

    private readonly byte[] _buffer;
    private static int _nextTrnId;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsHeader"/> with default values.
    /// </summary>
    /// <remarks>
    /// <see cref="NameTrnId"/> is the next available Id; all other values are 0 / false.
    /// </remarks>
    public NbNsHeader()
    {
      _buffer = new byte[NETBIOS_HEADER_LENGTH];
      NameTrnId = Convert.ToUInt16(Interlocked.Increment(ref _nextTrnId) & 0x0000FFFF);
    }

    /// <summary>
    /// Creates a new instance of <see cref="NbNsHeader"/> based on the first <see cref="NETBIOS_HEADER_LENGTH"/> bytes of <see cref="buffer"/>.
    /// </summary>
    /// <param name="buffer">Array of bytes beginning with a <see cref="NbNsHeader"/></param>
    /// <remarks>
    /// This constructor is called only by <see cref="TryParse"/>.
    /// The caller must ensure that buffer is not null and constains at least <see cref="NETBIOS_HEADER_LENGTH"/> bytes.
    /// </remarks>
    private NbNsHeader(IEnumerable<byte> buffer)
    {
      _buffer = buffer.Take(NETBIOS_HEADER_LENGTH).ToArray();
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Transaction ID
    /// </summary>
    public UInt16 NameTrnId
    {
      get { return BufferToUInt16(_buffer, OFFSET_NAME_TRN_ID); }
      set { UInt16ToBuffer(value, _buffer, OFFSET_NAME_TRN_ID); }
    }

    /// <summary>
    /// <c>true</c> if <see cref="NbNsHeader"/> belongs to a Response Packet; else <c>false</c>
    /// </summary>
    public bool IsResponse
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_R_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_R_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// Opcode of the Packet this <see cref="NbNsHeader"/> belongs to
    /// </summary>
    public OpcodeSpecifier Opcode
    {
      get
      {
        var opcode = BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
        if (CheckMask(opcode, MASK_OPCODE_SEPARATOR, MASK_OPCODE_QUERY))
          return OpcodeSpecifier.Query;
        if (CheckMask(opcode, MASK_OPCODE_SEPARATOR, MASK_OPCODE_REGISTRATION))
          return OpcodeSpecifier.Registration;
        if (CheckMask(opcode, MASK_OPCODE_SEPARATOR, MASK_OPCODE_RELEASE))
          return OpcodeSpecifier.Release;
        if (CheckMask(opcode, MASK_OPCODE_SEPARATOR, MASK_OPCODE_WACK))
          return OpcodeSpecifier.Wack;
        if (CheckMask(opcode, MASK_OPCODE_SEPARATOR, MASK_OPCODE_REFRESH))
          return OpcodeSpecifier.Refresh;
        throw new InvalidDataException("Opcode");
      }

      set
      {
        switch(value)
        {
          case OpcodeSpecifier.Query:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_OPCODE_QUERY), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case OpcodeSpecifier.Registration:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_OPCODE_REGISTRATION), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case OpcodeSpecifier.Release:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_OPCODE_RELEASE), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case OpcodeSpecifier.Wack:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_OPCODE_WACK), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case OpcodeSpecifier.Refresh:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_OPCODE_REFRESH), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
        }
      }
    }

    /// <summary>
    /// <c>true</c> if the Packet this <see cref="NbNsHeader"/> belongs to is broadcast or multicast; else <c>false</c>
    /// </summary>
    public bool IsBroadcast
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_B_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_B_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// <c>true</c> if the Netbios Name Server supports recursive query, registration, and release; else <c>false</c>
    /// </summary>
    /// <remarks>
    /// <c>true</c> only valid in responses from a Netbios Name Server; must be <c>false</c> in all other responses.
    /// </remarks>
    public bool IsRecursionAvailable
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_RA_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_RA_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// <c>true</c> if the Netbios Name Server shall iterate on the query, registration, or release; else <c>false</c>
    /// </summary>
    /// <remarks>
    /// May only be <c>true</c> on a request to a Netbios Name Server.
    /// </remarks>
    public bool IsRecursionDesired
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_RD_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_RD_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// <c>true</c> if this message was truncated because the datagram carrying it would be greater than 576 bytes in length; else <c>false</c>
    /// </summary>
    public bool IsTruncated
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_TC_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_TC_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// <c>true</c> if responding node is an authority for the domain name; else <c>false</c>
    /// </summary>
    public bool IsAuthoritativeAnswer
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_AA_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_AA_SEPARATOR_BOOL, value), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE); }
    }

    /// <summary>
    /// Result code contained in a Response in answer to a Request
    /// </summary>
    public RcodeSpecifier Rcode
    {
      get
      {
        var rcode = BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_OK))
          return RcodeSpecifier.Ok;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_FMT_ERR))
          return RcodeSpecifier.FmtErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_SVR_ERR))
          return RcodeSpecifier.SvrErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_NAM_ERR))
          return RcodeSpecifier.NamErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_IMP_ERR))
          return RcodeSpecifier.ImpErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_RFS_ERR))
          return RcodeSpecifier.RfsErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_ACT_ERR))
          return RcodeSpecifier.ActErr;
        if (CheckMask(rcode, MASK_RCODE_SEPARATOR, MASK_RCODE_CFT_ERR))
          return RcodeSpecifier.CftErr;
        throw new InvalidDataException("Rcode");
      }

      set
      {
        switch (value)
        {
          case RcodeSpecifier.Ok:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_OK), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.FmtErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_FMT_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.SvrErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_SVR_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.NamErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_NAM_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.ImpErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_IMP_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.RfsErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_RFS_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.ActErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_ACT_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
          case RcodeSpecifier.CftErr:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_OPCODE_NM_FLAGS_RCODE), MASK_OPCODE_SEPARATOR, MASK_RCODE_CFT_ERR), _buffer, OFFSET_OPCODE_NM_FLAGS_RCODE);
            break;
        }
      }
    }

    /// <summary>
    /// Number of NbNsQuestionEntries in the Packet
    /// </summary>
    public UInt16 QdCount
    {
      get { return BufferToUInt16(_buffer, OFFSET_QDCOUNT); }
      set { UInt16ToBuffer(value, _buffer, OFFSET_QDCOUNT); }
    }

    /// <summary>
    /// Number of NbNsResourceRecords in the answer section of a Packet
    /// </summary>
    public UInt16 AnCount
    {
      get { return BufferToUInt16(_buffer, OFFSET_ANCOUNT); }
      set { UInt16ToBuffer(value, _buffer, OFFSET_ANCOUNT); }
    }

    /// <summary>
    /// Number of NbNsResourceRecords in the authority section of a Packet
    /// </summary>
    public UInt16 NsCount
    {
      get { return BufferToUInt16(_buffer, OFFSET_NSCOUNT); }
      set { UInt16ToBuffer(value, _buffer, OFFSET_NSCOUNT); }
    }

    /// <summary>
    /// Number of NbNsResourceRecords in the additional records section of a Packet
    /// </summary>
    public UInt16 ArCount
    {
      get { return BufferToUInt16(_buffer, OFFSET_ARCOUNT); }
      set { UInt16ToBuffer(value, _buffer, OFFSET_ARCOUNT); }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsHeader"/> from a buffer of bytes, starting at the first byte
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsHeader</param>
    /// <param name="header">Parsed NbNsHeader if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, out NbNsHeader header)
    {
      header = null;
      if (buffer == null || buffer.Length < NETBIOS_HEADER_LENGTH)
        return false;

      header = new NbNsHeader(buffer);
      return true;
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      get { return NETBIOS_HEADER_LENGTH; }
    }

    public override byte[] ByteArray
    {
      get { return _buffer; }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("  NbNsHeader:");
      builder.AppendLine(String.Format("    NameTrnId: {0}", NameTrnId));
      builder.AppendLine(String.Format("    IsRespose: {0}", IsResponse));
      builder.AppendLine(String.Format("    OpCode: {0}", Opcode));
      builder.AppendLine("    NmFlags:");
      builder.AppendLine(String.Format("      IsBroadcast: {0}", IsBroadcast));
      builder.AppendLine(String.Format("      IsRecursionAvailable: {0}", IsRecursionAvailable));
      builder.AppendLine(String.Format("      IsRecursionDesired: {0}", IsRecursionDesired));
      builder.AppendLine(String.Format("      IsTruncated: {0}", IsTruncated));
      builder.AppendLine(String.Format("      IsAuthoritativeAnswer: {0}", IsAuthoritativeAnswer));
      builder.AppendLine(String.Format("    Rcode: {0}", Rcode));
      builder.AppendLine(String.Format("    QdCount: {0}", QdCount));
      builder.AppendLine(String.Format("    AnCount: {0}", AnCount));
      builder.AppendLine(String.Format("    NsCount: {0}", NsCount));
      builder.AppendLine(String.Format("    ArCount: {0}", ArCount));
      return builder.ToString();
    }

    #endregion
  }
}
