#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core.MediaManagement.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors.PictureMetadataExtractor.ExifMetaInfo
{
  /// <summary>
  /// ExifMetaInfo class is used to extract EXIF information from images.
  /// </summary>
  public class ExifMetaInfo : IDisposable
  {
    #region Variables

    private uint _pixXDim;
    private uint _pixYDim;
    private string _equipMake = string.Empty;
    private string _equipModel = string.Empty;
    private string _imageDescription = string.Empty;
    private string _copyright = string.Empty;
    private string _dtOrig = string.Empty;
    private string _dtDigitized = string.Empty;
    private uint _orientation;
    private Fraction _focalLength = new Fraction(0, 0);
    private Fraction _fNumber = new Fraction(0, 0);
    private Fraction _exposureTime = new Fraction(0, 0);
    private Fraction _exposureBias = new Fraction(0, 0);
    private string _isoSpeed = string.Empty;
    private bool _flashFired;
    private string _flashMode = string.Empty;
    private MeteringMode _meteringMode = MeteringMode.Unknown;
    private Bitmap _thumbImage;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the width of image.
    /// </summary>
    public uint PixXDim { get { return _pixXDim; } }
    /// <summary>
    /// Returns the height of image.
    /// </summary>
    public uint PixYDim { get { return _pixYDim; } }
    /// <summary>
    /// Returns the make of camera.
    /// </summary>
    public string EquipMake { get { return _equipMake; } }
    /// <summary>
    /// Returns the model of camera.
    /// </summary>
    public string EquipModel { get { return _equipModel; } }
    /// <summary>
    /// Returns the description of image.
    /// </summary>
    public string ImageDescription { get { return _imageDescription; } }
    /// <summary>
    /// Returns the copyright of image.
    /// </summary>
    public string Copyright { get { return _copyright; } }
    /// <summary>
    /// Returns the rotation of image.
    /// </summary>
    public uint Orientation { get { return _orientation; } }
    /// <summary>
    /// Returns the rotation of image.
    /// </summary>
    public Orientation OrientationType { get { return (Orientation) _orientation; } }

    ///<summary>
    ///Date and time when the original image data was generated. For a DSC, the date and time when the picture was taken. 
    ///</summary>
    public DateTime OriginalDate { get { return ExifDtToDateTime(_dtOrig); } }

    ///<summary>
    ///Date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then DateTimeOriginal and DateTimeDigitized will have the same contents.
    ///</summary>
    public DateTime DigitizedDate { get { return ExifDtToDateTime(_dtDigitized); } }

    ///<summary>
    ///Actual focal length, in millimeters, of the lens. Conversion is not made to the focal length of a 35 millimeter film camera.
    ///</summary>						
    public Fraction FocalLength { get { return _focalLength; } }

    ///<summary>
    ///F number.
    ///</summary>						
    public Fraction FNumber { get { return _fNumber; } }

    ///<summary>
    ///Exposure Time.
    ///</summary>						
    public Fraction ExposureTime { get { return _exposureTime; } }

    /// <summary>
    /// Exposure Bias.
    /// </summary>
    public Fraction ExposureBias { get { return _exposureBias; } }

    /// <summary>
    /// ISO speed rating.
    /// </summary>
    public string ISOSpeed { get { return _isoSpeed; } }

    /// <summary>
    /// Flash mode.
    /// </summary>
    public string FlashMode { get { return _flashMode; } }

    /// <summary>
    /// True if flash fired.
    /// </summary>
    public bool FlashFired { get { return _flashFired; } }

    /// <summary>
    /// Metering mode.
    /// </summary>
    public MeteringMode MeteringMode { get { return _meteringMode; } }

    /// <summary>
    /// Embedded thumbnail image.
    /// </summary>
    public Bitmap ThumbImage { get { return _thumbImage; } }

    #endregion

    #region Constructor

    /// <summary>
    /// Creates an ExifMetaInfo class by reading a local file.
    /// </summary>
    /// <param name="filename"></param>
    public ExifMetaInfo(string filename)
    {
      ReadMetaInfo(filename);
    }

    /// <summary>
    /// Creates an ExifMetaInfo class by reading a Stream from IResourceAccessor.OpenRead().
    /// </summary>
    /// <param name="mediaItemAccessor"></param>
    public ExifMetaInfo(IResourceAccessor mediaItemAccessor)
    {
      ReadMetaInfo(mediaItemAccessor);
    }

    /// <summary>
    /// Creates an ExifMetaInfo class by reading a Stream.
    /// </summary>
    /// <param name="mediaStream"></param>
    public ExifMetaInfo(Stream mediaStream)
    {
      ReadMetaInfo(mediaStream);
    }

    #endregion


    //The format is YYYY:MM:DD HH:MM:SS with time shown in 24-hour format and the date and time separated by one blank character (0x2000). The character string length is 20 bytes including the NULL terminator. When the field is empty, it is treated as unknown.
    private static DateTime ExifDtToDateTime(string exifDt)
    {
      try
      {
        if (String.IsNullOrEmpty(exifDt)) return DateTime.MinValue;
        exifDt = exifDt.Replace(' ', ':');
        string[] ymdhms = exifDt.Split(':');
        int years = int.Parse(ymdhms[0]);
        int months = int.Parse(ymdhms[1]);
        int days = int.Parse(ymdhms[2]);
        int hours = int.Parse(ymdhms[3]);
        int minutes = int.Parse(ymdhms[4]);
        int seconds = ymdhms.Length > 5 ? int.Parse(ymdhms[5]) : 0;
        return new DateTime(years, months, days, hours, minutes, seconds);
      }
      catch (Exception)
      {
        return DateTime.MinValue;
      }

    }

    private void ReadMetaInfo(IResourceAccessor mediaItemAccessor)
    {
      using (Stream mediaStream = mediaItemAccessor.OpenRead())
        ReadMetaInfo(mediaStream);
    }

    private void ReadMetaInfo(string filename)
    {
      using (FileStream fs = File.OpenRead(filename))
        ReadMetaInfo(fs);
    }

    private void ReadMetaInfo(Stream mediaStream)
    {
      long exifOffsetBase = 0;

      MemoryStream ms = new MemoryStream();
      BinaryReader br = new BinaryReader(mediaStream);
      bool isBigEndian = false;

      try
      {
        ulong tempOffset = 0;
        ulong storedOffset = 0;

        long thumbSearchPos = mediaStream.Position;

        byte[] readBuffer = br.ReadBytes(2);

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xD8)) // search for the SOI (FF D8)
        {
          readBuffer = br.ReadBytes(2);
          if (mediaStream.Position >= thumbSearchPos + (1 * 1024)) return; // exit the routine via the error handler if no thumb was found
        }

        bool exifInformationFound = true;

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xE1)) // search for the APP1 (Exif Data) (FF E1)
        {
          readBuffer = br.ReadBytes(2);
          if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD9) // EOI reached
          {
            throw (new Exception()); // exit the routine via the error handler
          }
          if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD8) // SOI reached
          {
            exifInformationFound = false;
            mediaStream.Seek(-2, SeekOrigin.Current);
            tempOffset = 0;
            break;
          }
          if (mediaStream.Position >= thumbSearchPos + (2 * 1024)) return; // exit the routine via the error handler if no thumb was found
        }

        if (exifInformationFound)
        {
          while (!(readBuffer[0] == 0x49 && readBuffer[1] == 0x49) && !(readBuffer[0] == 0x4D && readBuffer[1] == 0x4D)) // search for the byte order 
          {
            readBuffer = br.ReadBytes(2);
            if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD9) // EOI reached
            {
              throw (new Exception()); // exit the routine via the error handler
            }
            if (mediaStream.Position >= thumbSearchPos + (5 * 1024)) return; // exit the routine via the error handler if no thumb was found
          }

          if (readBuffer[0] == 0x4D && readBuffer[1] == 0x4D) // is big endian?
          {
            isBigEndian = true;
          }

          exifOffsetBase = mediaStream.Position - 2;

          br.ReadBytes(2); // read 0x42

          tempOffset = BitConverter.ToUInt32(ReadEndianBytes(br, isBigEndian, 4), 0); // read the offset of the exif data
        }

        while (tempOffset != 0) // cycle through the IFD's
        {
          mediaStream.Seek(exifOffsetBase + (long) tempOffset, SeekOrigin.Begin); // jump to the begin of the exif data
          int tmpCount = BitConverter.ToInt16(ReadEndianBytes(br, isBigEndian, 2), 0); // Read the count of entries

          if (tmpCount > 250) break; //skip corrupted exif info

          //mediaStream.Seek(tmpCount * 12, SeekOrigin.Current); // skip the entries

          ulong nextIfdOffset = 0; // If the Exif information is spread over more than one IFD, this value stores the offset to the next block

          for (int i = 0; i < tmpCount; i++)
          {
            uint entryId = BitConverter.ToUInt16(ReadEndianBytes(br, isBigEndian, 2), 0); // Read the entry ID
            uint entryType = BitConverter.ToUInt16(ReadEndianBytes(br, isBigEndian, 2), 0); // Read the entry type
            uint entryCount = BitConverter.ToUInt32(ReadEndianBytes(br, isBigEndian, 4), 0); // Read the entry count
            byte[] entryData = ReadEndianBytes(br, isBigEndian, 4); // Read the data value

            if (entryId == 0x8769 || (_equipMake.ToLower() == "canon" && entryId == 0x927C)) // is this the link to the next IFD block? Or the a makernote
            {
              nextIfdOffset = BitConverter.ToUInt32(entryData, 0); // store the offset of the next IFD block
            }
            else
            {
              int lenBase;
              switch ((PropertyTagType) entryType)
              {
                case PropertyTagType.Rational:
                  lenBase = 8;
                  break;
                case PropertyTagType.SRational:
                  lenBase = 8;
                  break;
                case PropertyTagType.Short:
                  lenBase = 2;
                  break;
                case PropertyTagType.Long:
                  lenBase = 4;
                  break;
                case PropertyTagType.SLONG:
                  lenBase = 4;
                  break;
                default:
                  lenBase = 1;
                  break;
              }

              if ((entryCount * lenBase) > 4)
              {
                ulong entryOffset = BitConverter.ToUInt32(entryData, 0); // Read the entry offset
                long curPos = mediaStream.Position; // store the current position in the image file
                mediaStream.Seek(exifOffsetBase + (long) entryOffset, SeekOrigin.Begin); // jump to the begin of the entry data
                entryData = br.ReadBytes((int) entryCount * lenBase); // read the entry
                mediaStream.Seek(curPos, SeekOrigin.Begin); // jump back to read the next entrty
              }

              if (_equipMake.ToLower() == "canon" && entryId == 0x1 && entryType == 0x3)
              {
                AnalyseCanonMakerNoteBlock1(entryData);
              }

              switch ((PropertyTagType) entryType)
              {
                case PropertyTagType.Short:
                  {
                    if (isBigEndian) // corrent the bit order
                    {
                      Array.Reverse(entryData);
                      Array.Reverse(entryData, 0, 2);
                      Array.Reverse(entryData, 2, 2);
                    }
                  }
                  break;

                case PropertyTagType.SRational:
                case PropertyTagType.Rational:
                  {
                    if (isBigEndian)
                    {
                      Array.Reverse(entryData, 0, 4);
                      Array.Reverse(entryData, 4, 4);
                    }
                  }
                  break;
              }

              FillProperty(entryId, entryType, entryData); // convert the byte and fill the property
            }
          }

          tempOffset = BitConverter.ToUInt32(ReadEndianBytes(br, isBigEndian, 4), 0); // read the offset of the next data field
          if (storedOffset != 0)
          {
            tempOffset = storedOffset;
            storedOffset = 0;
          }
          if (nextIfdOffset != 0)
          {
            storedOffset = tempOffset;
            tempOffset = nextIfdOffset;
            nextIfdOffset = 0;
          }
        }

        //readBuffer = br.ReadBytes(4);
        //mediaStream.Seek(-4, SeekOrigin.Current);

        //// does the thumbnail contain exif information? (is stupid and out of the standard but ...)
        //if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD8 && readBuffer[2] == 0xFF) // && readBuffer[3] == 0xE1
        //{
        //  // potential handling...
        //}
        //else
        //{
        //  //readBuffer = br.ReadBytes(8); // read XResolution Value
        //  //readBuffer = br.ReadBytes(8); // read YResolution Value
        //}

        //mediaStream.Seek(2, SeekOrigin.Begin); //jump back to the begin

        thumbSearchPos = mediaStream.Position;
        readBuffer = br.ReadBytes(2); // read begin of image

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xD8)) // search for the SOI (FF D8)
        {
          readBuffer = br.ReadBytes(2);
          if (mediaStream.Position >= thumbSearchPos + (10 * 1024)) return; // exit the routine via the error handler if no thumb was found
        }

        if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD8) // If the next two bytes descripe a SOI
        {
          ms.SetLength(0);
          ms.Write(readBuffer, 0, 2);
          int prevByte = 0;

          while (!(prevByte == 0xFF && readBuffer[0] == 0xD9)) // until a EOI is found
          {
            prevByte = readBuffer[0];
            readBuffer = br.ReadBytes(1);
            ms.Write(readBuffer, 0, 1);
            if (mediaStream.Position >= thumbSearchPos + (20 * 1024)) return; // exit the routine via the error handler if no thumb was found 
          }
          ms.Seek(0, SeekOrigin.Begin);

          _thumbImage = new Bitmap(ms);
        }
      }
      catch
      { }
      finally
      {
        br.Close();
        mediaStream.Close();
        ms.Close();
      }
    }

    private void AnalyseCanonMakerNoteBlock1(byte[] data)
    {
      // http://www.ozhiker.com/electronics/pjmt/jpeg_info/canon_mn.html
      uint tmpISO = data[(16 * 2)];
      if (tmpISO != 0)
      {
        switch (tmpISO)
        {
          case 15: _isoSpeed = "Auto";
            break;
          case 16: _isoSpeed = "50";
            break;
          case 17: _isoSpeed = "100";
            break;
          case 18: _isoSpeed = "200";
            break;
          case 19: _isoSpeed = "400";
            break;
        }
      }
    }

    private void FillFlashModeResult(uint data)
    {
      switch (data)
      {
        case 0x0: _flashMode = "Flash did not fire.";
          break;
        case 0x1: _flashMode = "Flash fired.";
          break;
        case 0x5: _flashMode = "Strobe return light not detected.";
          break;
        case 0x7: _flashMode = "Strobe return light detected.";
          break;
        case 0x9: _flashMode = "Flash fired, compulsory flash mode";
          break;
        case 0xD: _flashMode = "Flash fired, compulsory flash mode, return light not detected";
          break;
        case 0xF: _flashMode = "Flash fired, compulsory flash mode, return light detected";
          break;
        case 0x10: _flashMode = "Flash did not fire, compulsory flash mode";
          break;
        case 0x18: _flashMode = "Flash did not fire, auto mode";
          break;
        case 0x19: _flashMode = "Flash fired, auto mode";
          break;
        case 0x1d: _flashMode = "Flash fired, auto mode, return light not detected";
          break;
        case 0x1f: _flashMode = "Flash fired, auto mode, return light detected";
          break;
        case 0x20: _flashMode = "No flash function";
          break;
        case 0x41: _flashMode = "Flash fired, red-eye reduction mode";
          break;
        case 0x45: _flashMode = "Flash fired, red-eye reduction mode, return light not detected";
          break;
        case 0x47: _flashMode = "Flash fired, red-eye reduction mode, return light detected";
          break;
        case 0x49: _flashMode = "Flash fired, compulsory flash mode, red-eye reduction mode";
          break;
        case 0x4d: _flashMode = "Flash fired, compulsory flash mode, red-eye reduction mode, return light not detected";
          break;
        case 0x4f: _flashMode = "Flash fired, compulsory flash mode, red-eye reduction mode, return light detected";
          break;
        case 0x59: _flashMode = "Flash fired, auto mode, red-eye reduction mode";
          break;
        case 0x5d: _flashMode = "Flash fired, auto mode, return light not detected, red-eye reduction mode";
          break;
        case 0x5f: _flashMode = "Flash fired, auto mode, return light detected, red-eye reduction mode";
          break;
      }
    }

    private static byte[] ReadEndianBytes(BinaryReader br, bool readBigEndian, int countOfBytes)
    {
      byte[] tmpBuffer = br.ReadBytes(countOfBytes);
      if (readBigEndian) Array.Reverse(tmpBuffer);
      return tmpBuffer;
    }

    private void FillProperty(uint entryID, uint entryType, byte[] entryData)
    {
      switch ((PropertyTagId) entryID)
      {
        case PropertyTagId.EquipMake: _equipMake = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.EquipModel: _equipModel = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ImageDescription: _imageDescription = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.Copyright: _copyright = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifDTOrig: _dtOrig = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifDTDigitized: _dtDigitized = (string) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifFocalLength: _focalLength = (Fraction) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifFNumber: _fNumber = (Fraction) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifExposureBias: _exposureBias = (Fraction) PropertyTag.getValue(entryType, entryData);
          break;
        case PropertyTagId.ExifExposureTime:
          {
            _exposureTime = (Fraction) PropertyTag.getValue(entryType, entryData);
            if (_exposureTime.Numerator > 1)
            {
              _exposureTime.Denumerator /= _exposureTime.Numerator;
              _exposureTime.Numerator /= _exposureTime.Numerator;
            }
          }
          break;
        case PropertyTagId.ExifISOSpeed:
          {
            object tmpValue = PropertyTag.getValue(entryType, entryData);
            _isoSpeed = tmpValue.GetType().ToString().Equals("System.UInt16")
                          ? ((uint) (ushort) tmpValue).ToString()
                          : ((uint) tmpValue).ToString();
          }
          break;
        case PropertyTagId.Orientation:
          {
            object tmpValue = PropertyTag.getValue(entryType, entryData);
            _orientation = tmpValue.GetType().ToString().Equals("System.UInt16")
                             ? (ushort) tmpValue
                             : (uint) tmpValue;
          }
          break;
        case PropertyTagId.ExifPixXDim:
          {
            object tmpValue = PropertyTag.getValue(entryType, entryData);
            _pixXDim = tmpValue.GetType().ToString().Equals("System.UInt16")
                         ? (ushort) tmpValue
                         : (uint) tmpValue;
          }
          break;
        case PropertyTagId.ExifPixYDim:
          {
            object tmpValue = PropertyTag.getValue(entryType, entryData);
            _pixYDim = tmpValue.GetType().ToString().Equals("System.UInt16")
                         ? (ushort) tmpValue
                         : (uint) tmpValue;
          }
          break;
        case PropertyTagId.ExifFlash:
          {
            //object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            uint tmpValue = BitConverter.ToUInt16(entryData, 0);
            if ((tmpValue & 0x1) == 1)
            {
              _flashFired = true;
              FillFlashModeResult(tmpValue);
            }
            else
            {
              _flashFired = false;
              FillFlashModeResult(tmpValue);
            }
          }
          break;
        case PropertyTagId.ExifMeteringMode:
          {
            object tmpValue = PropertyTag.getValue(entryType, entryData);
            _meteringMode = tmpValue.GetType().ToString().Equals("System.UInt16")
                              ? (MeteringMode) (ushort) tmpValue
                              : (MeteringMode) (uint) tmpValue;
          }
          break;
      }
    }

    public void Dispose()
    {
      try
      {
        if (_thumbImage != null)
          _thumbImage.Dispose();
        _thumbImage = null;
      }
      catch (NullReferenceException)
      { }
    }
  }
}
