using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace MediaPortal.Extensions.UserServices.FanArtService.Interfaces
{
  /// <summary>
  /// Provides various image untilities, such as high quality resizing and the ability to save a JPEG.
  /// </summary>
  public static class ImageUtilities
  {
    /// <summary>
    /// A quick lookup for getting image encoders
    /// </summary>
    private static Dictionary<string, ImageCodecInfo> _encoders = null;

    /// <summary>
    /// A quick lookup for getting image encoders
    /// </summary>
    public static Dictionary<string, ImageCodecInfo> Encoders
    {
      //get accessor that creates the dictionary on demand
      get
      {
        //if the quick lookup isn't initialised, initialise it
        if (_encoders == null)
          _encoders = new Dictionary<string, ImageCodecInfo>();

        //if there are no codecs, try loading them
        if (_encoders.Count == 0)
          //get all the codecs
          foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
          {
            //add each codec to the quick lookup
            _encoders.Add(codec.MimeType.ToLower(), codec);
          }

        //return the lookup
        return _encoders;
      }
    }

    /// <summary>
    /// Resize the image to the specified width and height.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The width to resize to.</param>
    /// <param name="height">The height to resize to.</param>
    /// <returns>The resized image.</returns>
    public static Bitmap ResizeImage(Image image, int width, int height)
    {
      //a holder for the result
      Bitmap result = new Bitmap(width, height);

      //use a graphics object to draw the resized image into the bitmap
      using (Graphics graphics = Graphics.FromImage(result))
      {
        //set the resize quality modes to high quality
        graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        //draw the image into the target bitmap
        graphics.DrawImage(image, 0, 0, result.Width, result.Height);
      }

      //return the resulting bitmap
      return result;
    }

    /// <summary> 
    /// Saves an image as a jpeg image, with the given quality 
    /// </summary> 
    /// <param name="path">Path to which the image would be saved.</param>
    /// <param name="image">Image to save.</param>
    /// <param name="quality">An integer from 0 to 100, with 100 being the highest quality</param> 
    /// <exception cref="ArgumentOutOfRangeException">
    /// An invalid value was entered for image quality.
    /// </exception>
    public static void SaveJpeg(string path, Image image, int quality)
    {
      //ensure the quality is within the correct range
      if ((quality < 0) || (quality > 100))
      {
        //create the error message
        string error = string.Format("Jpeg image quality must be between 0 and 100, with 100 being the highest quality.  A value of {0} was specified.", quality);
        //throw a helpful exception
        throw new ArgumentOutOfRangeException(error);
      }

      //create an encoder parameter for the image quality
      EncoderParameter qualityParam = new EncoderParameter(Encoder.Quality, quality);
      //get the jpeg codec
      ImageCodecInfo jpegCodec = GetEncoderInfo("image/jpeg");

      //create a collection of all parameters that we will pass to the encoder
      EncoderParameters encoderParams = new EncoderParameters(1);
      //set the quality parameter for the codec
      encoderParams.Param[0] = qualityParam;
      //save the image using the codec and the parameters
      image.Save(path, jpegCodec, encoderParams);
    }

    /// <summary> 
    /// Returns the image codec with the given mime type 
    /// </summary> 
    public static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
      //do a case insensitive search for the mime type
      string lookupKey = mimeType.ToLower();

      //the codec to return, default to null
      ImageCodecInfo foundCodec = null;

      //if we have the encoder, get it to return
      if (Encoders.ContainsKey(lookupKey))
        //pull the codec from the lookup
        foundCodec = Encoders[lookupKey];

      return foundCodec;
    }
  }
}