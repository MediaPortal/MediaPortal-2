using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using SharpCompress.Archives;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Emulators.Common.GoodMerge
{
  public class SharpCompressExtractor : IExtractor
  {
    protected static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    protected static readonly HashSet<string> SUPPORTED_EXTENSIONS = new HashSet<string> { ".7z", ".gz", ".rar", ".tar", ".zip" };
    protected string _archivePath;
    protected IArchive _extractor;
    protected long _currentEntrySize;

    public SharpCompressExtractor(string archivePath)
    {
      _archivePath = archivePath;
    }

    public event EventHandler<ExtractionEventArgs> ExtractionProgress;
    protected virtual void OnExtractionProgress(ExtractionEventArgs e)
    {
      var extractionProgress = ExtractionProgress;
      if (extractionProgress != null)
        extractionProgress(this, e);
    }

    public event EventHandler ExtractionComplete;
    protected virtual void OnExtractionComplete()
    {
      var extractionComplete = ExtractionComplete;
      if (extractionComplete != null)
        extractionComplete(this, EventArgs.Empty);
    }

    public bool IsArchive()
    {
      string extension = DosPathHelper.GetExtension(_archivePath).ToLowerInvariant();
      return SUPPORTED_EXTENSIONS.Contains(extension);
    }

    protected bool Init()
    {
      if (_extractor != null)
        return true;
      try
      {
        _extractor = ArchiveFactory.Open(_archivePath);
        _extractor.CompressedBytesRead += ExtractorCompressedBytesRead;
        _extractor.EntryExtractionEnd += ExtractorEntryExtractionEnd;
        return true;
      }
      catch (Exception ex)
      {
        Logger.Error("Extractor: Failed to open archive '{0}'", ex, _archivePath);
      }
      Dispose();
      return false;
    }

    public List<string> GetArchiveFiles()
    {
      if (!Init() || _extractor.Entries == null)
        return null;
      return _extractor.Entries.Where(e => !e.IsDirectory).Select(e => e.Key).ToList();
    }

    public bool ExtractArchiveFile(string archiveFile, string extractionPath)
    {
      if (!Init() || _extractor.Entries == null)
        return false;

      IArchiveEntry entry = _extractor.Entries.FirstOrDefault(a => a.Key == archiveFile);
      if (entry != null)
      {
        string extractionDirectory = Path.GetDirectoryName(extractionPath);
        try
        {
          if (!Directory.Exists(extractionDirectory))
            Directory.CreateDirectory(extractionDirectory);
          _currentEntrySize = entry.Size;
          entry.WriteToFile(extractionPath);
          return true;
        }
        catch (Exception ex)
        {
          Logger.Error("Extractor: Failed to extract '{0}' to '{1}'", ex, entry.Key, extractionPath);
        }
      }
      return false;
    }

    public bool ExtractAll(string extractionPath)
    {
      if (!Init() || _extractor.Entries == null)
        return false;
      try
      {
        _extractor.WriteToDirectory(extractionPath);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Error("Extractor: Failed to extract all files from '{0}' to '{1}'", ex, _archivePath, extractionPath);
      }
      return false;
    }

    protected void ExtractorCompressedBytesRead(object sender, CompressedBytesReadEventArgs e)
    {
      int perc = _currentEntrySize > 0 ? (int)((e.CurrentFilePartCompressedBytesRead * 100) / _currentEntrySize) : 0;
      OnExtractionProgress(new ExtractionEventArgs(perc));
    }

    protected void ExtractorEntryExtractionEnd(object sender, ArchiveExtractionEventArgs<IArchiveEntry> e)
    {
      OnExtractionComplete();
    }

    public void Dispose()
    {
      if (_extractor != null)
      {
        _extractor.Dispose();
        _extractor = null;
      }
    }
  }
}