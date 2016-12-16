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

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Resource Record
  /// </summary>
  /// <remarks>
  /// A Netbios Resource Record has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// /                            RR_NAME                            /
  /// /                                                               /
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |           RR_TYPE             |          RR_CLASS             |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                              TTL                              |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |           RDLENGTH            |                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                               |
  /// /                                                               /
  /// /                             RDATA                             /
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.1.3
  /// The RDATA field varies depending on the Packet, in which this ResourceRecord is contained.
  /// Therefore this class is abstract and the concrete Resource Records of a specific packet
  /// type are dericed from this class.
  /// </remarks>
  public abstract class NbNsResourceRecordBase : NbPacketSegmentBase
  {
    #region Consts and enums

    // Offsets as of after RR_NAME
    private const int OFFSET_RR_TYPE = 0;
    private const int OFFSET_RR_CLASS = 2;
    private const int OFFSET_TTL = 4;
    private const int OFFSET_RD_LENGTH = 8;

    public enum RrTypeValue : ushort
    {
      A = 0x0001, // IP address Resource Record
      Ns = 0x0002, // Name Server Resource Record
      Null = 0x000A, // NULL Resource Record
      Nb = 0x0020, // NetBIOS general Name Service Resource Record
      NbStat = 0x0021 // NetBIOS NODE STATUS Resource Record
    }
    
    public enum RrClassValue : ushort
    {
      In = 0x0001
    }

    #endregion

    #region Private fields

    private NbName _rrName;

    #endregion

    #region Constructor

    /// <summary>
    /// Protected constructor only called from derived classes
    /// </summary>
    /// <param name="rrType"><see cref="RrTypeValue"/> for this ResourceRecord</param>
    /// <param name="rrClass"><see cref="RrClassValue"/> for this Resource Record (as per specs always <see cref="RrClassValue.In"/></param>
    /// <param name="ttl">"Time to live" for this ResourceRecord</param>
    protected NbNsResourceRecordBase(RrTypeValue rrType, RrClassValue rrClass, UInt32 ttl)
    {
      RrType = rrType;
      RrClass = rrClass;
      Ttl = ttl;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Resource Record Name
    /// </summary>
    public NbName RrName
    {
      get { return _rrName; }
      set { _rrName = value; }
    }

    /// <summary>
    /// Type of the Resource Record
    /// </summary>
    public RrTypeValue RrType { get; set; }
    
    /// <summary>
    /// Class of the Resource Record
    /// </summary>
    /// <remarks>
    /// As per specs this is always <see cref="RrClassValue.In"/>
    /// </remarks>
    public RrClassValue RrClass { get; set; }
    
    /// <summary>
    /// "Time to live" of the ResourceRecord
    /// </summary>
    public UInt32 Ttl { get; set; }
    
    /// <summary>
    /// Length of the RDATA field in bytes
    /// </summary>
    public UInt16 RdLength { get; set; }

    #endregion

    #region Protected methods

    /// <summary>
    /// Tries to parse all the fields of a ResourceRecord except for the RDATA field
    /// </summary>
    /// <param name="buffer">Byte array containing the Resource Record</param>
    /// <param name="offset">Zero based offset in the buffer where the Resource Record starts</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    /// <remarks>
    /// This method is called from derived classes in their TryParse method to parse the standard fields of
    /// every Resource Record. Only the RDATA field is different for each Resource Record and is therefore
    /// parsed by the specific TryParse methods of derived classes.
    /// </remarks>
    protected bool TryParse(byte[] buffer, int offset)
    {
      if (!NbName.TryParse(buffer, offset, out _rrName))
        return false;
      var rrNameLength = _rrName.Length;

      if (buffer.Length < offset + rrNameLength + 2 + 2 + 4 + 2)
        return false;

      RrType = (RrTypeValue)BufferToUInt16(buffer, offset + rrNameLength + OFFSET_RR_TYPE);
      RrClass = (RrClassValue)BufferToUInt16(buffer, offset + rrNameLength + OFFSET_RR_CLASS);
      Ttl = BufferToUInt32(buffer, offset + rrNameLength + OFFSET_TTL);
      RdLength = BufferToUInt16(buffer, offset + rrNameLength + OFFSET_RD_LENGTH);

      return buffer.Length >= offset + rrNameLength + 2 + 2 + 4 + 2 + RdLength;
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      get { return RrName.Length + 2 + 2 + 4 + 2 + RdLength; }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("  NbNsNodeStatusResponseResourceRecord:");
      builder.Append(RrName);
      builder.AppendLine(String.Format("    RrType: {0}", RrType));
      builder.AppendLine(String.Format("    RrClass: {0}", RrClass));
      builder.AppendLine(String.Format("    Ttl: {0}", Ttl));
      builder.AppendLine(String.Format("    RdLength: {0}", RdLength));
      return builder.ToString();
    }

    #endregion
  }
}
