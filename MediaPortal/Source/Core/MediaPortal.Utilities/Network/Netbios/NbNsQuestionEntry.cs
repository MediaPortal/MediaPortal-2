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
  /// Represents a Netbios Nameservice QuestionEntry
  /// </summary>
  /// <remarks>
  /// A Netbios Nameservice QuestionEntry has the following format:
  ///                      1 1 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 2 2 3 3
  ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |                                                               |
  /// /                         QUESTION_NAME                         /
  /// /                                                               /
  /// |                                                               |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// |         QUESTION_TYPE         |        QUESTION_CLASS         |
  /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
  /// Details can be found here: http://tools.ietf.org/html/rfc1002#section-4.2.1.2
  /// </remarks>
  public class NbNsQuestionEntry : NbPacketSegmentBase
  {
    #region Enums

    public enum QuestionTypeSpecifier : ushort
    {
      Nb = 0x0020,
      NbStat = 0x0021
    }

    public enum QuestionClassSpecifier : ushort
    {
      In = 0x0001
    }

    #endregion

    #region Private fields

    private readonly NbName _questionName;
    private readonly QuestionTypeSpecifier _questionType;
    private readonly QuestionClassSpecifier _questionClass;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new instance of <see cref="NbNsQuestionEntry"/> based on the given <see cref="questionName"/> and <see cref="questionType"/>
    /// </summary>
    /// <param name="questionName"><see cref="NbName"/> used for this object</param>
    /// <param name="questionType"><see cref="QuestionTypeSpecifier"/> used for this object</param>
    /// <remarks>
    /// <see cref="_questionClass"/> is always set to <see cref="QuestionClassSpecifier.In"/> as per NbNs spec.
    /// </remarks>
    public NbNsQuestionEntry(NbName questionName, QuestionTypeSpecifier questionType)
    {
      _questionName = questionName;
      _questionType = questionType;
      _questionClass = QuestionClassSpecifier.In;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// <see cref="NbName"/> of this <see cref="NbNsQuestionEntry"/>
    /// </summary>
    public NbName QuestionName { get { return _questionName; } }

    /// <summary>
    /// <see cref="QuestionTypeSpecifier"/> of this <see cref="NbNsQuestionEntry"/>
    /// </summary>
    public QuestionTypeSpecifier QuestionType { get { return _questionType; } }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbNsQuestionEntry"/> from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbNsQuestionEntry</param>
    /// <param name="offset">Zero based offset in the buffer where the NbNsQuestionEntry starts</param>
    /// <param name="questionEntry">Parsed NbNsQuestionEntry if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, int offset, out NbNsQuestionEntry questionEntry)
    {
      questionEntry = null;
      NbName questionName;
      if (!NbName.TryParse(buffer, offset, out questionName))
        return false;
      if (buffer.Length < offset + questionName.Length + 4)
        return false;

      var questionType = (QuestionTypeSpecifier)BufferToUInt16(buffer, offset + questionName.Length);
      if (!Enum.IsDefined(typeof(QuestionTypeSpecifier), questionType))
        return false;

      var questionClass = (QuestionClassSpecifier)BufferToUInt16(buffer, offset + questionName.Length + 2);
      if (!Enum.IsDefined(typeof(QuestionClassSpecifier), questionClass))
        return false;

      questionEntry = new NbNsQuestionEntry(questionName, questionType);
      return true;
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      // The length of the QuestionEntry is the length of the _questionName plus two bytes each,
      // for the _questionType and the _questionClass
      get { return _questionName.Length + 4; }
    }

    public override byte[] ByteArray
    {
      get
      {
        var result = new byte[Length];
        _questionName.ByteArray.CopyTo(result, 0);
        UInt16ToBuffer((UInt16)_questionType, result, _questionName.Length);
        UInt16ToBuffer((UInt16)_questionClass, result, _questionName.Length + 2);
        return result;
      }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine("  NbNsQuestionEntry:");
      builder.Append(QuestionName);
      builder.AppendLine(String.Format("    QuestionType: {0}", QuestionType));
      builder.AppendLine(String.Format("    QuestionClass: {0}", _questionClass));
      return builder.ToString();
    }

    #endregion
  }
}
