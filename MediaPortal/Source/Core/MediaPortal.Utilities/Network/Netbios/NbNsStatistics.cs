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
using System.Text;

namespace MediaPortal.Utilities.Network.Netbios
{
  /// <summary>
  /// Represents a Netbios Statistics Field
  /// </summary>
  /// <remarks>
  /// A Netbios Statistics Field has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |               UNIT_ID (Unique unit ID)                        |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |       UNIT_ID,continued       |    JUMPERS    |  TEST_RESULT  |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |       VERSION_NUMBER          |      PERIOD_OF_STATISTICS     |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |       NUMBER_OF_CRCs          |     NUMBER_ALIGNMENT_ERRORS   |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |       NUMBER_OF_COLLISIONS    |        NUMBER_SEND_ABORTS     |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                       NUMBER_GOOD_SENDS                       |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                      NUMBER_GOOD_RECEIVES                     |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |       NUMBER_RETRANSMITS      | NUMBER_NO_RESOURCE_CONDITIONS |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |  NUMBER_FREE_COMMAND_BLOCKS   |  TOTAL_NUMBER_COMMAND_BLOCKS  |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |MAX_TOTAL_NUMBER_COMMAND_BLOCKS|    NUMBER_PENDING_SESSIONS    |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |  MAX_NUMBER_PENDING_SESSIONS  |  MAX_TOTAL_SESSIONS_POSSIBLE  |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |   SESSION_DATA_PACKET_SIZE    |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.18
  /// and here: http://www.ubiqx.org/cifs/NetBIOS.html (section 1.4.3.5)
  /// This class is used only in the RDATA field of the NbNsNodeStatusResponseResourceRecord
  /// As per the second link above, most implementations put different values than those described above
  /// into this field. Microsoft puts e.g. the MAC address in the UNIT_ID field; Samba leaves all fields empty.
  /// We therefore only implement the UNIT_ID field.
  /// </remarks>
  public class NbNsStatistics : NbPacketSegmentBase
  {
    #region Consts

    private const int OFFSET_UNIT_ID = 0;
    public const int STATISTICS_LENGTH = 48;

    #endregion

    #region Private fields

    private readonly byte[] _buffer = new byte[STATISTICS_LENGTH];

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbNsStatistics"/> with all values set to zero
    /// </summary>
    public NbNsStatistics()
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="NbNsStatistics"/> based on the given <see cref="buffer"/>
    /// </summary>
    /// <param name="buffer">Byte array containing the <see cref="NbNsStatistics"/> field</param>
    /// <remarks>
    /// <see cref="buffer"/> must not be null and contain exactly <see cref="STATISTICS_LENGTH"/> bytes.
    /// </remarks>
    public NbNsStatistics(byte[] buffer)
    {
      if (buffer == null || buffer.Length != STATISTICS_LENGTH)
        throw new ArgumentException("buffer");
      buffer.CopyTo(_buffer, 0);
    }

    #endregion

    #region Public properties

    /// <summary>
    /// For packets from Windows Nodes, this field contains the MAC address
    /// </summary>
    public String UnitId { get { return String.Format("{0:X2}:{1:X2}:{2:X2}:{3:X2}:{4:X2}:{5:X2}", _buffer[OFFSET_UNIT_ID], _buffer[OFFSET_UNIT_ID + 1], _buffer[OFFSET_UNIT_ID + 2], _buffer[OFFSET_UNIT_ID + 3], _buffer[OFFSET_UNIT_ID + 4], _buffer[OFFSET_UNIT_ID + 5]); }}

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsStatistics"/> from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsStatistics</param>
    /// <param name="offset">Zero based offset in the buffer where the NbNsStatistics starts</param>
    /// <param name="statistics">Parsed NbNsStatistics if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, int offset, out NbNsStatistics statistics)
    {
      statistics = null;
      if (buffer == null)
        return false;
      if (offset < 0 || offset > buffer.Length - STATISTICS_LENGTH)
        return false;
      statistics = new NbNsStatistics(buffer.Skip(offset).Take(STATISTICS_LENGTH).ToArray());
      return true;
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      get { return STATISTICS_LENGTH; }
    }

    public override byte[] ByteArray
    {
      get { return _buffer; }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("    NbNsStatistics:");
      builder.AppendLine(String.Format("      UnitId: {0}", UnitId));
      return builder.ToString();
    }

    #endregion
  }
}
