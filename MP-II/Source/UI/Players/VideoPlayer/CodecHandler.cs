#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Ui.Players.Video
{
  public class CodecHandler
  {
    #region Enums

    [Flags]
    public enum CodecCapabilities
    {
      /// <summary>
      /// No special capabilities or restrictions.
      /// </summary>
      None = 0,
      /// <summary>
      /// MPEG audio decoding support.
      /// </summary>
      AudioMPEG = 1,
      /// <summary>
      /// MPEG AAC audion decoding support.
      /// </summary>
      AudioAAC = 2,
      /// <summary>
      /// MPEG2 video decoding support.
      /// </summary>
      VideoMPEG2 = 4,
      /// <summary>
      /// MPEG4/H264 video decoding support.
      /// </summary>
      VideoH264 = 8,
      /// <summary>
      /// DivX video decoding support.
      /// </summary>
      VideoDIVX = 16,

      /// <summary>
      /// Restricted to only one video codec in graph.
      /// </summary>
      SingleVideoCodecOnly = 1024
    }

    #endregion

    #region Properties

    public List<CodecInfo> CodecList 
    {
      get
      {
        return codecList;
      }
    }
    
    #endregion

    #region Variables

    List<CodecInfo> codecList;

    #endregion

    /// <summary>
    /// Checks if all capabilities are supported
    /// </summary>
    /// <param name="capabilities">Capabilities to check in</param>
    /// <param name="checkCapability">Capabilities to check for</param>
    /// <returns></returns>
    public bool Supports(CodecCapabilities capabilities, CodecCapabilities checkCapability)
    {
      return (capabilities & checkCapability) == checkCapability;
    }


    /// <summary>
    /// Check if codec exists and add it to list
    /// </summary>
    /// <param name="newCodec">Codec to add</param>
    public void TryAdd(CodecInfo newCodec)
    {
      if (DoesComObjectExists(newCodec.CLSID))
      {
        codecList.Add(newCodec);
      }
    }

    // checks to see if a COM object is registered and exists on the filesystem
    public static bool DoesComObjectExists(string CLSID)
    {
      using (RegistryKey myRegKey = Registry.LocalMachine)
      {
        Object val;

        try
        {
          // get the pathname to the COM server DLL if the key exists
          using (RegistryKey subKey = myRegKey.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + CLSID + @"\InprocServer32"))
          {
            if (subKey == null)
            {
              return false;
            }
            val = subKey.GetValue(null); // the null gets default
          }
        }
        catch
        {
          return false;
        }

        try
        {
          // parse out the version number embedded in the resource
          // in the DLL
          if (!System.IO.File.Exists(val.ToString()))
            return false;
        }
        catch
        {
          return false;
        }

        return true;
      }
    }

    /// <summary>
    /// Sets a codec as preferred for the Capability
    /// </summary>
    /// <param name="CodecName">Name of codec</param>
    /// <param name="Capability">Capability to prefer codec for</param>
    public void SetPreferred(String CodecName, CodecCapabilities Capability)
    {
      foreach (CodecInfo currentCodec in codecList)
      {
        // Does codec support this capability ?
        if ((currentCodec.Capabilities & Capability) != 0)
        {
          if (currentCodec.Name == CodecName)
          {
            currentCodec.Preferred = true;
          }
          else
          {
            currentCodec.Preferred = false;
          }
        }
      }
      // sort list by "preferred" property
      codecList.Sort(); 
    }
    public CodecHandler()
    {
      codecList = new List<CodecInfo>();

      // add known other audio and video codecs
      TryAdd(new CodecInfo("Microsoft DTV-DVD Video Decoder", CodecCapabilities.VideoH264 | CodecCapabilities.VideoMPEG2 | CodecCapabilities.SingleVideoCodecOnly, "{212690FB-83E5-4526-8FD7-74478B7939CD}"));
      TryAdd(new CodecInfo("CyberLink H.264/AVC Decoder", CodecCapabilities.VideoH264, "{5FFFC195-E3DE-4219-981E-FFA227A02FBB}"));
      TryAdd(new CodecInfo("CyberLink H.264/AVC Decoder (PDVD7)", CodecCapabilities.VideoH264, "{D12E285B-3B29-4416-BA8E-79BD81D193CC}"));
      TryAdd(new CodecInfo("CyberLink H.264/AVC Decoder (PDVD7.X)", CodecCapabilities.VideoH264, "{F2E3D920-0F9B-4319-BE87-EB94CCEB6C09}"));
      TryAdd(new CodecInfo("CoreAVC Video Decoder", CodecCapabilities.VideoH264, "{09571A4B-F1FE-4C60-9760-DE6D310C7C31}"));
      TryAdd(new CodecInfo("MPC Video Decoder", CodecCapabilities.VideoH264 | CodecCapabilities.VideoMPEG2, "{008BAC12-FBAF-497B-9670-BC6F6FBAE2C4}"));
      

      TryAdd(new CodecInfo("CyberLink Video/SP Decoder", CodecCapabilities.VideoMPEG2, "{C81C8C5A-B354-4DEB-96D3-8BD8D0C8ABD0}"));
      TryAdd(new CodecInfo("CyberLink Video/SP Decoder", CodecCapabilities.VideoMPEG2, "{B828D96A-4AC3-4C2E-9AD4-3596EE9A3046}"));
      TryAdd(new CodecInfo("CyberLink Video/SP Decoder (PDVD7)", CodecCapabilities.VideoMPEG2, "{8ACD52ED-9C2D-4008-9129-DCE955D86065}"));
      TryAdd(new CodecInfo("Microsoft MPEG-2 Video Decoder", CodecCapabilities.VideoMPEG2, "{212690FB-83E5-4526-8FD7-74478B7939CD}"));
      TryAdd(new CodecInfo("NVIDIA Video Decoder", CodecCapabilities.VideoMPEG2, "{71E4616A-DB5E-452B-8CA5-71D9CC7805E9}"));
      TryAdd(new CodecInfo("MPV Decoder Filter", CodecCapabilities.VideoMPEG2, "{39F498AF-1A09-4275-B193-673B0BA3D478}"));
      TryAdd(new CodecInfo("InterVideo Video Decoder", CodecCapabilities.VideoMPEG2, "{0246CA20-776D-11D2-8010-00104B9B8592}"));

      TryAdd(new CodecInfo("Microsoft DTV-DVD Audio Decoder", CodecCapabilities.AudioMPEG /*| CodecCapabilities.AudioAAC*/, "{E1F1A0B8-BEEE-490D-BA7C-066C40B5E2B9}"));
      TryAdd(new CodecInfo("CyberLink Audio Decoder (PDVD7)", CodecCapabilities.AudioMPEG, "{284DC28A-4A7D-442C-BC2E-D7480556E4D8}"));
      TryAdd(new CodecInfo("CyberLink Audio Decoder (PDVD7.x)", CodecCapabilities.AudioMPEG, "{D5DBA1A7-61A0-437E-B6AB-C9C422F466B5}"));
      TryAdd(new CodecInfo("CyberLink Audio Decoder (PDVD7 UPnP)", CodecCapabilities.AudioMPEG, "{706E503A-EB19-4106-9D7C-0384359D511A}"));
      TryAdd(new CodecInfo("CyberLink Audio Decoder", CodecCapabilities.AudioMPEG, "{B60C424E-AB7B-429F-9B9B-93684E51EA75}"));
      TryAdd(new CodecInfo("CyberLink Audio Decoder", CodecCapabilities.AudioMPEG, "{03EC05EA-C2A7-49A8-971F-580D5891F2FB}"));
      TryAdd(new CodecInfo("NVIDIA Audio Decoder", CodecCapabilities.AudioMPEG, "{6C0BDF86-C36A-4D83-8BDB-312D2EAF409E}"));
      TryAdd(new CodecInfo("MPA Decoder Filter", CodecCapabilities.AudioMPEG, "{3D446B6F-71DE-4437-BE15-8CE47174340F}"));
      TryAdd(new CodecInfo("ffdshow Audio Decoder", CodecCapabilities.AudioMPEG | CodecCapabilities.AudioAAC, "{0F40E1E5-4F79-4988-B1A9-CC98794E6B55}"));
      TryAdd(new CodecInfo("Microsoft MPEG-1/DD Audio Decoder", CodecCapabilities.AudioMPEG, "{E1F1A0B8-BEEE-490D-BA7C-066C40B5E2B9}"));
      TryAdd(new CodecInfo("InterVideo Audio Decoder", CodecCapabilities.AudioMPEG, "{7E2E0DC1-31FD-11D2-9C21-00104B3801F6}"));

      TryAdd(new CodecInfo("ffdshow Video Decoder", CodecCapabilities.VideoDIVX, "{04FE9017-F873-410E-871E-AB91661A4EF7}"));
      TryAdd(new CodecInfo("DivX Decoder Filter", CodecCapabilities.VideoDIVX, "{78766964-0000-0010-8000-00AA00389B71}"));
      TryAdd(new CodecInfo("Xvid MPEG-4 Video Decoder", CodecCapabilities.VideoDIVX, "{64697678-0000-0010-8000-00AA00389B71}"));
    }
  }
}
