using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.Plugins.WifiRemote.Utils
{
    class ImageHelperError
    {
        public enum ImageHelperErrorType
        {
            WatcherCreate,
            WatcherEnable,
            DirectoryCreate,
            Timeout,
            ScreenshotRead
        };

        /// <summary>
        /// Unique code for this error
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Descriptive message of this error
        /// </summary>
        public String ErrorMessage { get; set; }

        public ImageHelperError(ImageHelperErrorType type)
        {
            setupExceptionWithType(type);
        }

        protected void setupExceptionWithType(ImageHelperErrorType type)
        {
            switch (type)
            {
                case ImageHelperErrorType.WatcherCreate:
                    ErrorCode = 10;
                    ErrorMessage = "Could not watch for MediaPortal screenshots.";
                    break;

                case ImageHelperErrorType.WatcherEnable:
                    ErrorCode = 11;
                    ErrorMessage = "Error starting to watch for MediaPortal screenshots.";
                    break;

                case ImageHelperErrorType.DirectoryCreate:
                    ErrorCode = 20;
                    ErrorMessage = "Could not create screenshot directory.";
                    break;

                case ImageHelperErrorType.Timeout:
                    ErrorCode = 30;
                    ErrorMessage = "Timeout while waiting for MediaPortal to take the screenshot.";
                    break;
                    
                case ImageHelperErrorType.ScreenshotRead:
                    ErrorCode = 40;
                    ErrorMessage = "Could not read MediaPortal screenshot";
                    break;

                default:
                    ErrorCode = 0;
                    ErrorMessage = "An unexpected error occured.";
                    break;
            }
        }
    }

    class ImageHelper
    {
        #region Take a MediaPortal screenshot

        /// <summary>
        /// Callback for when the screenshot was taken and is stored as a
        /// byte array in the Screenshot property.
        /// </summary>
        public delegate void ScreenshotReadyCallback();

        /// <summary>
        /// Callback for when the screenshot could not be taken or processed.
        /// </summary>
        public delegate void ScreenshotFailedCallback(ImageHelperError error);

        /// <summary>
        /// Tracks if a screenshot is being made at the moment
        /// </summary>
        bool takingScreenshot;

        /// <summary>
        /// FileSystemWatcher watching for new screenshots
        /// </summary>
        FileSystemWatcher watcher;

        /// <summary>
        /// Path of the current screenshot
        /// </summary>
        String screenshotPath;

        /// <summary>
        /// Number of times the screenshot was tried to open.
        /// We abort after maximumScreenshotOpenTries times to avoid an
        /// infinite loop.
        /// </summary>
        uint screenshotOpenTries;

        /// <summary>
        /// Abort trying to open the screenshot after this number of tries.
        /// </summary>
        uint maximumScreenshotOpenTries;

        private Image screenshot;
        /// <summary>
        /// The screenshot taken with the takeScreenshot() method
        /// </summary>
        public Image Screenshot
        {
            get { return screenshot; }
            set { screenshot = value; }
        }

        public ImageHelper()
        {
            maximumScreenshotOpenTries = 20;
            screenshotOpenTries = 0;
        }

        /// <summary>
        /// Takes a Screenshot of the GUI
        /// </summary>
        public void TakeScreenshot()
        {
          var handler = Control.FromHandle(ServiceRegistration.Get<IScreenControl>().MainWindowHandle);
          if (handler != null)
          {
            Rectangle bounds = handler.Bounds;
            using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
            {
              using (Graphics g = Graphics.FromImage(bitmap))
              {
                g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);
              }
              // save screenshot and process
              Screenshot = bitmap;
              OnScreenshotReady();
            }
          }
          else
          {
            ServiceRegistration.Get<ILogger>().Warn("Wifi Remote: Couldn't create screenshot");
          }
        }

        /// <summary>
        /// Returns the resized screenshot as a byte array.
        /// </summary>
        /// <param name="width">Width to resize the screenshot proportionally to, 0 to keep original</param>
        /// <returns></returns>
        public byte[] resizedScreenshot(int width)
        {
            if (Screenshot == null)
            {
                return new byte[0];
            }

            Image image = (width > 0) ? ImageHelper.ResizedImage(Screenshot, width) : Screenshot;
            return ImageHelper.imageToByteArray(image, System.Drawing.Imaging.ImageFormat.Jpeg);
        }

        /// <summary>
        /// Check if the screenshot is locked.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected static bool IsScreenshotReady(String path)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    long streamLength = inputStream.Length;
                    return streamLength > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Screenshot ready event
        /// </summary>
        public event ScreenshotReadyCallback ScreenshotReady;
        protected void OnScreenshotReady()
        {
            if (ScreenshotReady != null)
            {
                ScreenshotReady();
            }
        }

        /// <summary>
        /// Screenshot failed event
        /// </summary>
        public event ScreenshotFailedCallback ScreenshotFailed;
        protected void OnScreenshotFailed(ImageHelperError error)
        {
            if (ScreenshotFailed != null)
            {
                ScreenshotFailed(error);
            }
        }

        #endregion

        #region Static utility methods

        /// <summary>
        /// Returns an image as its byte array representation.
        /// Used to make images encodable in JSON.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static byte[] imageToByteArray(Image img, System.Drawing.Imaging.ImageFormat format)
        {
            byte[] byteArray = new byte[0];
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, format);
                stream.Close();
                byteArray = stream.ToArray();
            }

            return byteArray;
        }

        /// <summary>
        /// Resizes an image to the target with. The height is calculated
        /// proportionally to the source image height.
        /// </summary>
        /// <param name="source">The source image to resize</param>
        /// <param name="width">Target width for the resized image</param>
        /// <returns></returns>
        public static Image ResizedImage(Image source, int width)
        {
            if (source.Width <= width)
            {
                return source;
            }

            int height = (int)(source.Height / ((double)source.Width / (double)width));
            Image target = new Bitmap(source, width, height);

            return target;
        }

        #endregion
    }
}
