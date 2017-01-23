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
using System.Drawing;
using System.IO;
using System.Linq;
using FreeImageAPI;
using FreeImageAPI.Metadata;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Extensions.MetadataExtractors.ImageMetadataExtractor.ExifMetaInfo
{
  /// <summary>
  /// ExifMetaInfo class is used to extract EXIF information from images.
  /// </summary>
  public class ExifMetaInfo : IDisposable
  {
    #region Variables

    private uint? _pixXDim;
    private uint? _pixYDim;
    private string _equipMake = string.Empty;
    private string _equipModel = string.Empty;
    private string _imageDescription = string.Empty;
    private string _copyright = string.Empty;
    private DateTime? _dtOrig = null;
    private DateTime? _dtDigitized = null;
    private MetadataModel.ExifImageOrientation? _orientation;
    private double? _focalLength = null;
    private double? _fNumber = null;
    private string _exposureTime = null;
    private double? _exposureBias = null;
    private string _isoSpeed = string.Empty;
    private bool _flashFired;
    private string _flashMode = string.Empty;
    private MeteringMode _meteringMode = MeteringMode.Unknown;
    private Bitmap _thumbImage;
    private double? _longitude;
    private double? _latitude;

    #endregion

    #region Properties

    /// <summary>
    /// Returns the width of image.
    /// </summary>
    public uint? PixXDim { get { return _pixXDim; } }

    /// <summary>
    /// Returns the height of image.
    /// </summary>
    public uint? PixYDim { get { return _pixYDim; } }

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
    public MetadataModel.ExifImageOrientation? OrientationType { get { return _orientation; } }

    ///<summary>
    ///Date and time when the original image data was generated. For a DSC, the date and time when the image was taken. 
    ///</summary>
    public DateTime? OriginalDate { get { return _dtOrig; } }

    ///<summary>
    ///Date and time when the image was stored as digital data. If, for example, an image was captured by DSC and at the same time the file was recorded, then DateTimeOriginal and DateTimeDigitized will have the same contents.
    ///</summary>
    public DateTime? DigitizedDate { get { return _dtDigitized; } }

    ///<summary>
    ///Actual focal length, in millimeters, of the lens. Conversion is not made to the focal length of a 35 millimeter film camera.
    ///</summary>						
    public double? FocalLength { get { return _focalLength; } }

    ///<summary>
    ///F number.
    ///</summary>						
    public double? FNumber { get { return _fNumber; } }

    ///<summary>
    ///Exposure Time.
    ///</summary>						
    public string ExposureTime { get { return _exposureTime; } }

    /// <summary>
    /// Exposure Bias.
    /// </summary>
    public double? ExposureBias { get { return _exposureBias; } }

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

    /// <summary>
    /// Gets the GPS latitude.
    /// </summary>
    public double? Latitude { get { return _latitude; } }

    /// <summary>
    /// Gets the GPS longitude.
    /// </summary>
    public double? Longitude { get { return _longitude; } }

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
    public ExifMetaInfo(IFileSystemResourceAccessor mediaItemAccessor)
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


    private void ReadMetaInfo(IFileSystemResourceAccessor mediaItemAccessor)
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
      FIBITMAP dib = new FIBITMAP();
      try
      {
        // Check if FreeImage is available
        if (!FreeImage.IsAvailable())
          throw new Exception("FreeImage is not available!");

        // Load the image from stream, try to read headers only, without decoding
        dib = FreeImage.LoadFromStream(mediaStream, FREE_IMAGE_LOAD_FLAGS.LOAD_NOPIXELS);
        if (dib.IsNull)
          throw new Exception("FreeImage could not load image");

        FillProperties(dib);
      }
      catch
      { }
      finally
      {
        mediaStream.Close();
        FreeImage.UnloadEx(ref dib);
      }
    }

    private void FillProperties(FIBITMAP dib)
    {
      // Create a wrapper for all metadata the image contains
      ImageMetadata iMetadata = new ImageMetadata(dib);

      var main = ((MDM_EXIF_MAIN) iMetadata[FREE_IMAGE_MDMODEL.FIMD_EXIF_MAIN]);
      var exif = ((MDM_EXIF_EXIF) iMetadata[FREE_IMAGE_MDMODEL.FIMD_EXIF_EXIF]);
      _equipMake = SafeTrim(main.Make);
      _equipModel = SafeTrim(main.EquipmentModel);
      _imageDescription = SafeTrim(main.ImageDescription);
      _copyright = SafeJoin(main.Copyright);
      _dtOrig = exif.DateTimeOriginal;
      _dtDigitized = exif.DateTimeDigitized;
      _focalLength = exif.FocalLength;
      _fNumber = exif.FNumber;
      if (exif.ExposureTime.HasValue)
        _exposureTime = string.Format("{0}/{1}", exif.ExposureTime.Value.Numerator, exif.ExposureTime.Value.Denominator);

      _exposureBias = exif.ExposureBiasValue;
      _isoSpeed = exif.ISOSpeedRatings != null ? exif.ISOSpeedRatings[0].ToString() : null;
      _orientation = main.Orientation;
      _pixXDim = exif.PixelXDimension;
      _pixYDim = exif.PixelYDimension;
      _flashFired = exif.Flash.HasValue && (exif.Flash.Value & 1) == 1;
      if (exif.Flash.HasValue)
        FillFlashModeResult(exif.Flash.Value);
      _meteringMode = (MeteringMode) (exif.MeteringMode.HasValue ? exif.MeteringMode.Value : 0);

      TryParseGPS(iMetadata, out _latitude, out _longitude);
    }

    private bool TryParseGPS(ImageMetadata iMetadata, out double? latitude, out double? longitude)
    {
      MDM_EXIF_GPS gps = (MDM_EXIF_GPS) iMetadata[FREE_IMAGE_MDMODEL.FIMD_EXIF_GPS];

      double? lonValue = ToDecimalDegree(gps.Longitude);
      if (lonValue.HasValue && gps.LongitudeDirection != null && gps.LongitudeDirection == MetadataModel.LongitudeType.West)
        lonValue *= -1;

      double? latValue = ToDecimalDegree(gps.Latitude);
      if (latValue.HasValue && gps.Latitude != null && gps.LatitudeDirection == MetadataModel.LatitudeType.South)
        latValue *= -1;

      latitude = latValue;
      longitude = lonValue;
      return latitude.HasValue && longitude.HasValue;
    }

    private string SafeTrim(string exifString)
    {
      if (string.IsNullOrWhiteSpace(exifString))
        return null;
      return exifString.Trim();
    }

    private string SafeJoin(string[] list)
    {
      if (list == null)
        return null;

      return list.Select(SafeTrim).Aggregate((a, b) => string.Format("{0}; {1}", a, b));
    }

    private double? ToDecimalDegree(FIURational[] vals)
    {
      if (vals != null && vals.Length == 3)
        return vals[0] + (vals[1] / 60) + (vals[2] / 3600);
      return null;
    }

    // TODO: Implement enum for FlashMode and return that enum, implement translation function for that enum to a string.
    private void FillFlashModeResult(uint data)
    {
      switch (data)
      {
        case 0x0: _flashMode = "Flash did not fire";
          break;
        case 0x1: _flashMode = "Flash fired";
          break;
        case 0x5: _flashMode = "Strobe return light not detected";
          break;
        case 0x7: _flashMode = "Strobe return light detected";
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
    public void Dispose()
    {
      if (_thumbImage != null)
        _thumbImage.Dispose();
      _thumbImage = null;
    }
  }
}
