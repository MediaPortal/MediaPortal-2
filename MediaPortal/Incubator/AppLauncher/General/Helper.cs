#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.AppLauncher.Settings;

namespace MediaPortal.Plugins.AppLauncher.General
{
  /// <summary>
  /// Little Helpers 
  /// </summary>
  public class Helper
  {
    public static string AppLauncherFolder = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\AppLauncher");

    public static List<string> Groups = new List<string>(); 

    /// <summary>
    /// Get the Path for a Icon. If no Path found, the Icon will saved.
    /// </summary>
    /// <param name="title">Title for the Icon</param>
    /// <param name="bmp">The Icon as .bmp</param>
    /// <returns>Path for the Icon</returns>
    public static string GetIconPath(string title, Image bmp)
    {
      var path = AppLauncherFolder + "\\" + title + ".bmp";

      if (!IconExists(path))
      {
        bmp.Save(path);
      }
      return path;
    }

    private static bool IconExists(string title)
    {
      if (!Directory.Exists(AppLauncherFolder))
        Directory.CreateDirectory(AppLauncherFolder);
      return File.Exists(title);
    }

    public static void SaveApps(Apps apps)
    {
      foreach (var a in apps.AppsList)
      {
        if (a.Password == "") continue;
        var s = Crypter.Encrypt(a.Password);
        a.Password = s;
      }
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(apps);
    }

    public static Apps LoadApps(bool decryptPasswords)
    {
      var settingsManager = ServiceRegistration.Get<ISettingsManager>();
      var apps = settingsManager.Load<Apps>() ?? new Apps(new List<App>());
      if (!decryptPasswords)
        return apps;

      foreach (var a in apps.AppsList)
      {
        if (a.Password == "") continue;
        var s = Crypter.Decrypt(a.Password);
        a.Password = s;
      }
      return apps;
    }
  }

  /// <summary>
  /// Cryptographer for strings e.g. Password
  /// </summary>
  public class Crypter
  {
    private const string KEY = "MediaPortal2";

    /// <summary>
    /// Encrypt the given string using the default key.
    /// </summary>
    /// <param name="strToEncrypt">The string to be encrypted.</param>
    /// <returns>The encrypted string.</returns>
    public static string Encrypt(string strToEncrypt)
    {
      try
      {
        return Encrypt(strToEncrypt, KEY);
      }
      catch (Exception ex)
      {
        return "Wrong Input. " + ex.Message;
      }
    }

    /// <summary>
    /// Decrypt the given string using the default key.
    /// </summary>
    /// <param name="strEncrypted">The string to be decrypted.</param>
    /// <returns>The decrypted string.</returns>
    public static string Decrypt(string strEncrypted)
    {
      try
      {
        return Decrypt(strEncrypted, KEY);
      }
      catch (Exception ex)
      {
        return "Wrong Input. " + ex.Message;
      }
    }

    /// <summary>
    /// Encrypt the given string using the specified key.
    /// </summary>
    /// <param name="strToEncrypt">The string to be encrypted.</param>
    /// <param name="strKey">The encryption key.</param>
    /// <returns>The encrypted string.</returns>
    private static string Encrypt(string strToEncrypt, string strKey)
    {
      try
      {
        var objDesCrypto = new TripleDESCryptoServiceProvider();
        var objHashMd5 = new MD5CryptoServiceProvider();
        var strTempKey = strKey;
        var byteHash = objHashMd5.ComputeHash(Encoding.Default.GetBytes(strTempKey));

        objDesCrypto.Key = byteHash;
        objDesCrypto.Mode = CipherMode.ECB;

        var byteBuff = Encoding.Default.GetBytes(strToEncrypt);
        return Convert.ToBase64String(objDesCrypto.CreateEncryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));
      }
      catch (Exception ex)
      {
        return "Wrong Input. " + ex.Message;
      }
    }


    /// <summary>
    /// Decrypt the given string using the specified key.
    /// </summary>
    /// <param name="strEncrypted">The string to be decrypted.</param>
    /// <param name="strKey">The decryption key.</param>
    /// <returns>The decrypted string.</returns>
    private static string Decrypt(string strEncrypted, string strKey)
    {
      try
      {
        var objDesCrypto = new TripleDESCryptoServiceProvider();
        var objHashMd5 = new MD5CryptoServiceProvider();
        var strTempKey = strKey;
        var byteHash = objHashMd5.ComputeHash(Encoding.Default.GetBytes(strTempKey));

        objDesCrypto.Key = byteHash;
        objDesCrypto.Mode = CipherMode.ECB;

        var byteBuff = Convert.FromBase64String(strEncrypted);
        var strDecrypted = Encoding.Default.GetString(objDesCrypto.CreateDecryptor().TransformFinalBlock(byteBuff, 0, byteBuff.Length));

        return strDecrypted;
      }
      catch (Exception ex)
      {
        return "Wrong Input. " + ex.Message;
      }
    }
  }
}
