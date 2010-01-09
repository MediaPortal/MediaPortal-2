#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using System.Text;
using System.Drawing;
using System.IO;

namespace Media.Importers.PictureImporter
{
  public class ExifMetaInfo : IDisposable
  {
    #region privats

    private uint _PixXDim;
    private uint _PixYDim;
    private string _EquipMake = "";
    private string _EquipModel = "";
    private string _ImageDescription = "";
    private string _Copyright = "";
    private string _DTOrig = "";
    private string _DTDigitized = "";
    private uint _Orientation;
    private Fraction _FocalLength = new Fraction(0, 0);
    private Fraction _FNumber = new Fraction(0, 0);
    private Fraction _ExposureTime = new Fraction(0, 0);
    private Fraction _ExposureBias = new Fraction(0, 0);
    private string _ISOSpeed = "";
    private bool _FlashFired;
    private string _FlashMode = "";
    private MeteringMode _MeteringMode = MeteringMode.Unknown;
    private Bitmap _ThumbImage;

    #endregion

    #region Properties

    public uint PixXDim { get { return _PixXDim; } }
    public uint PixYDim { get { return _PixYDim; } }
    public string EquipMake { get { return _EquipMake; } }
    public string EquipModel { get { return _EquipModel; } }
    public string ImageDescription { get { return _ImageDescription; } }
    public string Copyright { get { return _Copyright; } }
    public uint Orientation { get { return _Orientation; } }
    public Orientation OrientationType { get { return (Orientation)_Orientation; } }

    ///<summary>
    ///Date and time when the original image data was generated. For a DSC, the date and time when the picture was taken. 
    ///</summary>
    public DateTime DTOrig { get { return ExifDTToDateTime(_DTOrig); } }

    ///<summary>
    ///Date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then DateTimeOriginal and DateTimeDigitized will have the same contents.
    ///</summary>
    public DateTime DTDigitized { get { return ExifDTToDateTime(_DTDigitized); } }

    ///<summary>
    ///Actual focal length, in millimeters, of the lens. Conversion is not made to the focal length of a 35 millimeter film camera.
    ///</summary>						
    public Fraction FocalLength { get { return _FocalLength; } }

    ///<summary>
    ///F number.
    ///</summary>						
    public Fraction FNumber { get { return _FNumber; } }

    ///<summary>
    ///Exposure Time
    ///</summary>						
    public Fraction ExposureTime { get { return _ExposureTime; } }

    public Fraction ExposureBias { get { return _ExposureBias; } }

    public string ISOSpeed { get { return _ISOSpeed; } }

    public string FlashMode { get { return _FlashMode; } }

    public bool FlashFired { get { return _FlashFired; } }

    public MeteringMode MeteringMode { get { return _MeteringMode; } }

    public Bitmap ThumbImage { get { return _ThumbImage; } }


    #endregion

    public ExifMetaInfo(string filename)
    {
      ReadMetaInfo(filename);
    }

    public static int EXIFOrientationToRotation(int orientation)
    {
      //Log.Info("Orientation: {0}", orientation);

      if (orientation == 6)
        return 1;

      if (orientation == 3)
        return 2;

      if (orientation == 8)
        return 3;

      return 0;
    }

    //The format is YYYY:MM:DD HH:MM:SS with time shown in 24-hour format and the date and time separated by one blank character (0x2000). The character string length is 20 bytes including the NULL terminator. When the field is empty, it is treated as unknown.
    private static DateTime ExifDTToDateTime(string exifDT)
    {
      try
      {
        if (String.IsNullOrEmpty(exifDT)) return DateTime.MinValue;
        exifDT = exifDT.Replace(' ', ':');
        string[] ymdhms = exifDT.Split(':');
        int years = int.Parse(ymdhms[0]);
        int months = int.Parse(ymdhms[1]);
        int days = int.Parse(ymdhms[2]);
        int hours = int.Parse(ymdhms[3]);
        int minutes = int.Parse(ymdhms[4]);
        int seconds = int.Parse(ymdhms[5]);
        return new DateTime(years, months, days, hours, minutes, seconds);
      }
      catch (Exception)
      {
        return DateTime.MinValue;
      }

    }

    private void ReadMetaInfo(string filename)
    {
      long ExifOffsetBase = 0;

      MemoryStream ms = new MemoryStream();
      FileStream fs = File.OpenRead(filename);
      BinaryReader br = new BinaryReader(fs);
      bool IsBigEndian = false;

      try
      {
        byte[] readBuffer;
        ulong tempOffset = 0;
        ulong storedOffset = 0;

        long ThumbSearchPos = fs.Position;

        readBuffer = br.ReadBytes(2);

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xD8)) // search for the SOI (FF D8)
        {
          readBuffer = br.ReadBytes(2);
          if (fs.Position >= ThumbSearchPos + (1 * 1024)) return; // exit the routine via the error handler if no thumb was found
        }

        bool ExifInformationFound = true;

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xE1)) // search for the APP1 (Exif Data) (FF E1)
        {
          readBuffer = br.ReadBytes(2);
          if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD9) // EOI reached
          {
            throw (new Exception()); // exit the routine via the error handler
          }
          if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD8) // SOI reached
          {
            ExifInformationFound = false;
            fs.Seek(-2, SeekOrigin.Current);
            tempOffset = 0;
            break;
          }
          if (fs.Position >= ThumbSearchPos + (2 * 1024)) return; // exit the routine via the error handler if no thumb was found
        }

        if (ExifInformationFound)
        {
          while (!(readBuffer[0] == 0x49 && readBuffer[1] == 0x49) && !(readBuffer[0] == 0x4D && readBuffer[1] == 0x4D)) // search for the byte order 
          {
            readBuffer = br.ReadBytes(2);
            if (readBuffer[0] == 0xFF && readBuffer[1] == 0xD9) // EOI reached
            {
              throw (new Exception()); // exit the routine via the error handler
            }
            if (fs.Position >= ThumbSearchPos + (5 * 1024)) return; // exit the routine via the error handler if no thumb was found
          }

          if (readBuffer[0] == 0x4D && readBuffer[1] == 0x4D) // is big endian?
          {
            IsBigEndian = true;
          }

          ExifOffsetBase = fs.Position - 2;

          readBuffer = br.ReadBytes(2); // read 0x42

          tempOffset = BitConverter.ToUInt32(ReadEndianBytes(br, IsBigEndian, 4), 0); // read the offset of the exif data
        }

        while (tempOffset != 0) // cycle through the IFD's
        {
          fs.Seek(ExifOffsetBase + (long)tempOffset, SeekOrigin.Begin); // jump to the begin of the exif data
          int tmpCount = BitConverter.ToInt16(ReadEndianBytes(br, IsBigEndian, 2), 0); // Read the count of entries

          if (tmpCount > 250) break; //skip corrupted exif info

          //fs.Seek(tmpCount * 12, SeekOrigin.Current); // skip the entries

          ulong nextIfdOffset = 0; // If the Exif information is spread over more than one IFD, this value stores the offset to the next block

          for (int i = 0; i < tmpCount; i++)
          {
            uint entryId = BitConverter.ToUInt16(ReadEndianBytes(br, IsBigEndian, 2), 0); // Read the entry ID
            uint entryType = BitConverter.ToUInt16(ReadEndianBytes(br, IsBigEndian, 2), 0); // Read the entry type
            uint entryCount = BitConverter.ToUInt32(ReadEndianBytes(br, IsBigEndian, 4), 0); // Read the entry count
            byte[] entryData = ReadEndianBytes(br, IsBigEndian, 4); // Read the data value

            if (entryId == 0x8769 || (_EquipMake.ToLower() == "canon" && entryId == 0x927C)) // is this the link to the next IFD block? Or the a makernote
            {
              nextIfdOffset = BitConverter.ToUInt32(entryData, 0); // store the offset of the next IFD block
            }
            else
            {
              int LenBase = 1;
              switch ((PropertyTagType)entryType)
              {
                case PropertyTagType.Rational:
                  LenBase = 8;
                  break;
                case PropertyTagType.SRational:
                  LenBase = 8;
                  break;
                case PropertyTagType.Short:
                  LenBase = 2;
                  break;
                case PropertyTagType.Long:
                  LenBase = 4;
                  break;
                case PropertyTagType.SLONG:
                  LenBase = 4;
                  break;
                default:
                  LenBase = 1;
                  break;
              }

              if ((entryCount * LenBase) > 4)
              {
                ulong entryOffset = BitConverter.ToUInt32(entryData, 0); // Read the entry offset
                long curPos = fs.Position; // store the current position in the image file
                fs.Seek(ExifOffsetBase + (long)entryOffset, SeekOrigin.Begin); // jump to the begin of the entry data
                entryData = br.ReadBytes((int)entryCount * LenBase); // read the entry
                fs.Seek(curPos, SeekOrigin.Begin); // jump back to read the next entrty
              }

              if (_EquipMake.ToLower() == "canon" && entryId == 0x1 && entryType == 0x3)
              {
                AnalyseCanonMakerNoteBlock1(entryData);
              }

              switch ((PropertyTagType)entryType)
              {
                case PropertyTagType.Short:
                  {
                    if (IsBigEndian) // corrent the bit order
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
                    if (IsBigEndian)
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

          tempOffset = BitConverter.ToUInt32(ReadEndianBytes(br, IsBigEndian, 4), 0); // read the offset of the next data field
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
        //fs.Seek(-4, SeekOrigin.Current);

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

        //fs.Seek(2, SeekOrigin.Begin); //jump back to the begin

        ThumbSearchPos = fs.Position;
        readBuffer = br.ReadBytes(2); // read begin of image

        while (!(readBuffer[0] == 0xFF && readBuffer[1] == 0xD8)) // search for the SOI (FF D8)
        {
          readBuffer = br.ReadBytes(2);
          if (fs.Position >= ThumbSearchPos + (10 * 1024)) return; // exit the routine via the error handler if no thumb was found
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
            if (fs.Position >= ThumbSearchPos + (20 * 1024)) return; // exit the routine via the error handler if no thumb was found 
          }
          ms.Seek(0, SeekOrigin.Begin);

          _ThumbImage = new Bitmap(ms);
        }
      }
      catch (Exception )
      {
      }
      finally
      {
        br.Close();
        fs.Close();
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
          case 15: _ISOSpeed = "Auto";
            break;
          case 16: _ISOSpeed = "50";
            break;
          case 17: _ISOSpeed = "100";
            break;
          case 18: _ISOSpeed = "200";
            break;
          case 19: _ISOSpeed = "400";
            break;
        }
      }
    }

    private void FillFlashModeResult(uint data)
    {
      switch (data)
      {
        case 0x0: _FlashMode = "Flash did not fire.";
          break;
        case 0x1: _FlashMode = "Flash fired.";
          break;
        case 0x5: _FlashMode = "Strobe return light not detected.";
          break;
        case 0x7: _FlashMode = "Strobe return light detected.";
          break;
        case 0x9: _FlashMode = "Flash fired, compulsory flash mode";
          break;
        case 0xD: _FlashMode = "Flash fired, compulsory flash mode, return light not detected";
          break;
        case 0xF: _FlashMode = "Flash fired, compulsory flash mode, return light detected";
          break;
        case 0x10: _FlashMode = "Flash did not fire, compulsory flash mode";
          break;
        case 0x18: _FlashMode = "Flash did not fire, auto mode";
          break;
        case 0x19: _FlashMode = "Flash fired, auto mode";
          break;
        case 0x1d: _FlashMode = "Flash fired, auto mode, return light not detected";
          break;
        case 0x1f: _FlashMode = "Flash fired, auto mode, return light detected";
          break;
        case 0x20: _FlashMode = "No flash function";
          break;
        case 0x41: _FlashMode = "Flash fired, red-eye reduction mode";
          break;
        case 0x45: _FlashMode = "Flash fired, red-eye reduction mode, return light not detected";
          break;
        case 0x47: _FlashMode = "Flash fired, red-eye reduction mode, return light detected";
          break;
        case 0x49: _FlashMode = "Flash fired, compulsory flash mode, red-eye reduction mode";
          break;
        case 0x4d: _FlashMode = "Flash fired, compulsory flash mode, red-eye reduction mode, return light not detected";
          break;
        case 0x4f: _FlashMode = "Flash fired, compulsory flash mode, red-eye reduction mode, return light detected";
          break;
        case 0x59: _FlashMode = "Flash fired, auto mode, red-eye reduction mode";
          break;
        case 0x5d: _FlashMode = "Flash fired, auto mode, return light not detected, red-eye reduction mode";
          break;
        case 0x5f: _FlashMode = "Flash fired, auto mode, return light detected, red-eye reduction mode";
          break;
      }
    }

    private byte[] ReadEndianBytes(BinaryReader br, bool ReadBigEndian, int CountOfBytes)
    {
      byte[] tmpBuffer = br.ReadBytes(CountOfBytes);
      if (ReadBigEndian) Array.Reverse(tmpBuffer);
      return tmpBuffer;
    }

    private void FillProperty(uint EntryID, uint EntryType, byte[] EntryData)
    {
      switch ((PropertyTagId)EntryID)
      {
        case PropertyTagId.EquipMake: _EquipMake = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.EquipModel: _EquipModel = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ImageDescription: _ImageDescription = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.Copyright: _Copyright = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifDTOrig: _DTOrig = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifDTDigitized: _DTDigitized = (string)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifFocalLength: _FocalLength = (Fraction)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifFNumber: _FNumber = (Fraction)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifExposureBias: _ExposureBias = (Fraction)PropertyTag.getValue(EntryType, EntryData);
          break;
        case PropertyTagId.ExifExposureTime:
          {
            _ExposureTime = (Fraction)PropertyTag.getValue(EntryType, EntryData);
            if (_ExposureTime.Numerator > 1)
            {
              _ExposureTime.Denumerator /= _ExposureTime.Numerator;
              _ExposureTime.Numerator /= _ExposureTime.Numerator;
            }
          }
          break;
        case PropertyTagId.ExifISOSpeed:
          {
            object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            if (tmpValue.GetType().ToString().Equals("System.UInt16")) _ISOSpeed = ((uint)(ushort)tmpValue).ToString();
            else _ISOSpeed = ((uint)tmpValue).ToString();
          }
          break;
        case PropertyTagId.Orientation:
          {
            object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            if (tmpValue.GetType().ToString().Equals("System.UInt16")) _Orientation = (uint)(ushort)tmpValue;
            else _Orientation = (uint)tmpValue;
          }
          break;
        case PropertyTagId.ExifPixXDim:
          {
            object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            if (tmpValue.GetType().ToString().Equals("System.UInt16")) _PixXDim = (uint)(ushort)tmpValue;
            else _PixXDim = (uint)tmpValue;
          }
          break;
        case PropertyTagId.ExifPixYDim:
          {
            object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            if (tmpValue.GetType().ToString().Equals("System.UInt16")) _PixYDim = (uint)(ushort)tmpValue;
            else _PixYDim = (uint)tmpValue;
          }
          break;
        case PropertyTagId.ExifFlash:
          {
            //object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            uint tmpValue = BitConverter.ToUInt16(EntryData, 0);
            if ((tmpValue & 0x1) == 1)
            {
              _FlashFired = true;
              FillFlashModeResult(tmpValue);
            }
            else
            {
              _FlashFired = false;
              FillFlashModeResult(tmpValue);
            }
          }
          break;
        case PropertyTagId.ExifMeteringMode:
          {
            object tmpValue = PropertyTag.getValue(EntryType, EntryData);
            if (tmpValue.GetType().ToString().Equals("System.UInt16")) _MeteringMode = (MeteringMode)(uint)(ushort)tmpValue;
            else _MeteringMode = (MeteringMode)(uint)tmpValue;
          }
          break;
      }
    }

    public void Dispose()
    {
      try
      {
        if (_ThumbImage != null)
          _ThumbImage.Dispose();
        _ThumbImage = null;
      }
      catch (NullReferenceException)
      { }
    }
  }
}
