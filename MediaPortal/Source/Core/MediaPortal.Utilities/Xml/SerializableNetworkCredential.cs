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
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.Utilities.Xml
{
  /// <summary>
  /// Serializable <see cref="NetworkCredential"/> that encrypts and decrypts the password during (de)serialization
  /// </summary>
  /// <remarks>
  /// THE ENCRYPTION USED IN THIS CLASS FOR THE PASSWORD IS NOT SECURE!
  /// It is meant as a child protection, but anyone with sufficient programming skills
  /// can decrypt the password. The class makes sure that the decrypted password is not
  /// kept in memory longer than necessary. The encrypted password will be different for
  /// every serialization even if the unencrypted password is the same.
  /// </remarks>
  public class SerializableNetworkCredential : NetworkCredential, IXmlSerializable
  {
    #region consts

    private const string XML_ELEMENT_NAME_USERNAME = "UserName";
    private const string XML_ELEMENT_NAME_DOMAIN = "Domain";
    private const string XML_ELEMENT_NAME_PASSWORD = "EncryptedPassword";

    private const int RFC_2898_ITERATIONS = 1000;
    private const int RANDOMIZER_LENGTH = 3;
    private static readonly Encoding ENCODING = Encoding.Unicode;
    private static readonly byte[] KEY_BYTES = ENCODING.GetBytes("MediaPortal 2");
    private static readonly byte[] SALT_BYTES = ENCODING.GetBytes("Salt for MP2");

    #endregion

    #region private methods

    /// <summary>
    /// Takes a <see cref="SecureString"/>, encrypts it using AES and returns an encrypted <see cref="string"/>
    /// </summary>
    /// <param name="secureStringToEncrypt"><see cref="SecureString"/> to encrypt</param>
    /// <returns>Encrypted <see cref="string"/></returns>
    private static string AesEncrypt(SecureString secureStringToEncrypt)
    {
      using (var ms = new MemoryStream())
        using (var aes = new RijndaelManaged())
        {
          aes.KeySize = 256;
          aes.BlockSize = 128;
          var key = new Rfc2898DeriveBytes(KEY_BYTES, SALT_BYTES, RFC_2898_ITERATIONS);
          aes.Key = key.GetBytes(aes.KeySize / 8);
          aes.IV = key.GetBytes(aes.BlockSize / 8);
          aes.Mode = CipherMode.CBC;
          using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
          {
            var randomBytes = ENCODING.GetBytes(Path.GetRandomFileName().Substring(0, RANDOMIZER_LENGTH));
            cs.Write(randomBytes, 0, randomBytes.Length);
            var intPtr = IntPtr.Zero;
            try
            {
              intPtr = Marshal.SecureStringToGlobalAllocUnicode(secureStringToEncrypt);
              for (var i = 0; i < secureStringToEncrypt.Length * 2; i++)
                cs.WriteByte(Marshal.ReadByte(intPtr, i));
            }
            finally
            {
              if (intPtr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
            }
            cs.Close();
          }
          return Convert.ToBase64String(ms.ToArray());
        }
    }

    /// <summary>
    /// Takes a string encrypted with <see cref="AesEncrypt"/>, decrypts it and returns a <see cref="SecureString"/>
    /// </summary>
    /// <param name="stringToDecrypt"><see cref="string"/> that was encrypted with <see cref="AesEncrypt"/></param>
    /// <returns><see cref="SecureString"/> containing the decrypted <paramref name="stringToDecrypt"/></returns>
    private static SecureString AesDecrypt(string stringToDecrypt)
    {
      var bytesToDecrypt = Convert.FromBase64String(stringToDecrypt);
      using (var ms = new MemoryStream())
        using (var aes = new RijndaelManaged())
        {
          aes.KeySize = 256;
          aes.BlockSize = 128;
          var key = new Rfc2898DeriveBytes(KEY_BYTES, SALT_BYTES, RFC_2898_ITERATIONS);
          aes.Key = key.GetBytes(aes.KeySize / 8);
          aes.IV = key.GetBytes(aes.BlockSize / 8);
          aes.Mode = CipherMode.CBC;
          char[] charArray = null;
          var result = new SecureString();
          try
          {
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
              cs.Write(bytesToDecrypt, 0, bytesToDecrypt.Length);
              cs.Close();
            }
            charArray = ENCODING.GetChars(ms.ToArray());
            for (var i = RANDOMIZER_LENGTH; i < charArray.Length; i++)
              result.AppendChar(charArray[i]);
          }
          finally
          {
            if (charArray != null)
              Array.Clear(charArray, 0, charArray.Length);
            Array.Clear(ms.GetBuffer(), 0, ms.GetBuffer().Length);
          }
          return result;
        }
    }

    #endregion

    #region IXmlSerializable implementation

    public XmlSchema GetSchema()
    {
      return null;
    }

    public void ReadXml(XmlReader reader)
    {
      reader.ReadStartElement();
      UserName = reader.ReadElementContentAsString();
      Domain = reader.ReadElementContentAsString();
      SecurePassword = AesDecrypt(reader.ReadElementContentAsString());
      reader.ReadEndElement();
    }

    public void WriteXml(XmlWriter writer)
    {
      writer.WriteElementString(XML_ELEMENT_NAME_USERNAME, UserName);
      writer.WriteElementString(XML_ELEMENT_NAME_DOMAIN, Domain);
      writer.WriteElementString(XML_ELEMENT_NAME_PASSWORD, AesEncrypt(SecurePassword));
    }

    #endregion
  }
}
