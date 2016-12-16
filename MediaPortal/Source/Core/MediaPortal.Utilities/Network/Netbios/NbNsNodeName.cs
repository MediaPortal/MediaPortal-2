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
using System.IO;
using System.Linq;
using System.Text;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios NodeName Entry
  /// </summary>
  /// <remarks>
  /// A NetbiosNodeName Entry has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// +---                                                         ---+
  /// |                                                               |
  /// +---                    NETBIOS FORMAT NAME                  ---+
  /// |                                                               |
  /// +---                                                         ---+
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         NAME_FLAGS            |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.18
  /// and here: http://www.ubiqx.org/cifs/NetBIOS.html (section 1.4.3.5.1) 
  /// The Netbios Format Name is the 16 bytes unencoded Netbios Name including suffix.
  /// This class is used only in the RDATA field of the NbNsNodeStatusResponseResourceRecord
  /// </remarks>
  public class NbNsNodeName : NbPacketSegmentBase
  {
    #region Consts and enums

    public const int NODE_NAME_LENGTH = 18;
    private const int NAME_LENGTH = 15;
    private const int OFFSET_NAME = 0;
    private const int OFFSET_SUFFIX = 15;
    private const int OFFSET_NAME_FLAGS = 16;

    // The NAME_FLAGS field is defined as:
    //                                           1   1   1   1   1   1
    //   0   1   2   3   4   5   6   7   8   9   0   1   2   3   4   5
    // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    // | G |  ONT  |DRG|CNF|ACT|PRM|          RESERVED                 |
    // +---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+
    //
    // G: Group Name Flag
    // ONT: Owner Node Type
    //   00: B-Node
    //   01: P-Node
    //   10: M-Node
    //   11: Reserved for future use
    // DRG: Deregister Flag
    // CNF: Conflict Flag
    // ACT: Active Name Flag.  All entries have this flagset to one (1).
    // PRM: Permanent Name Flag

    private static readonly UInt16 MASK_G_SEPARATOR_BOOL =   Convert.ToUInt16("1000000000000000", 2);
    private static readonly UInt16 MASK_DRG_SEPARATOR_BOOL = Convert.ToUInt16("0001000000000000", 2);
    private static readonly UInt16 MASK_CNF_SEPARATOR_BOOL = Convert.ToUInt16("0000100000000000", 2);
    private static readonly UInt16 MASK_ACT_SEPARATOR_BOOL = Convert.ToUInt16("0000010000000000", 2);
    private static readonly UInt16 MASK_PRM_SEPARATOR_BOOL = Convert.ToUInt16("0000001000000000", 2);

    public enum OwnerNodeTypes
    {
      BNode = 0,
      PNode = 1,
      MNode = 2
    }
    private static readonly UInt16 MASK_ONT_SEPARATOR = Convert.ToUInt16("0110000000000000", 2);
    private static readonly UInt16 MASK_ONT_B_NODE =    Convert.ToUInt16("0000000000000000", 2);
    private static readonly UInt16 MASK_ONT_P_NODE =    Convert.ToUInt16("0010000000000000", 2);
    private static readonly UInt16 MASK_ONT_M_NODE =    Convert.ToUInt16("0100000000000000", 2);

    #endregion
    
    #region Private fields

    private readonly byte[] _buffer = new byte[NODE_NAME_LENGTH];

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeName"/> with standard values
    /// </summary>
    /// <param name="name">Unencoded Name of the <see cref="NbNsNodeName"/></param>
    /// <param name="suffix">Netbios Suffix of the <see cref="NbNsNodeName"/></param>
    /// <remarks>
    /// <see cref="IsActive"/> is set to true, all other values to 0 / false.
    /// </remarks>
    public NbNsNodeName(String name, NbName.KnownNetbiosSuffixes suffix)
    {
      Name = name;
      Suffix = suffix;
      IsActive = true;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeName"/> based on the provided byte array
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsNodeName</param>
    /// <remarks>
    /// This constructor is only called from <see cref="TryParse"/>.
    /// The called must make sure that buffer is not null and buffer contains exactly <see cref="NODE_NAME_LENGTH"/> bytes.
    /// </remarks>
    private NbNsNodeName(byte[] buffer)
    {
      if (buffer == null || buffer.Length != NODE_NAME_LENGTH)
        throw new ArgumentException("buffer");
      buffer.CopyTo(_buffer, 0);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Unencoded Name of the <see cref="NbNsNodeName"/>
    /// </summary>
    public String Name
    {
      get { return NbName.ENCODING.GetString(_buffer, OFFSET_NAME, NAME_LENGTH).Trim(); }

      set
      {
        if (value.Length > NAME_LENGTH)
          throw new ArgumentException("Name");
        NbName.ENCODING.GetBytes(value.PadRight(NAME_LENGTH), 0, NAME_LENGTH, _buffer, OFFSET_NAME);
      }
    }

    /// <summary>
    /// Netbios Suffix of the <see cref="NbNsNodeName"/>
    /// </summary>
    public NbName.KnownNetbiosSuffixes Suffix
    {
      get { return (NbName.KnownNetbiosSuffixes)_buffer[OFFSET_SUFFIX]; }

      set { _buffer[OFFSET_SUFFIX] = (byte)value; }
    }

    /// <summary>
    /// <c>true</c> if <see cref="NbNsNodeName"/> is a Group Name
    /// <c>false</c> if <see cref="NbNsNodeName"/> is a Unique Name
    /// </summary>
    public bool IsGroup
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_G_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_G_SEPARATOR_BOOL, value), _buffer, MASK_G_SEPARATOR_BOOL); }
    }

    /// <summary>
    /// NodeType of the owner of this <see cref="NbNsNodeName"/>
    /// </summary>
    public OwnerNodeTypes OwnerNodeType
    {
      get
      {
        var ont = BufferToUInt16(_buffer, OFFSET_NAME_FLAGS);
        if (CheckMask(ont, MASK_ONT_SEPARATOR, MASK_ONT_B_NODE))
          return OwnerNodeTypes.BNode;
        if (CheckMask(ont, MASK_ONT_SEPARATOR, MASK_ONT_P_NODE))
          return OwnerNodeTypes.PNode;
        if (CheckMask(ont, MASK_ONT_SEPARATOR, MASK_ONT_M_NODE))
          return OwnerNodeTypes.MNode;
        throw new InvalidDataException("OwnerNodeType");
      }

      set
      {
        switch (value)
        {
          case OwnerNodeTypes.BNode:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_ONT_SEPARATOR, MASK_ONT_B_NODE), _buffer, OFFSET_NAME_FLAGS);
            break;
          case OwnerNodeTypes.PNode:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_ONT_SEPARATOR, MASK_ONT_P_NODE), _buffer, OFFSET_NAME_FLAGS);
            break;
          case OwnerNodeTypes.MNode:
            UInt16ToBuffer(SetMask(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_ONT_SEPARATOR, MASK_ONT_M_NODE), _buffer, OFFSET_NAME_FLAGS);
            break;
        }
      }
    }

    /// <summary>
    /// <c>true</c> if this name is in the process of being deleted; else <c>false</c>
    /// </summary>
    public bool IsDeregistered
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_DRG_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_DRG_SEPARATOR_BOOL, value), _buffer, OFFSET_NAME_FLAGS); }
    }

    /// <summary>
    /// <c>true</c> if the name on this node is in conflict; else <c>false</c>
    /// </summary>
    public bool IsConflicted
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_CNF_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_CNF_SEPARATOR_BOOL, value), _buffer, OFFSET_NAME_FLAGS); }
    }

    /// <summary>
    /// All entries have this flag set to <c>true</c>
    /// </summary>
    public bool IsActive
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_ACT_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_ACT_SEPARATOR_BOOL, value), _buffer, OFFSET_NAME_FLAGS); }
    }

    /// <summary>
    /// <c>true</c> if this entry is for the permanent node name
    /// </summary>
    public bool IsPermanent
    {
      get { return CheckBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_PRM_SEPARATOR_BOOL); }
      set { UInt16ToBuffer(SetBits(BufferToUInt16(_buffer, OFFSET_NAME_FLAGS), MASK_PRM_SEPARATOR_BOOL, value), _buffer, OFFSET_NAME_FLAGS); }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsNodeName"/> from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsNodeName</param>
    /// <param name="offset">Zero based offset in the buffer where the NbNsNodeName starts</param>
    /// <param name="nodeName">Parsed NbNsNodeName if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, int offset, out NbNsNodeName nodeName)
    {
      nodeName = null;
      if (buffer == null)
        return false;
      if (offset < 0 || offset > buffer.Length - NODE_NAME_LENGTH)
        return false;
      nodeName = new NbNsNodeName(buffer.Skip(offset).Take(NODE_NAME_LENGTH).ToArray());
      return true;
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      get { return NODE_NAME_LENGTH; }
    }

    public override byte[] ByteArray
    {
      get { return _buffer; }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine(String.Format("      NbNsNodeName: '{0}'", Name));
      builder.AppendLine(String.Format("        IsGroup: {0}", IsGroup));

      String suffixName;
      if (IsGroup)
      {
        switch (Suffix)
        {
          case NbName.KnownNetbiosSuffixes.DomainName:
            suffixName = "DomainName";
            break;
          case NbName.KnownNetbiosSuffixes.MasterBrowsers:
            suffixName = "MasterBrowsers";
            break;
          case NbName.KnownNetbiosSuffixes.DomainControllers:
            suffixName = "DomainControllers";
            break;
          case NbName.KnownNetbiosSuffixes.BrowserServiceElections:
            suffixName = "BrowserServiceElections";
            break;
          default:
            suffixName = String.Format("{0:X}", (byte)Suffix);
            break;
        }
      }
      else
      {
        switch (Suffix)
        {
          case NbName.KnownNetbiosSuffixes.WorkstationService:
            suffixName = "WorkstationService";
            break;
          case NbName.KnownNetbiosSuffixes.MessengerService:
            suffixName = "MessengerService";
            break;
          case NbName.KnownNetbiosSuffixes.FileServerService:
            suffixName = "FileServerService";
            break;
          case NbName.KnownNetbiosSuffixes.DomainMasterBrowser:
            suffixName = "DomainMasterBrowser";
            break;
          case NbName.KnownNetbiosSuffixes.MasterBrowser:
            suffixName = "MasterBrowser";
            break;
          default:
            suffixName = String.Format("{0:X}", (byte)Suffix);
            break;
        }
      }
      builder.AppendLine(String.Format("        Suffix: {0}", suffixName));
      builder.AppendLine(String.Format("        OwnerNodeType: {0}", OwnerNodeType));
      builder.AppendLine("        NameFlags:");
      builder.AppendLine(String.Format("          IsDeregistered: {0}", IsDeregistered));
      builder.AppendLine(String.Format("          IsConflicted: {0}", IsConflicted));
      builder.AppendLine(String.Format("          IsActive: {0}", IsActive));
      builder.AppendLine(String.Format("          IsPermanent: {0}", IsPermanent));

      return builder.ToString();
    }

    #endregion
  }
}
