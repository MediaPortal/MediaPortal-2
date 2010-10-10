#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using DirectShowLib;
using Microsoft.Win32;

namespace MediaPortal.UI.Players.Video.Tools
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

    #region Constants

    public static Guid WMMEDIASUBTYPE_ACELPnet = new Guid("00000130-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_Base = new Guid("00000000-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_DRM = new Guid("00000009-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_MP3 = new Guid("00000055-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_MP43 = new Guid("3334504D-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_MP4S = new Guid("5334504D-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_M4S2 = new Guid("3253344D-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_P422 = new Guid("32323450-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_MPEG2_VIDEO = new Guid("e06d8026-db46-11cf-b4d1-00805f6cbbea");
    public static Guid WMMEDIASUBTYPE_MSS1 = new Guid("3153534D-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_MSS2 = new Guid("3253534D-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WebStream = new Guid("776257d4-c627-41cb-8f81-7ac7ff1c40cc");
    public static Guid WMMEDIASUBTYPE_WMAudio_Lossless = new Guid("00000163-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMAudioV2 = new Guid("00000161-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMAudioV7 = new Guid("00000161-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMAudioV8 = new Guid("00000161-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMAudioV9 = new Guid("00000162-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMSP1 = new Guid("0000000A-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMV1 = new Guid("31564D57-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMV2 = new Guid("32564D57-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMV3 = new Guid("33564D57-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMVA = new Guid("41564D57-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WMVP = new Guid("50564D57-0000-0010-8000-00AA00389B71");
    public static Guid WMMEDIASUBTYPE_WVP2 = new Guid("32505657-0000-0010-8000-00AA00389B71");
    public static Guid MEDIASUBTYPE_AC3_AUDIO = new Guid("e06d802c-db46-11cf-b4d1-00805f6cbbea");
    public static Guid MEDIASUBTYPE_AC3_AUDIO_OTHER = new Guid("00002000-0000-0010-8000-00aa00389b71");
    public static Guid MEDIASUBTYPE_DDPLUS_AUDIO = new Guid("a7fb87af-2d02-42fb-a4d4-05cd93843bdd");
    public static Guid MEDIASUBTYPE_MPEG1_PAYLOAD = new Guid("e436eb81-524f-11ce-9f53-0020af0ba770");
    public static Guid MEDIASUBTYPE_MPEG1_AUDIO = new Guid("e436eb87-524f-11ce-9f53-0020af0ba770");
    public static Guid MEDIASUBTYPE_MPEG2_AUDIO = new Guid("e06d802b-db46-11cf-b4d1-00805f6cbbea");
    public static Guid MEDIASUBTYPE_LATM_AAC_AUDIO = new Guid("000001ff-0000-0010-8000-00aa00389b71");
    public static Guid MEDIASUBTYPE_AAC_AUDIO = new Guid("000000ff-0000-0010-8000-00aa00389b71");

    #endregion

    #region Properties

    public List<CodecInfo> CodecList 
    {
      get
      {
        return _codecList;
      }
    }
    
    #endregion

    #region Variables

    readonly List<CodecInfo> _codecList;

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
      if (DoesComObjectExists(newCodec.CLSID.ToString()))
      {
        _codecList.Add(newCodec);
      }
    }

    /// <summary>
    /// Checks to see if a COM object is registered and exists on the filesystem.
    /// </summary>
    /// <param name="clsid">class id</param>
    /// <returns>true if exists.</returns>
    public static bool DoesComObjectExists(string clsid)
    {
      using (RegistryKey myRegKey = Registry.LocalMachine)
      {
        Object val;

        try
        {
          // get the pathname to the COM server DLL if the key exists
          using (RegistryKey subKey = myRegKey.OpenSubKey(@"SOFTWARE\Classes\CLSID\" + clsid + @"\InprocServer32"))
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
          return System.IO.File.Exists(val.ToString());
        }
        catch
        {
          return false;
        }
      }
    }

    /// <summary>
    /// Sets a codec as preferred for the Capability
    /// </summary>
    /// <param name="codecName">Name of codec</param>
    /// <param name="capability">Capability to prefer codec for</param>
    public void SetPreferred(String codecName, CodecCapabilities capability)
    {
      foreach (CodecInfo currentCodec in _codecList)
      {
        // Does codec support this capability ?
        if ((currentCodec.Capabilities & capability) != 0)
        {
          currentCodec.Preferred = currentCodec.Name == codecName;
        }
      }
      // sort list by "preferred" property
      _codecList.Sort(); 
    }
    public CodecHandler()
    {
      _codecList = new List<CodecInfo>();
    }


    /// <summary>
    /// Gets a list of DirectShow filter names that accept the passed MediaType/MediaSubType.
    /// </summary>
    /// <param name="mediaType">MediaType</param>
    /// <param name="mediaSubType">MediaSubType</param>
    /// <returns>List of names</returns>
    public static List<CodecInfo> GetFilters(Guid mediaType, Guid mediaSubType)
    {
      return GetFilters(mediaType, mediaSubType, (Merit)0x080001);
    }

    /// <summary>
    /// Gets a list of DirectShow filter names that accept the passed MediaType/MediaSubType and minimum Merit.
    /// </summary>
    /// <param name="mediaType">MediaType</param>
    /// <param name="mediaSubType">MediaSubType</param>
    /// <param name="merit">Minimum merit</param>
    /// <returns>List of names</returns>
    public static List<CodecInfo> GetFilters(Guid mediaType, Guid mediaSubType, Merit merit)
    {
      return GetFilters(new Guid[] { mediaType, mediaSubType }, new Guid[0], merit);
    }

    /// <summary>
    /// Gets a list of DirectShow filter names that accept the passed MediaType/MediaSubType and output the passed MediaType/MediaSubType.
    /// </summary>
    /// <param name="inputMediaAndSubTypes">Array of MediaType/MediaSubType</param>
    /// <param name="outputMediaAndSubTypes">Array of MediaType/MediaSubType</param>
    /// <returns>List of names</returns>
    public static List<CodecInfo> GetFilters(Guid[] inputMediaAndSubTypes, Guid[] outputMediaAndSubTypes)
    {
      return GetFilters(inputMediaAndSubTypes, outputMediaAndSubTypes, (Merit) 0x080001);
    }

    /// <summary>
    /// Gets a list of DirectShow filter names that accept the passed MediaType/MediaSubType and output the passed MediaType/MediaSubType.
    /// </summary>
    /// <param name="inputMediaAndSubTypes">Array of MediaType/MediaSubType</param>
    /// <param name="outputMediaAndSubTypes">Array of MediaType/MediaSubType</param>
    /// <param name="merit"></param>
    /// <returns>List of names</returns>
    public static List<CodecInfo> GetFilters(Guid[] inputMediaAndSubTypes, Guid[] outputMediaAndSubTypes, Merit merit)
    {
      List<CodecInfo> filters = new List<CodecInfo>();
      IEnumMoniker enumMoniker = null;
      IMoniker[] moniker = new IMoniker[1];
      IFilterMapper2 mapper = (IFilterMapper2) new FilterMapper2();
      try
      {
        int hResult = mapper.EnumMatchingFilters(
            out enumMoniker,
            0,
            true,
            merit,
            true,
            inputMediaAndSubTypes.Length,
            inputMediaAndSubTypes,
            null,
            null,
            false,
            true,
            outputMediaAndSubTypes.Length,
            outputMediaAndSubTypes,
            null,
            null);
        do
        {
          hResult = enumMoniker.Next(1, moniker, IntPtr.Zero);
          if ((moniker[0] == null))
            break;

          string filterName = FilterGraphTools.GetFriendlyName(moniker[0]);
          Guid filterClassId = FilterGraphTools.GetCLSID(moniker[0]);
          CodecInfo codecInfo = new CodecInfo(filterName, CodecCapabilities.None, filterClassId);
          filters.Add(codecInfo);
          
          FilterGraphTools.TryRelease(ref moniker[0]);
        } while (true);
        filters.Sort();
        return filters;
      }finally
      {
        FilterGraphTools.TryRelease(ref enumMoniker);
      }
    }
  }
}