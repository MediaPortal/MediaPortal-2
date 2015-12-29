#region Copyright (C) 2011-2013 MPExtended

// Copyright (C) 2011-2013 MPExtended Developers, http://www.mpextended.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace MediaPortal.Plugins.MP2Extended.Authentication
{
  internal static class Encryption
  {
    // Given the fact that our source, including our signing key, is open source, this isn't secure at all. It's used
    // to avoid having passwords in plain text in the config file, which will be good enough,.

    private static readonly byte[] SALT = Guid.Parse("{91f7ed53-f726-4880-97aa-4a13edcb6286}").ToByteArray();

    public static string Encrypt(string key, string text)
    {
      SymmetricAlgorithm enc = GetAlgorithm(key);
      try
      {
        // do the actual encryption
        var encryptor = enc.CreateEncryptor(enc.Key, enc.IV);
        using (var dataStream = new MemoryStream())
        {
          using (var cryptoStream = new CryptoStream(dataStream, encryptor, CryptoStreamMode.Write))
          {
            using (var writer = new StreamWriter(cryptoStream))
            {
              writer.Write(text);
            }
          }

          // get data
          return Convert.ToBase64String(dataStream.ToArray());
        }
      }
      finally
      {
        enc.Clear();
      }
    }

    public static string Decrypt(string key, string text)
    {
      SymmetricAlgorithm dec = GetAlgorithm(key);
      try
      {
        // do the actual encryption
        var decryptor = dec.CreateDecryptor(dec.Key, dec.IV);
        byte[] data = Convert.FromBase64String(text);
        using (var dataStream = new MemoryStream(data))
        {
          using (var cryptoStream = new CryptoStream(dataStream, decryptor, CryptoStreamMode.Read))
          {
            using (var reader = new StreamReader(cryptoStream))
            {
              // get data
              return reader.ReadToEnd();
            }
          }
        }
      }
      finally
      {
        dec.Clear();
      }
    }

    private static SymmetricAlgorithm GetAlgorithm(string key)
    {
      // We Xor the key, which is available in the src, with the motherboard Id to get a more secret key
      string finalKey = XorIt(GetMotherboardId(), key);
      
      SymmetricAlgorithm encryptionAlgorithm = new AesManaged();
      encryptionAlgorithm.KeySize = 256;

      var keyDerive = new Rfc2898DeriveBytes(finalKey, SALT);
      encryptionAlgorithm.Key = keyDerive.GetBytes(encryptionAlgorithm.KeySize / 8);
      encryptionAlgorithm.IV = keyDerive.GetBytes(encryptionAlgorithm.BlockSize / 8);

      return encryptionAlgorithm;
    }

    private static string GetMotherboardId()
    {
      ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
      ManagementObjectCollection moc = mos.Get();
      string motherBoard = "789745136";  // random number in case we don't find an id
      foreach (var o in moc)
      {
        var mo = (ManagementObject)o;
        motherBoard = (string)mo["SerialNumber"];
      }

      return motherBoard;
    }

    public static string XorIt(string key, string input)
    {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < input.Length; i++)
        sb.Append((char)(input[i] ^ key[(i % key.Length)]));
      String result = sb.ToString();

      return result;
    }
  }
}