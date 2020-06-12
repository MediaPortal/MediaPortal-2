using Emulators.Common.GoodMerge;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emulators.GoodMerge
{
  public class ExtractionCompletedEventArgs : EventArgs
  {
    public ExtractionCompletedEventArgs(string extractedItem, string extractedPath, bool success)
    {
      ExtractedItem = extractedItem;
      ExtractedPath = extractedPath;
      Success = success;
    }
    public string ExtractedItem { get; private set; }
    public string ExtractedPath { get; private set; }
    public bool Success { get; private set; }
  }

  public class GoodMergeExtractor
  {
    protected const string EXTRACT_PATH_PREFIX = "MP2GoodMergeCache";
    protected IWork _extractionThread;

    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public event EventHandler<ExtractionEventArgs> ExtractionProgress;
    protected virtual void OnExtractionProgress(object sender, ExtractionEventArgs e)
    {
      var handler = ExtractionProgress;
      if (handler != null)
        handler(this, e);
    }

    public event EventHandler<ExtractionCompletedEventArgs> ExtractionCompleted;
    protected virtual void OnExtractionCompleted(ExtractionCompletedEventArgs e)
    {
      var handler = ExtractionCompleted;
      if (handler != null)
        handler(this, e);
    }

    public void Extract(ILocalFsResourceAccessor accessor, string selectedItem)
    {
      if (accessor == null || string.IsNullOrEmpty(selectedItem))
        return;
      _extractionThread = ServiceRegistration.Get<IThreadPool>().Add(() => DoExtract(accessor, selectedItem));
    }

    public void WaitForExtractionThread()
    {
      while (_extractionThread != null)
        Thread.Sleep(100);
    }

    protected void DoExtract(ILocalFsResourceAccessor accessor, string selectedItem)
    {
      string resourcePath = accessor.CanonicalLocalResourcePath.LastPathSegment.Path;
      string extractionPath = GetExtractionPath(resourcePath, selectedItem);
      Logger.Debug("GoodMergeExtractor: Extracting '{0}' from '{1}' to '{2}'", selectedItem, resourcePath, extractionPath);
      bool result;
      using (IExtractor extractor = ExtractorFactory.Create(accessor.LocalFileSystemPath))
      {
        extractor.ExtractionProgress += OnExtractionProgress;
        result = extractor.ExtractArchiveFile(selectedItem, extractionPath);
      }
      if (!result)
        //Sometimes an empty file has been created when extraction fails
        DeleteExtractedFile(extractionPath);
      _extractionThread = null;
      OnExtractionCompleted(new ExtractionCompletedEventArgs(selectedItem, extractionPath, result));
    }

    public static bool IsExtracted(ILocalFsResourceAccessor accessor, string selectedItem, out string extractedPath)
    {
      extractedPath = GetExtractionPath(accessor.CanonicalLocalResourcePath.LastPathSegment.Path, selectedItem);
      return File.Exists(extractedPath);
    }

    public static string GetExtractionDirectory()
    {
      return Path.Combine(Path.GetTempPath(), EXTRACT_PATH_PREFIX);
    }

    public static void DeleteExtractionDirectory()
    {
      string extractionDirectory = GetExtractionDirectory();
      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(extractionDirectory);
        if (directoryInfo.Exists)
          directoryInfo.Delete(true);
      }
      catch (Exception ex)
      {
        Logger.Warn("GoodMergeExtractor: Unable to delete extraction directory '{0}': {1}", extractionDirectory, ex);
      }
    }

    protected static void DeleteExtractedFile(string extractedFile)
    {
      try
      {
        File.Delete(extractedFile);
      }
      catch
      {
      }
    }

    protected static string GetExtractionPath(string archivePath, string selectedItem)
    {
      return Path.Combine(GetExtractionDirectory(), GetPathHash(archivePath), selectedItem);
    }

    protected static string GetPathHash(string path)
    {
      byte[] input = Encoding.UTF8.GetBytes(path);
      byte[] output;
      using (MD5 md5Hash = MD5.Create())
        output = md5Hash.ComputeHash(input);
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < output.Length; i++)
        sb.Append(output[i].ToString("x2"));
      return sb.ToString();
    }
  }
}
