using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities.SystemAPI;
using OpenCvSharp;
using OpenCvSharp.Internal.Util;
using System;

namespace OpenCvLib
{
  public class OpenCvWrapper
  {
    protected static bool _isInit;

    /// <summary>
    /// Attempts to set the native dll search path and load the native dlls.
    /// Should be called once before calling any methods of this class.
    /// </summary>
    /// <exception cref="OpenCvSharpException">Thrown if the native dlls could not be loaded.</exception>
    /// <exception cref="Exception">Thrown if the search path could not be set.</exception>
    public static void Init()
    {
      if (_isInit)
        return;

      if (!NativeMethods.SetPlatformSearchDirectories(out _))
        throw new Exception("Error adding dll probe path");
      PInvokeHelper.TryPInvoke();
      _isInit = true;
    }

    /// <summary>
    /// Tries to extract a thumbnail image from a video file.
    /// </summary>
    /// <param name="path">The path to the video file..</param>
    /// <param name="percentageOffset">The percentage offset into the video to extract the thumbnail from,
    /// valid values are between 0 and 1.</param>
    /// <param name="maxWidth">The maximum width of the extracted thumbnail.</param>
    /// <param name="thumbnail">If successful, the extracted thumbnail data.</param>
    /// <returns><c>true</c> if the thumbnail was extracted.</returns>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Init"/> has not yet been called.</exception>
    public static bool TryExtractThumbnail(string path, double percentageOffset, int maxWidth, out byte[] thumbnail)
    {
      if (!_isInit)
        throw new InvalidOperationException("Init must be called before calling any methods of this class");

      thumbnail = null;

      try
      {
        using (VideoCapture capture = new VideoCapture())
        {
          // Check that the capture can be opened and that it has frames to capture.
          if (!ValidateCapture(capture, path))
            return false;

          // Calculate and set the frame to capture
          int captureFrame = (int)(capture.FrameCount * percentageOffset);
          Logger.Debug("OpenCvWrapper: Setting position frame for '{0}' to {1}", path, captureFrame);
          capture.PosFrames = captureFrame;

          // Capture the frame
          using (Mat mat = capture.RetrieveMat())
          {
            // Check that the frame isn't empty
            if (!ValidateMat(mat, path))
              return false;

            // If the frame width is less than the max, extract it at it's current size
            if (mat.Width <= maxWidth)
            {
              Logger.Debug("OpenCvWrapper: Extracting thumbnail from '{0}' at original size {1}x{2}", path, mat.Width, mat.Height);
              thumbnail = mat.ToBytes();
              return true;
            }

            // Else resize to the maximum width retaining the aspect ratio
            double downscale = (double)maxWidth / mat.Width;
            using (Mat scaledMat = mat.Resize(new Size(maxWidth, mat.Height * downscale)))
            {
              Logger.Debug("OpenCvWrapper: Extracting thumbnail from '{0}' at scaled size {1}x{2}", path, scaledMat.Width, scaledMat.Height);
              thumbnail = scaledMat.ToBytes();
              return true;
            }
          }
        }
      }
      catch (Exception ex)
      {
        Logger.Warn("OpenCvWrapper: Exception when extracting thumbnail from '{0}'", ex, path);
        return false;
      }
    }

    protected static bool ValidateCapture(VideoCapture capture, string path)
    {
      if (!capture.Open(path))
      {
        Logger.Warn("OpenCvWrapper: Unable to extract thumbnail from '{0}', cannot open file", path);
        return false;
      }

      if (capture.FrameCount == 0)
      {
        Logger.Warn("OpenCvWrapper: Unable to extract thumbnail from '{0}', frame count is 0", path);
        return false;
      }

      return true;
    }

    protected static bool ValidateMat(Mat mat, string path)
    {
      if (mat.Empty())
      {
        Logger.Warn("OpenCvWrapper: Unable to extract thumbnail from '{0}', mat is empty", path);
        return false;
      }

      if (mat.Width == 0 || mat.Height == 0)
      {
        Logger.Warn("OpenCvWrapper: Unable to extract thumbnail from '{0}', invalid dimensions {1}x{2}", path, mat.Width, mat.Height);
        return false;
      }

      return true;
    }

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
