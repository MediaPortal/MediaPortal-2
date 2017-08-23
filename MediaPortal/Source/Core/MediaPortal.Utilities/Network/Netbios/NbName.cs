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
  /// Represents a Netbios Name
  /// </summary>
  /// <remarks>
  /// A Netbios Name consists of two parts: The Name label and (optionally) a scope.
  /// Name label:
  ///    The Name label represents the actual name of a resource. It can exist in encoded form and in unencoded form.
  ///    In a Netbios Packet it has nearly always the encoded form.
  ///    - In unencoded form, the name label has between 1 and 16 characters. In Microsoft's implementation the 16th character
  ///      is the so called Netbios Suffix, which represents the type of a resource so that the actual name can only have
  ///      up to 15 characters. There are theoretically some restrictions on the characters that can be contained in an
  ///      unencoded name label - some sources even say that only 'a'-'z', 'A'-'Z', '0'-'9' and '-' may be used. In practice no
  ///      implementation obeys these restrictions. To be as compatibly as possible, we therefore allow any character that
  ///      may be represented by a single byte using the ISO-8859-1 encoding. This encoding (contrary to ASCII encoding) uses
  ///      the full 8 bits and does not allow charactes with more than 1 byte. There is a wildcard name "*" that represents
  ///      any possible resource (e.g. in a query).
  ///    - In encoded form, the Name label always consts of 32 characters. The encoding works as follows:
  ///      x The unencoded Name label is right-padded with spaces to exactly 16 characters.
  ///      x The wildcard name is not padded with spaces but with 0x00.
  ///      x In Microsoft implementations the 16th character is replaced with the Netbios Suffix.
  ///      x The string representation is converted into 16 bytes.
  ///      x Every byte is splitted in a high nibble and a low nibble.
  ///      x To each nibble, the value 0x41 (representing an 'A') is added.
  ///      x Instead of the original character, the characters represented by the high nibble and the low nibble (each increased
  ///        by 0x41) are concatenated to a string, resulting in a 32 character string, each character in the range from 'A' to 'P'
  ///      An unencoded name of "FRED" e.g. results in an encoded form of "EGFCEFEECACACACACACACACACACACACA" (wihout Netbios Suffix).
  /// Scope:
  ///    Different Netbios Scopes are separated from each other so that computers in different Scopes do not see each other
  ///    although they may be in the same network. The use of the Scope is discouraged. Current versions of Windows do not
  ///    allow entering a Netbios Scope unless you edit the registry. Nevertheless, we have to support the Scope because if
  ///    there is a Scope present in a Netbios name, the structure of the Netbios Packet is different.
  ///    A Scope consist of 1 to n Scope labels separated by '.'. Each Scope label may have up to 63 characters. These characters
  ///    are not encoded. An example for a scope is "MEDIAPORTAL.COM".
  /// The so called Level-One-Encoded full Netbios name with an unencoded Name label of "FRED" and a Scope of "MEDIAPORTAL.COM"
  /// then looks like: "EGFCEFEECACACACACACACACACACACACA.MEDIAPORTAL.COM"
  /// Before this Netbios Name is put in a Netbios Package, it is "Level-Two-Encoded". This means that the dots are removed and
  /// in front of every label (Name label and Scope labels) a byte representing the length of the following label is prepended
  /// (LabelLength). The LabelLength of the Name label is always 0x20 (32). At the end of all labels of a Netbios Name, a 0x00
  /// byte is appended. The total length of a Level-Two-Encoded Netibios Name must not exceed 255 bytes.
  /// The two highest bits of a LabelLength byte must be 0 so that the maximum LabelLength is 63. If the two highest bits of a
  /// LabelLength byte are 1, the 14 following bits represent a pointer to a specific byte number in the Netbios Packet. The
  /// Netbios Name is then continued with the label at the position, the pointer points to.
  /// Testing two Netbios Names for equality must be case invariant. It is, however, not clear whether the unencoded Name label
  /// and the Scope labels must be compared (this is what Samba does) or whether the Level-One-Encoded form of the Netbios Name
  /// must be compared (this is what Windows does).
  /// For further details see http://tools.ietf.org/html/rfc1002#section-4.1 and http://tools.ietf.org/html/rfc1001#section-14
  /// </remarks>
  public class NbName : NbPacketSegmentBase, IEquatable<NbName>
  {
    #region Consts

    public const string WILDCARD_CLEARTEXT = "*";
    private const char SCOPE_SEPARATOR = '.';
    private const int NAME_LENGTH_ENCODED = 32;
    private const int MAX_NAME_LENGTH_CLEARTEXT = 15;
    private const int MAX_LABEL_LENGTH = 63;
    private const int MAX_DOMAIN_NAME_LENGHT = 255;

    // We use ISO-8859-1 encoding as - contrary to ASCII encoding - it is an 8-bit encoding
    // so that we can represent all byte-values are characters.
    public static readonly Encoding ENCODING = Encoding.GetEncoding("ISO-8859-1", new EncoderExceptionFallback(), new DecoderExceptionFallback());

    #endregion

    #region Enums

    /// <summary>
    /// Selected known Netbios Suffixes
    /// </summary>
    /// <remarks>
    /// For a more extensive list see http://ubiqx.org/cifs/Appendix-C.html
    /// </remarks>
    public enum KnownNetbiosSuffixes : byte 
    {
      // Known Unique Suffixes
      WorkstationService = 0x00,
      MessengerService = 0x01,
      FileServerService = 0x20,
      DomainMasterBrowser = 0x1B,
      MasterBrowser = 0x1D,

      // Known Group Suffixes
      DomainName = 0x00,
      MasterBrowsers = 0x01,
      DomainControllers = 0x1C,
      BrowserServiceElections = 0x1E,
    }

    #endregion

    #region Private fields

    private readonly string _netbiosName;
    private readonly KnownNetbiosSuffixes _netbiosSuffix;
    private readonly string _netbiosScope;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new instance of <see cref="NbName"/>
    /// </summary>
    /// <param name="netbiosName">Unencoded Netbios Name label</param>
    /// <param name="netbiosSuffix">Netbios Suffix</param>
    /// <param name="netbiosScope">Netbios Scope</param>
    /// <remarks>
    /// We only perform length checks of <see cref="netbiosName"/> and <see cref="netbiosScope"/>
    /// The caller must make sure that the characters in these parameters can be converted into a
    /// single byte via ISO-8859-1 encoding. If this is not the case, an exception will be thrown later
    /// when the <see cref="NbName"/> is encoded into a byte array.
    /// </remarks>
    public NbName(string netbiosName, KnownNetbiosSuffixes netbiosSuffix, string netbiosScope = null)
    {
      // Validity checks for netbiosName
      if (netbiosName == null)
        throw new ArgumentNullException("netbiosName");
      if (netbiosName.Length > MAX_NAME_LENGTH_CLEARTEXT || netbiosName.Length < 1)
        throw new ArgumentException("netbiosName");
      _netbiosName = netbiosName;

      _netbiosSuffix = netbiosSuffix;

      // Validity checks for netbiosScope
      if (String.IsNullOrEmpty(netbiosScope))
      {
        _netbiosScope = null;
        return;
      }
      var scopeLabels = netbiosScope.Split(SCOPE_SEPARATOR);
      if (scopeLabels.Any(label => label.Length > MAX_LABEL_LENGTH))
        throw new ArgumentException("netbiosScope");
      _netbiosScope = netbiosScope;

      // Max length check for the whole NbName
      if (GetLength() > MAX_DOMAIN_NAME_LENGHT)
        throw new ArgumentException("netbiosScope");
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Level-One encodes a string an writes it into a byte array
    /// </summary>
    /// <param name="source">String to encode</param>
    /// <param name="buffer">Byte array to write the encoded string to</param>
    /// <param name="offset">Zero based offset in the byte array, where the encoded string is written</param>
    private static void LevelOneEncode(string source, IList<byte> buffer, int offset)
    {
      var sourceBytes = ENCODING.GetBytes(source);
      var counter = 0;
      foreach (var sourceByte in sourceBytes)
      {
        var highNibble = (byte)((sourceByte & 0xF0) >> 4);
        var lowNibble = (byte)(sourceByte & 0x0F);
        buffer[offset + counter * 2] = (byte)(highNibble + 0x41);
        buffer[offset + counter * 2 + 1] = (byte)(lowNibble + 0x41);
        counter++;
      }
    }

    /// <summary>
    /// Decodes a Level-One encoded string
    /// </summary>
    /// <param name="source">Byte array containing the encoded string</param>
    /// <param name="offset">Zero based offset in the byte array where the encoded string starts</param>
    /// <param name="byteCount">Number of bytes starting at offset representing the encoded string</param>
    /// <returns>Unencoded string</returns>
    private static string LevelOneDecode(IList<byte> source, int offset, int byteCount)
    {
      var decodedBytes = new byte[byteCount / 2];
      for (var counter = 0; counter < byteCount / 2; counter++)
        decodedBytes[counter] = (byte)(((source[offset + counter * 2] -0x41) << 4) | (source[offset + counter * 2 + 1] - 0x41));
      return ENCODING.GetString(decodedBytes);
    }

    /// <summary>
    /// Calculats the length in bytes of the <see cref="NbName"/> in Level-Two-encoded form
    /// </summary>
    /// <returns>Length</returns>
    private int GetLength()
    {
      var result = NAME_LENGTH_ENCODED + 1; // 1 is the label length byte before the label itself
      if (_netbiosScope != null)
      {
        var scopeLabels = _netbiosScope.Split(SCOPE_SEPARATOR);
        result += scopeLabels.Sum(label => label.Length + 1); // 1 is the label length byte before the label itself
      }
      result++; // Netbios Name ends with a zero length label byte
      return result;
    }

    #endregion

    #region Public properties

    /// <summary>
    /// Decoded Netbios Name
    /// </summary>
    public string Name { get { return _netbiosName; } }

    /// <summary>
    /// Netbios Name Suffix
    /// </summary>
    public KnownNetbiosSuffixes Suffix { get { return _netbiosSuffix; } }

    /// <summary>
    /// Netbios Scope
    /// </summary>
    public string Scope { get { return _netbiosScope; } }

    /// <summary>
    /// Convenience property returning a wildcard NbName object
    /// </summary>
    public static NbName WildCardName { get { return new NbName(WILDCARD_CLEARTEXT, KnownNetbiosSuffixes.WorkstationService);} }

    #endregion

    #region Public methods

    /// <summary>
    /// Tries to parse a <see cref="NbName"/> from a buffer of bytes
    /// </summary>
    /// <param name="buffer">Byte array containing the NbName</param>
    /// <param name="offset">Zero based offset in the buffer where the NbName starts</param>
    /// <param name="nbName">Parsed NbName if successful, else null</param>
    /// <returns><c>true</c> if parsing was successful, else <c>false</c></returns>
    public static bool TryParse(byte[] buffer, int offset, out NbName nbName)
    {
      nbName = null;
      if (buffer == null)
        return false;
      if (offset < 0 || offset >= buffer.Length)
        return false;

      if (buffer[offset] == 0x00)
        return false;

      string name = null;
      KnownNetbiosSuffixes suffix = 0;
      string scope = null;
      while (buffer[offset] != 0x00)
      {
        // If the current byte is a label string pointer (the first two bits are set)
        if (buffer[offset] >= 192)
        {
          // Then the fourteen bits starting at bit three of the current byte represent a pointer to the label in the buffer
          if (offset + 1 >= buffer.Length)
            return false;
          var pointer = BitConverter.ToUInt16(buffer, offset);
          pointer &= Convert.ToUInt16("0011111111111111", 2);
          if (pointer >= buffer.Length)
            return false;
          offset = pointer;
        }
        // If the current byte is not a laber string pointer (the first two bits are not set)
        else if (buffer[offset] < 64)
        {
          // Then the current byte represents the length of the label
          var length = buffer[offset];
          offset++;
          if (length % 2 != 0 || offset + length >= buffer.Length)
            return false;
          if (name == null)
          {
            // label must be the netbiosName label
            var nameAndSuffix = LevelOneDecode(buffer, offset, length);
            suffix = (KnownNetbiosSuffixes)nameAndSuffix[nameAndSuffix.Length - 1];
            name = nameAndSuffix.Substring(0, nameAndSuffix.Length - 1);
            
            // Wildcard names are padded with null bytes, other names are padded with spaces
            name = name.Trim('\0', ' ');
          }
          else
          {
            // label must be a scope label
            scope = (scope == null) ? ENCODING.GetString(buffer, offset, length) : scope + "." + ENCODING.GetString(buffer, offset, length);
          }
          offset += length;
        }
        else
          return false;
      }

      nbName = new NbName(name, suffix, scope);
      return true;
    }

    #endregion

    #region IEquatable implementation

    public bool Equals(NbName nbName)
    {
      if (ReferenceEquals(nbName, null))
        return false;
      return _netbiosName.Equals(nbName.Name, StringComparison.OrdinalIgnoreCase) && _netbiosSuffix == nbName.Suffix && _netbiosScope.Equals(nbName.Scope, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object obj)
    {
      return Equals(obj as NbName);
    }

    public override int GetHashCode()
    {
      return _netbiosName.ToUpperInvariant().GetHashCode() ^ _netbiosSuffix.GetHashCode() ^ _netbiosScope.ToUpperInvariant().GetHashCode();
    }

    public static bool operator ==(NbName name1, NbName name2)
    {
      return ReferenceEquals(name1, null) ? ReferenceEquals(name2, null) : name1.Equals(name2);
    }

    public static bool operator !=(NbName name1, NbName name2)
    {
      return !(name1 == name2);
    }

    #endregion

    #region Base overrides

    public override int Length
    {
      get { return GetLength(); }
    }

    public override byte[] ByteArray
    {
      get
      {
        var result = new byte[Length];
        var offset = 0;

        result[offset] = NAME_LENGTH_ENCODED;
        offset++;
        var paddedName = (_netbiosName == WILDCARD_CLEARTEXT) ? _netbiosName.PadRight(15, '\0') : _netbiosName.PadRight(15, ' ');
        LevelOneEncode(paddedName + ENCODING.GetString(new[] { (byte)_netbiosSuffix }), result, offset);
        offset += NAME_LENGTH_ENCODED;

        if (_netbiosScope != null)
        {
          var scopeLabels = _netbiosScope.Split(SCOPE_SEPARATOR);
          foreach (var scopeLabel in scopeLabels)
          {
            result[offset] = (byte)scopeLabel.Length;
            offset++;
            var encodedLabel = ENCODING.GetBytes(scopeLabel);
            encodedLabel.CopyTo(result, offset);
            offset += encodedLabel.Length;
          }
        }

        result[offset] = 0x00;
        return result;
      }
    }

    public override string ToString()
    {
      var builder = new StringBuilder();
      builder.AppendLine(String.Format("    NbName: '{0}'", Name));
      builder.AppendLine(String.Format("      Suffix: 0x{0:X2}", (byte)Suffix));
      builder.AppendLine((Scope == null) ? "      Scope: <null>" : String.Format("      Scope: '{0}'", Scope));
      return builder.ToString();
    }

    #endregion
  }
}
