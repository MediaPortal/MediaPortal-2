#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Drawing;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Utilities.Graphics;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// <see cref="FanArtImage"/> represents a fanart image that can be transmitted from server to clients using UPnP.
  /// It supports resizing of existing images to different resolutions. The resized images will be cached on server data folder
  /// (<see cref="CACHE_PATH"/>), so they can be reused later.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Note: This class is serialized/deserialized by the <see cref="XmlSerializer"/>.
  /// If changed, this has to be taken into consideration.
  /// </para>
  /// </remarks>
  public class FanArtImage
  {
    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Thumbs\FanArt");

    // We could use some cache for this instance, if we would have one...
    protected static XmlSerializer _xmlSerializer; // Lazy initialized

    public FanArtImage()
    { }

    public FanArtImage(string name, byte[] binaryData)
    {
      Name = name;
      BinaryData = binaryData;
    }

    /// <summary>
    /// Returns the name of this FanArtImage.
    /// </summary>
    [XmlAttribute("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Contains the binary data of the Image.
    /// </summary>
    [XmlElement("BinaryData")]
    public byte[] BinaryData { get; set; }

    /// <summary>
    /// Serializes this user profile instance to XML.
    /// </summary>
    /// <returns>String containing an XML fragment with this instance's data.</returns>
    public string Serialize()
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      StringBuilder sb = new StringBuilder(); // Will contain the data, formatted as XML
      using (XmlWriter writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
        xs.Serialize(writer, this);
      return sb.ToString();
    }

    /// <summary>
    /// Serializes this user profile instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a user profile instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as FanArtImage;
    }

    /// <summary>
    /// Deserializes a user profile instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static FanArtImage Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as FanArtImage;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(FanArtImage)));
    }

    /// <summary>
    /// Loads an image from filesystem an returns a new <see cref="FanArtImage"/>.
    /// </summary>
    /// <param name="fileName">File name to load</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <returns>FanArtImage or <c>null</c>.</returns>
    public static FanArtImage FromFile(string fileName, int maxWidth, int maxHeight)
    {
      if (string.IsNullOrEmpty(fileName))
        return null;

      fileName = ResizeImage(fileName, maxWidth, maxHeight);
      FileInfo fileInfo = new FileInfo(fileName);
      if (!fileInfo.Exists)
        return null;

      try
      {
        byte[] binary = new byte[fileInfo.Length];
        using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
        using (BinaryReader binaryReader = new BinaryReader(fileStream))
          binaryReader.Read(binary, 0, binary.Length);

        return new FanArtImage(fileInfo.Name, binary);
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// Resizes an image to given size. The resized image will be saved to cache, so it can be reused later. Images that
    /// are smaller than the requested target size will not be scaled up, but returned in original size.
    /// </summary>
    /// <param name="originalFile">Image to resize</param>
    /// <param name="maxWidth">Maximum image width</param>
    /// <param name="maxHeight">Maximum image height</param>
    /// <returns></returns>
    protected static string ResizeImage(string originalFile, int maxWidth, int maxHeight)
    {
      if (maxWidth == 0 || maxHeight == 0)
        return originalFile;

      if (!Directory.Exists(CACHE_PATH))
        Directory.CreateDirectory(CACHE_PATH);

      string thumbFileName = Path.Combine(CACHE_PATH, string.Format("th_{0}x{1}_{2}", maxWidth, maxHeight, Path.GetFileName(originalFile)));
      if (File.Exists(thumbFileName))
        return thumbFileName;

      try
      {
        Image fullsizeImage = Image.FromFile(originalFile);
        if (fullsizeImage.Width <= maxWidth)
          maxWidth = fullsizeImage.Width;

        int newHeight = fullsizeImage.Height * maxWidth / fullsizeImage.Width;
        if (newHeight > maxHeight)
        {
          // Resize with height instead
          maxWidth = fullsizeImage.Width * maxHeight / fullsizeImage.Height;
          newHeight = maxHeight;
        }

        using (fullsizeImage)
        using (Image newImage = ImageUtilities.ResizeImage(fullsizeImage, maxWidth, newHeight))
          ImageUtilities.SaveJpeg(thumbFileName, newImage, 95);

        return thumbFileName;
      }
      catch (Exception)
      {
        return originalFile;
      }
    }
  }
}
