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
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using DirectShowLib.SBE;

namespace MCEBuddy.MetaData
{
  /// <summary>The type of a metadata attribute value.</summary>


  /// <summary>Represents a metadata attribute.</summary>
  public class MetadataItem : ICloneable
  {
    /// <summary>The name of the attribute.</summary>
    private string _name;
    /// <summary>The value of the attribute.</summary>
    private object _value;
    /// <summary>The type of the attribute value.</summary>
    private StreamBufferAttrDataType _type;

    /// <summary>Initializes the metadata item.</summary>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <param name="type">The type of the attribute value.</param>
    public MetadataItem(string name, object value, StreamBufferAttrDataType type)
    {
      Name = name;
      Value = value;
      Type = type;
    }

    /// <summary>Gets or sets the name of the attribute.</summary>
    public string Name { get { return _name; } set { _name = value; } }
    /// <summary>Gets or sets the value of the attribute.</summary>
    public object Value { get { return _value; } set { _value = value; } }
    /// <summary>Gets or sets the type of the attribute value.</summary>
    public StreamBufferAttrDataType Type { get { return _type; } set { _type = value; } }

    /// <summary>Clones the attribute item.</summary>
    /// <returns>A shallow copy of the attribute.</returns>
    public MetadataItem Clone() { return (MetadataItem)MemberwiseClone(); }

    /// <summary>Clones the attribute item.</summary>
    /// <returns>A shallow copy of the attribute.</returns>
    object ICloneable.Clone() { return Clone(); }
  }

  /// <summary>Metadata editor for ASF files, including WMA, WMV, and DVR-MS files.</summary>
  public abstract class MetadataEditor : IDisposable
  {
    /// <summary>The Title attribute contains the title of the content in the file.</summary>
    public const string Title = "Title";
    /// <summary>The WM/SubTitle attribute contains the subtitle of the content.</summary>
    public const string Subtitle = "WM/SubTitle";
    /// <summary>The Description attribute contains a description of the content of the file.</summary>
    public const string Description = "Description";
    /// <summary>The WM/SubTitleDescription attribute contains a description of the content of the file.</summary>
    public const string SubtitleDescription = "WM/SubTitleDescription";
    /// <summary>The WM/MediaCredits attribute contains a list of those involved in the production of the content of the file.</summary>
    public const string Credits = "WM/MediaCredits";
    /// <summary>The Author attribute contains the name of a media artist or actor associated with the content.</summary>
    public const string Author = "Author";
    /// <summary>The WM/AlbumArtist attribute contains the name of the primary artist for the album.</summary>
    public const string AlbumArtist = "WM/AlbumArtist";
    /// <summary>The WM/AlbumTitle attribute contains the title of the album on which the content was originally released.</summary>
    public const string AlbumTitle = "WM/AlbumTitle";
    /// <summary>The WM/MediaStationName attribute contains the title of the station that aired the content was originally released.</summary>
    public const string StationName = "WM/MediaStationName";
    /// <summary>The WM/Composer attribute contains the name of the music composer.</summary>
    public const string Composer = "WM/Composer";
    /// <summary>The WM/ParentalRating attribute contains the parental rating of the content.</summary>
    public const string ParentalRating = "WM/ParentalRating";
    /// <summary>The WM/ParentalRating attribute contains the reason for the parental rating of the content.</summary>
    public const string ParentalRatingReason = "WM/ParentalRatingReason";
    /// <summary>The WM/MediaOriginalBroadcastDateTime attribute contains the original broadcast date and time of the content.</summary>
    public const string MediaOriginalBroadcastDateTime = "WM/MediaOriginalBroadcastDateTime";
    /// <summary>The WM/Mood attribute contains a category name for the mood of the content.</summary>
    public const string Mood = "WM/Mood";
    /// <summary>The WM/Genre attribute contains the genre of the content.</summary>
    public const string Genre = "WM/Genre";
    /// <summary>The WM/Language attribute contains the language of the stream.</summary>
    public const string Language = "WM/Language";
    /// <summary>The WM/Lyrics attribute contains the lyrics as a simple String.</summary>
    public const string Lyrics = "WM/Lyrics";
    /// <summary>The WM/Lyrics_Synchronised attribute contains lyrics synchronized to times in the file.</summary>
    public const string SynchronizedLyrics = "WM/Lyrics_Synchronised";
    /// <summary>The Duration attribute contains the length of the file in hundreds of nanoseconds.</summary>
    public const string Duration = "Duration";
    /// <summary>The WM/ContentGroupDescription attribute contains a content group description.</summary>
    public const string ContentGroupDescription = "WM/ContentGroupDescription";
    /// <summary>The WM/PartOfSet attribute contains the set grouping for this content.</summary>
    public const string PartOfSet = "WM/PartOfSet";

    /// <summary>Path to the file whose metadata is being edited.</summary>
    protected string _path;

    /// <summary>Initialize the editor.</summary>
    protected MetadataEditor(string path)
    {
      if (path == null) throw new ArgumentNullException("path");
      _path = path;
    }

    /// <summary>Releases all of the resources for the editor.</summary>
    void IDisposable.Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>Releases all of the resources for the editor.</summary>
    /// <param name="disposing">Whether the object is currently being disposed (rather than finalized).</param>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>Releases all of the resources for the editor.</summary>
    ~MetadataEditor() { Dispose(false); }

    /// <summary>Gets the path to the file being edited.</summary>
    public string Path { get { return _path; } }

    /// <summary>Retrieves the string value of a metadata item.</summary>
    /// <param name="items">The collection of metadata items containing the item to be retrieved.</param>
    /// <param name="name">The name of the attribute value to be retrieved.</param>
    /// <returns>The attribute value as a String.</returns>
    public static string GetMetadataItemAsString(IDictionary items, string name)
    {
      MetadataItem item = (MetadataItem)items[name];
      if (item == null || item.Value == null) return String.Empty;
      return item.Value.ToString().Trim();
    }

    /// <summary>Sets the value of a string attribute.</summary>
    /// <param name="items">The metadata items collection.</param>
    /// <param name="name">The name of the attribute.</param>
    /// <param name="value">The new value of the attribute.</param>
    public static void SetMetadataItemAsString(IDictionary items, string name, string value)
    {
      items[name] = new MetadataItem(name, value, StreamBufferAttrDataType.String);
    }

    /// <summary>Copies a metadata item from one collection under one name to another collection under another name.</summary>
    /// <param name="source">The source collection.</param>
    /// <param name="sourceName">The source name.</param>
    /// <param name="destination">The destination collection.</param>
    /// <param name="destinationName">The destination name.</param>
    private static void CopyMetadataItem(IDictionary source, string sourceName, IDictionary destination, string destinationName)
    {
      // Gets the source item
      MetadataItem item = (MetadataItem)source[sourceName];

      // Clone the item and copy it to the destination
      if (item != null)
      {
        item = item.Clone();
        item.Name = destinationName;
        destination[destinationName] = item;
      }
    }
    public virtual IDictionary CopyMetadataFromSource(MetadataEditor source, bool augmentMetadata)
    {
      return CopyMetadataFromSource(source, augmentMetadata, false);
    }

    /// <summary>Migrate the metadata from one file to another.</summary>
    /// <param name="source">The source editor.></param>
    /// <param name="augmentMetadata">Whether to augment the metadata for WMP and MCE.</param>
    /// <returns>The migrated collection.</returns>
    public virtual IDictionary CopyMetadataFromSource(MetadataEditor source, bool augmentMetadata, bool throwComErrors)
    {
      // Get the source metadata
      IDictionary metadata = source.GetAttributes(true);

      //using (StreamWriter sw = File.CreateText(@"C:\temp\metadata.txt"))
      //{
      //    Hashtable ht = (Hashtable)metadata;
      //    foreach (string k in ht.Keys)
      //        sw.WriteLine(k);
      //}


      // Augment the metadata to provide a better experience in both WMP and MCE
      if (augmentMetadata)
      {
        string title = GetMetadataItemAsString(metadata, MetadataEditor.Title);
        string subTitle = GetMetadataItemAsString(metadata, MetadataEditor.Subtitle);
        if (!title.EndsWith(subTitle))
        {
          title += (title.Length > 0 && subTitle.Length > 0 ? " - " : String.Empty) + subTitle;
        }
        SetMetadataItemAsString(metadata, MetadataEditor.Title, title);

        CopyMetadataItem(metadata, MetadataEditor.SubtitleDescription, metadata, MetadataEditor.Description);
        CopyMetadataItem(metadata, MetadataEditor.Credits, metadata, MetadataEditor.Author);
        CopyMetadataItem(metadata, MetadataEditor.Title, metadata, MetadataEditor.AlbumTitle);
        CopyMetadataItem(metadata, MetadataEditor.StationName, metadata, MetadataEditor.Composer);
        CopyMetadataItem(metadata, MetadataEditor.ParentalRating, metadata, MetadataEditor.ContentGroupDescription);
        CopyMetadataItem(metadata, MetadataEditor.MediaOriginalBroadcastDateTime, metadata, MetadataEditor.PartOfSet);
        CopyMetadataItem(metadata, MetadataEditor.ParentalRating, metadata, MetadataEditor.Mood);
      }

      // Set the metadata
      SetAttributes(metadata, throwComErrors);
      return metadata;
    }

    /// <summary>Converts a value to the target type and gets its byte representation.</summary>
    /// <param name="item">The item whose value is to be translated.</param>
    /// <param name="valueData">The output byte array.</param>
    protected static bool TranslateAttributeToByteArray(MetadataItem item, out byte[] valueData)
    {
      int valueLength;
      switch (item.Type)
      {
        case StreamBufferAttrDataType.DWord:
          valueData = BitConverter.GetBytes((int)item.Value);
          return true;

        case StreamBufferAttrDataType.Word:
          valueData = BitConverter.GetBytes((short)item.Value);
          return true;

        case StreamBufferAttrDataType.QWord:
          valueData = BitConverter.GetBytes((long)item.Value);
          return true;

        case StreamBufferAttrDataType.Bool:
          valueData = BitConverter.GetBytes(((bool)item.Value) ? 1 : 0);
          return true;

        case StreamBufferAttrDataType.Guid:
          valueData = ((Guid)item.Value).ToByteArray();
          return true;

        case StreamBufferAttrDataType.String:
          string strValue = item.Value.ToString();
          valueLength = (strValue.Length + 1) * 2; // plus 1 for null-term, times 2 for unicode
          valueData = new byte[valueLength];
          Buffer.BlockCopy(strValue.ToCharArray(), 0, valueData, 0, strValue.Length * 2);
          valueData[valueLength - 2] = 0;
          valueData[valueLength - 1] = 0;
          return true;

        default:
          valueData = null;
          return false;
      }
    }

    /// <summary>Sets the collection of string attributes onto the specified file and stream.</summary>
    /// <param name="propsToSet">The properties to set on the file.</param>
    public abstract void SetAttributes(IDictionary propsToSet);

    public abstract void SetAttributes(IDictionary propsToSet, bool throwComErrors);

    /// <summary>An attempt was made to add, modify, or delete a read only attribute.</summary>
    protected const uint NS_E_ATTRIBUTE_READ_ONLY = 0xC00D0BD6;
    /// <summary>An attempt was made to add attribute that is not allowed for the given media type.</summary>
    protected const uint NS_E_ATTRIBUTE_NOT_ALLOWED = 0xC00D0BD7;


    /// <summary>Gets the value of the specified attribute.</summary>
    /// <param name="itemType">The type of the attribute.</param>
    /// <param name="valueData">The byte array to be parsed.</param>
    protected static object ParseAttributeValue(StreamBufferAttrDataType itemType, byte[] valueData)
    {
      if (!Enum.IsDefined(typeof(StreamBufferAttrDataType), itemType))
        throw new ArgumentOutOfRangeException("itemType");
      if (valueData == null) throw new ArgumentNullException("valueData");

      // Convert the attribute value to a byte array based on the item type.
      switch (itemType)
      {
        case StreamBufferAttrDataType.String:
          StringBuilder sb = new StringBuilder(valueData.Length);
          for (int i = 0; i < valueData.Length - 2; i += 2)
          {
            sb.Append(Convert.ToString(BitConverter.ToChar(valueData, i), System.Globalization.CultureInfo.InvariantCulture));
          }
          string result = sb.ToString();
          if (result.EndsWith("\\0")) result = result.Substring(0, result.Length - 2);
          return result;
        case StreamBufferAttrDataType.Bool: return BitConverter.ToBoolean(valueData, 0);
        case StreamBufferAttrDataType.DWord: return BitConverter.ToInt32(valueData, 0);
        case StreamBufferAttrDataType.QWord: return BitConverter.ToInt64(valueData, 0);
        case StreamBufferAttrDataType.Word: return BitConverter.ToInt16(valueData, 0);
        case StreamBufferAttrDataType.Guid: return new Guid(valueData);
        case StreamBufferAttrDataType.Binary: return valueData;
        default: throw new ArgumentOutOfRangeException("itemType");
      }
    }

    /// <summary>Gets all of the attributes on a file.</summary>
    /// <returns>A collection of the attributes from the file.</returns>
    public abstract IDictionary GetAttributes();

    /// <summary>Gets all of the attributes on a file.</summary>
    /// <returns>A collection of the attributes from the file.</returns>
    public abstract IDictionary GetAttributes(bool forCopy);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    protected struct WMPicture
    {
      public IntPtr pwszMIMEType;
      public byte bPictureType;
      public IntPtr pwszDescription;
      [MarshalAs(UnmanagedType.U4)]
      public int dwDataLen;
      public IntPtr pbData;
    }
  }
}

