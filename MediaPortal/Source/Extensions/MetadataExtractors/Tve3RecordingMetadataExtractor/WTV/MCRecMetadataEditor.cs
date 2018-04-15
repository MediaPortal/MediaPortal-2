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

#region Original Copyright

/* 
 * Original source of this file: https://mcebuddy2x.codeplex.com/ 
 *  * MCEBuddy 2.x is an open source software project licensed under the GNU General Public License v2. 
 */

#endregion

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

using DirectShowLib;
using DirectShowLib.SBE;

namespace MCEBuddy.MetaData
{
  /// <summary>Metadata editor for DVR-MS files.</summary>
  public sealed class MCRecMetadataEditor : MetadataEditor
  {
    IStreamBufferRecordingAttribute _editor;

    /// <summary>Initializes the editor.</summary>
    /// <param name="filepath">The path to the file.</param>
    public MCRecMetadataEditor(string filepath)
      : base(filepath)
    {
      _editor = (IStreamBufferRecordingAttribute)new StreamBufferRecordingAttributes();//ClassId.CoCreateInstance(ClassId.RecordingAttributes);
      IFileSourceFilter sourceFilter = (IFileSourceFilter)_editor;
      sourceFilter.Load(filepath, null);
    }

    public MCRecMetadataEditor(object editor)
      : base("blank")
    {
      _editor = (IStreamBufferRecordingAttribute)editor;
    }

    /// <summary>Gets all of the attributes on a file.</summary>
    /// <returns>A collection of the attributes from the file.</returns>
    public override System.Collections.IDictionary GetAttributes()
    {
      return GetAttributes(false);
    }

    /// <summary>Gets all of the attributes on a file.</summary>
    /// <returns>A collection of the attributes from the file.</returns>
    public override System.Collections.IDictionary GetAttributes(bool forCopy)
    {
      if (_editor == null) throw new ObjectDisposedException(GetType().Name);

      Hashtable propsRetrieved = new Hashtable();
      List<string> tagsForCopy = new List<string>();

      if (forCopy)
      {
        string overrideFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MetatagOverride.txt");

        if (File.Exists(overrideFile))
        {
          using (StreamReader sr = File.OpenText(overrideFile))
          {
            string tag;
            tag = sr.ReadLine();
            while (!String.IsNullOrEmpty(tag))
            {
              tagsForCopy.Add(tag);
              tag = sr.ReadLine();
            }
          }
        }
        else
        {
          tagsForCopy.AddRange(new string[] { "Description", "WM/ParentalRating", "WM/Provider", "WM/MediaCredits", "WM/MediaIsDelay", "WM/WMRVServiceID", "WM/WMRVInBandRatingLevel", "WM/MediaOriginalRunTime", "WM/MediaIsSAP", "WM/MediaIsFinale", "WM/MediaNetworkAffiliation", "WM/WMRVOriginalSoftPrePadding", "WM/WMRVOriginalSoftPostPadding", "Title", "WM/WMRVDTVContent", "WM/Mood", "WM/MediaIsSubtitled", "WM/WMRVActualSoftPrePadding", "WM/MediaStationName", "WM/ContentGroupDescription", "WM/Language", "WM/ParentalRatingReason", "WM/WMRVEndTime", "WM/WMRVHardPostPadding", "WM/VideoClosedCaptioning", "WM/WMRVInBandRatingAttributes", "WM/WMRVContentProtectedPercent", "WM/MediaIsTape", "WM/WMRVEncodeTime", "WM/MediaIsRepeat", "WM/WMRVHDContent", "WM/SubTitle", "WM/MediaIsLive", "WM/MediaOriginalBroadcastDateTime", "WM/SubTitleDescription", "Author", "WM/WMRVATSCContent", "WM/MediaStationCallSign", "WM/WMRVWatched", "WM/WMRVInBandRatingSystem", "WM/MediaOriginalChannel", "WM/AlbumTitle", "WM/ProviderRating", "WM/ProviderCopyright", "WM/MediaIsPremiere", "WM/WMRVContentProtected", "WM/Genre", "WM/Composer", "WM/OriginalReleaseTime", "WM/WMRVHardPrePadding", "WM/WMRVActualSoftPostPadding", "WM/ToolName", "WM/ToolVersion", "WM/WMRVScheduleItemID", "WM/WMRVRequestID", "WM/WMRVServiceID", "WM/WMRVProgramID", "WM/WMRVContentProtected" });
        }

        foreach (string tag in tagsForCopy)
        {
          StreamBufferAttrDataType attributeType;
          byte[] attributeValue = null;
          IntPtr attPtr = IntPtr.Zero;
          //ushort attributeNameLength = 0;
          short attributeValueLength = 0;

          try
          {
            // Get the lengths of the name and the value, then use them to create buffers to receive them
            _editor.GetAttributeByName(tag, 0, out attributeType, attPtr, ref attributeValueLength);

            //attributeValue = new byte[attributeValueLength];
            attPtr = Marshal.AllocHGlobal(attributeValueLength);

            // Get the name and value
            _editor.GetAttributeByName(tag, 0, out attributeType, attPtr, ref attributeValueLength);

            attributeValue = new byte[attributeValueLength];
            Marshal.Copy(attPtr, attributeValue, 0, attributeValueLength);

            // If we got a name, parse the value and add the metadata item
            if (attributeValue != null && attributeValue.Length > 0)
            {
              object val = ParseAttributeValue(attributeType, attributeValue);
              string key = tag;
              propsRetrieved[key] = new MetadataItem(key, val, attributeType);
            }
          }
          finally
          {
            if (attPtr != IntPtr.Zero)
              Marshal.FreeHGlobal(attPtr);
          }
        }
      }
      else
      {
        // Get the number of attributes
        short attributeCount = 0;
        _editor.GetAttributeCount(0, out attributeCount);

        propsRetrieved.Add("FileName", new MetadataItem("FileName", _path, StreamBufferAttrDataType.String));

        // Get each attribute by index
        for (short i = 0; i < attributeCount; i++)
        {
          IntPtr attPtr = IntPtr.Zero;
          StreamBufferAttrDataType attributeType;
          StringBuilder attributeName = null;
          byte[] attributeValue = null;
          short attributeNameLength = 0;
          short attributeValueLength = 0;

          try
          {
            // Get the lengths of the name and the value, then use them to create buffers to receive them
            //uint reserved = 0;
            _editor.GetAttributeByIndex(i, 0, attributeName, ref attributeNameLength,
                out attributeType, attPtr, ref attributeValueLength);

            attPtr = Marshal.AllocHGlobal(attributeValueLength);

            attributeName = new StringBuilder(attributeNameLength);
            attributeValue = new byte[attributeValueLength];

            // Get the name and value
            _editor.GetAttributeByIndex(i, 0, attributeName, ref attributeNameLength,
                out attributeType, attPtr, ref attributeValueLength);

            Marshal.Copy(attPtr, attributeValue, 0, attributeValueLength);

            // If we got a name, parse the value and add the metadata item
            if (attributeName != null && attributeName.Length > 0)
            {
              object val = ParseAttributeValue(attributeType, attributeValue);
              string key = attributeName.ToString().TrimEnd('\0');
              //if (!tagsForCopy.Contains(key))
              propsRetrieved[key] = new MetadataItem(key, val, attributeType);
            }
          }
          catch
          {
            //swallow error
          }
          finally
          {
            if (attPtr != IntPtr.Zero)
              Marshal.FreeHGlobal(attPtr);
          }
        }
      }
      // Return the parsed items
      return propsRetrieved;
    }

    public override void SetAttributes(System.Collections.IDictionary propsToSet)
    {
      SetAttributes(propsToSet, true);
    }

    /// <summary>Sets the collection of string attributes onto the specified file and stream.</summary>
    /// <param name="propsToSet">The properties to set on the file.</param>
    public override void SetAttributes(System.Collections.IDictionary propsToSet, bool throwComErrors)
    {
      if (_editor == null) throw new ObjectDisposedException(GetType().Name);
      if (propsToSet == null) throw new ArgumentNullException("propsToSet");

      byte[] attributeValueBytes;

      // Add each metadata item
      foreach (DictionaryEntry entry in propsToSet)
      {
        // Get the current item and convert it as appropriate to a byte array
        MetadataItem item = (MetadataItem)entry.Value;
        if (item.Name != "FileName" && TranslateAttributeToByteArray(item, out attributeValueBytes))
        {
          IntPtr setParm = IntPtr.Zero;

          try
          {
            setParm = Marshal.AllocHGlobal(attributeValueBytes.Length);
            Marshal.Copy(attributeValueBytes, 0, setParm, attributeValueBytes.Length);
            // Set the attribute onto the file
            _editor.SetAttribute(0, item.Name, item.Type,
                setParm, (short)attributeValueBytes.Length);
          }
          catch (COMException exc)
          {
            if (throwComErrors)
            {
              // Try to throw a better exception if possible
              uint hr = (uint)Marshal.GetHRForException(exc);
              switch (hr)
              {
                case NS_E_ATTRIBUTE_READ_ONLY:
                  throw new ArgumentException("An attempt was made to add, modify, or delete a read only attribute.", item.Name, exc);
                case NS_E_ATTRIBUTE_NOT_ALLOWED:
                  throw new ArgumentException("An attempt was made to add attribute that is not allowed for the given media type.", item.Name, exc);
                default:
                  throw;
              }
            }
          }
          finally
          {
            if (setParm != IntPtr.Zero)
            {
              Marshal.FreeHGlobal(setParm);
            }
          }
        }
      }
    }


    /// <summary>Release all resources.</summary>
    /// <param name="disposing">Whether this is being called from IDisposable.Dispose.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && _editor != null)
      {
        while (Marshal.ReleaseComObject(_editor) > 0) ;
        _editor = null;
      }
    }
  }
}

