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
using System.Text;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Resource Record in a Netbios Node Status Response
  /// </summary>
  /// <remarks>
  /// A NbNsNodeStatusResponseResourceRecord has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// /                            RR_NAME                            /
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |        NBSTAT (0x0021)        |         IN (0x0001)           |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                          0x00000000                           |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          RDLENGTH             |   NUM_NAMES   |               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+               +
  /// |                                                               |
  /// +                                                               +
  /// /                         NODE_NAME ARRAY                       /
  /// +                                                               +
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// +                                                               +
  /// /                           STATISTICS                          /
  /// +                                                               +
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.18
  /// </remarks>
  public class NbNsNodeStatusResponseResourceRecord : NbNsResourceRecordBase
  {
    #region Private fields

    private List<NbNsNodeName> _nodeNames;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeStatusResponseResourceRecord"/> with the given <see cref="nodeNames"/> and <see cref="NbNsStatistics"/>
    /// </summary>
    /// <param name="rrName"><see cref="NbName"/> for this <see cref="NbNsNodeStatusResponseResourceRecord"/></param>
    /// <param name="nodeNames">List of <see cref="NbNsNodeName"/>s for this <see cref="NbNsNodeStatusResponseResourceRecord"/></param>
    /// <param name="statistics"><see cref="NbNsStatistics"/> for this <see cref="NbNsNodeStatusResponseResourceRecord"/></param>
    public NbNsNodeStatusResponseResourceRecord(NbName rrName, IEnumerable<NbNsNodeName> nodeNames, NbNsStatistics statistics)
      : base(RrTypeValue.NbStat, RrClassValue.In, 0)
    {
      RrName = rrName;
      NodeNames = new List<NbNsNodeName>(nodeNames);
      Statistics = statistics;
    }

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeStatusResponseResourceRecord"/> with default values
    /// </summary>
    /// <remarks>
    /// This constructor is only called from <see cref="TryParse"/>.
    /// <see cref="NbNsResourceRecordBase.RrName"/>, <see cref="NodeNames"/> and <see cref="Statistics"/> are null;
    /// the called must make sure that these properties are set.
    /// </remarks>
    private NbNsNodeStatusResponseResourceRecord()
      : base(RrTypeValue.NbStat, RrClassValue.In, 0)
    {
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// List of <see cref="NbNsNodeName"/>s owned by the responding node
    /// </summary>
    public List<NbNsNodeName> NodeNames
    {
      get { return new List<NbNsNodeName>(_nodeNames); }
      set
      {
        if (value == null)
          throw new ArgumentNullException("value");
        _nodeNames = value;
        RdLength = (ushort)(1 + _nodeNames.Count * NbNsNodeName.NODE_NAME_LENGTH + NbNsStatistics.STATISTICS_LENGTH);
      }
    }

    /// <summary>
    /// Number of <see cref="NbNsNodeName"/>s in this Packet
    /// </summary>
    public byte NumNames { get { return (byte)_nodeNames.Count; } }

    /// <summary>
    /// Statistics of the responding node
    /// </summary>
    public NbNsStatistics Statistics { get; set; }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsNodeStatusResponseResourceRecord"/> from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsNodeStatusResponseResourceRecord</param>
    /// <param name="offset">Zero based offset in the buffer where the NbNsNodeStatusResponseResourceRecord starts</param>
    /// <param name="resourceRecord">Parsed NbNsNodeStatusResponseResourceRecord if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, int offset, out NbNsNodeStatusResponseResourceRecord resourceRecord)
    {
      resourceRecord = null;

      var result = new NbNsNodeStatusResponseResourceRecord();
      if (!result.TryParse(buffer, offset))
        return false;

      if (result.RrType != RrTypeValue.NbStat || result.RrClass != RrClassValue.In)
        return false;

     offset += result.RrName.Length + 2 + 2 + 4 + 2;
     var numNames = buffer[offset];
     offset++;

     if (buffer.Length <= offset + numNames * NbNsNodeName.NODE_NAME_LENGTH + NbNsStatistics.STATISTICS_LENGTH)
        return false;

      var nodeNames = new List<NbNsNodeName>();
      for (byte i = 0; i < numNames; i++)
      {
        NbNsNodeName nodeName;
        if (!NbNsNodeName.TryParse(buffer, offset, out nodeName))
          return false;
        nodeNames.Add(nodeName);
        offset += NbNsNodeName.NODE_NAME_LENGTH;
      }
      result.NodeNames = nodeNames;

      NbNsStatistics statistics;
      if (!NbNsStatistics.TryParse(buffer, offset, out statistics))
        return false;
      result.Statistics = statistics;

      resourceRecord = result;
      return true;
    }

    #endregion

    #region Base overrides

    public override byte[] ByteArray
    {
      get
      {
        var result = new byte[Length];
        var offset = 0;

        RrName.ByteArray.CopyTo(result, offset);
        offset += RrName.Length;

        UInt16ToBuffer((UInt16)RrType, result, offset);
        offset += 2;

        UInt16ToBuffer((UInt16)RrClass, result, offset);
        offset += 2;

        UInt32ToBuffer(Ttl, result, offset);
        offset += 4;

        UInt16ToBuffer(RdLength, result, offset);
        offset += 2;

        result[offset] = (byte)_nodeNames.Count;
        offset++;

        foreach (var nodeName in _nodeNames)
        {
          nodeName.ByteArray.CopyTo(result, offset);
          offset += NbNsNodeName.NODE_NAME_LENGTH;
        }

        Statistics.ByteArray.CopyTo(result, offset);

        return result;
      }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.Append(base.ToString());
      builder.AppendLine(String.Format("    NumNames: {0}", NumNames));
      foreach (var nodeName in NodeNames)
        builder.Append(nodeName);
      builder.Append(Statistics);
      return builder.ToString();
    }

    #endregion
  }
}
