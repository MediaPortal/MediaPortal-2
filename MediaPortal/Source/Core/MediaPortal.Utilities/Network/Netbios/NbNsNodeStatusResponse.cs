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

using System.Linq;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Nameservice Node Status Response Packet
  /// </summary>
  /// <remarks>
  /// A Netbios Nameservice Node Status Response Packet has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// NbNsHeader:
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         NAME_TRN_ID           |1|  0x0  |1|0|0|0|0 0|0|  0x0  |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          0x0000               |           0x0001              |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          0x0000               |           0x0000              |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// NbNsNodeStatusResponseResourceRecord:
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
  public class NbNsNodeStatusResponse : NbNsPacketBase
  {
    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeStatusResponse"/> basedon the given <see cref="NbNsHeader"/> and <see cref="NbNsNodeStatusResponseResourceRecord"/>
    /// </summary>
    /// <param name="header"><see cref="NbNsHeader"/> to use for this <see cref="NbNsNodeStatusResponse"/></param>
    /// <param name="resourceRecord"><see cref="NbNsNodeStatusResponseResourceRecord"/> to use for this <see cref="NbNsNodeStatusResponse"/></param>
    public NbNsNodeStatusResponse(NbNsHeader header, NbNsNodeStatusResponseResourceRecord resourceRecord)
      : base(header, PacketTypes.NodeStatusResponse)
    {
      PacketSegments.Add(resourceRecord);
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// <see cref="NbNsNodeStatusResponseResourceRecord"/> of this <see cref="NbNsNodeStatusResponse"/>
    /// </summary>
    public NbNsNodeStatusResponseResourceRecord ResourceRecord { get { return (NbNsNodeStatusResponseResourceRecord)PacketSegments[1]; } }

    /// <summary>
    /// Convenience property returning the Netbios computer name of the responding computer
    /// </summary>
    /// <remarks>
    /// The Netbios computer name of the responding computer is determined as the returned node name
    /// - that is not a group name and
    /// - having a Netbios Suffix of 0x00 (i.e. NbName.KnownNetbiosSuffixes.WorkstationService)
    /// This type of node name should always be contained in the response. If this should not be the case,
    /// this method returns null.
    /// </remarks>
    public string WorkstationName
    {
      get
      {
        var workstationNodeName = ResourceRecord.NodeNames.FirstOrDefault(nodeName => !nodeName.IsGroup && nodeName.Suffix == NbName.KnownNetbiosSuffixes.WorkstationService);
        return (workstationNodeName == null) ? null : workstationNodeName.Name;
      }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsNodeStatusResponse"/> from a buffer of bytes starting after <see cref="NbNsHeader.NETBIOS_HEADER_LENGTH"/> bytes
    /// </summary>
    /// <param name="header"><see cref="NbNsHeader"/> already parsed from the beginning of <see cref="buffer"/></param>
    /// <param name="buffer">Byte array containing the NbNsNodeStatusResponse</param>
    /// <param name="result">Parsed NbNsNodeStatusResponse if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    /// <remarks>
    /// This method is only called from <see cref="NbNsPacketBase.TryParse"/>.
    /// </remarks>
    internal static bool TryParse(NbNsHeader header, byte[] buffer, out NbNsNodeStatusResponse result)
    {
      result = null;

      NbNsNodeStatusResponseResourceRecord rr;
      if (!NbNsNodeStatusResponseResourceRecord.TryParse(buffer, NbNsHeader.NETBIOS_HEADER_LENGTH, out rr))
        return false;

      result = new NbNsNodeStatusResponse(header, rr);
      return true;
    }

    #endregion
  }
}
