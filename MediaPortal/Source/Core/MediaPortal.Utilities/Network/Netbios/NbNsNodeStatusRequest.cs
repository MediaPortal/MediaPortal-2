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

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Nameservice Node Status Request Packet
  /// </summary>
  /// <remarks>
  /// A Netbios Nameservice Node Status Request Packet has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// NbNsHeader:
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         NAME_TRN_ID           |0|  0x0  |0|0|0|0|0 0|B|  0x0  |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          0x0001               |           0x0000              |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |          0x0000               |           0x0000              |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// NbNsQuestionEntry:
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// /                         QUESTION_NAME                         /
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         NBSTAT (0x0021)       |        IN (0x0001)            |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.17
  /// </remarks>
  public class NbNsNodeStatusRequest : NbNsPacketBase
  {
    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeStatusRequest"/> based on the given <see cref="NbNsHeader"/> and <see cref="NbNsQuestionEntry"/>
    /// </summary>
    /// <param name="header"><see cref="NbNsHeader"/> to use for this <see cref="NbNsNodeStatusRequest"/></param>
    /// <param name="question"><see cref="NbNsQuestionEntry"/> to use for this <see cref="NbNsNodeStatusRequest"/></param>
    public NbNsNodeStatusRequest(NbNsHeader header, NbNsQuestionEntry question)
      : base(header, PacketTypes.NodeStatusRequest)
    {
      PacketSegments.Add(question);
    }

    /// <summary>
    /// Creates a new instance of <see cref="NbNsNodeStatusRequest"/> based on the given <see cref="NbName"/> and <see cref="isBroadcast"/> value
    /// </summary>
    /// <param name="name"><see cref="NbName"/> to include in the <see cref="NbNsQuestionEntry"/> of this <see cref="NbNsNodeStatusRequest"/></param>
    /// <param name="isBroadcast">Indicates whether this <see cref="NbNsNodeStatusRequest"/> is a broadcase / multicast Package</param>
    public NbNsNodeStatusRequest(NbName name, bool isBroadcast)
      : base(new NbNsHeader(), PacketTypes.NodeStatusRequest)
    {
      Header.QdCount = 1;
      Header.IsBroadcast = isBroadcast;
      PacketSegments.Add(new NbNsQuestionEntry(name, NbNsQuestionEntry.QuestionTypeSpecifier.NbStat));
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// <see cref="NbNsQuestionEntry"/> of this <see cref="NbNsNodeStatusRequest"/>
    /// </summary>
    public NbNsQuestionEntry QuestionEntry { get { return (NbNsQuestionEntry)PacketSegments[1]; } }

    /// <summary>
    /// Convenience property returning a <see cref="NbNsNodeStatusRequest"/> for a wildcard Name and as unicast Package
    /// </summary>
    public static NbNsNodeStatusRequest WildCardNodeStatusRequest { get { return new NbNsNodeStatusRequest(NbName.WildCardName, false); } }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsNodeStatusRequest"/> from a buffer of bytes starting after <see cref="NbNsHeader.NETBIOS_HEADER_LENGTH"/> bytes
    /// </summary>
    /// <param name="header"><see cref="NbNsHeader"/> already parsed from the beginning of <see cref="buffer"/></param>
    /// <param name="buffer">Byte array containing the NbNsNodeStatusRequest</param>
    /// <param name="result">Parsed NbNsNodeStatusRequest if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    /// <remarks>
    /// This method is only called from <see cref="NbNsPacketBase.TryParse"/>.
    /// </remarks>
    internal static bool TryParse(NbNsHeader header, byte[] buffer, out NbNsNodeStatusRequest result)
    {
      result = null;

      NbNsQuestionEntry questionEntry;
      if (!NbNsQuestionEntry.TryParse(buffer, NbNsHeader.NETBIOS_HEADER_LENGTH, out questionEntry))
        return false;

      result = new NbNsNodeStatusRequest(header, questionEntry);
      return true;
    }

    #endregion
  }
}
