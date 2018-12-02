#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.ImpersonationService;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;
using System.Threading;

namespace MediaPortal.Extensions.MetadataExtractors.MatroskaLib
{
  /// <summary>
  /// <see cref="MatroskaInfoReader"/> uses the mkvtoolnix utilities to extract metadata and attachments from matroska files.
  /// </summary>
  public class MatroskaInfoReader
  {
    /// <summary>
    /// Maximum duration for creating a tag extraction.
    /// </summary>
    protected const int PROCESS_TIMEOUT_MS = 15000;
    protected const int MAX_CONCURRENT_MKVINFO = 5;
    protected const int MAX_CONCURRENT_MKVEXTRACT = 5;

    #region Fields

    private readonly ILocalFsResourceAccessor _lfsra;
    private List<MatroskaConsts.MatroskaAttachment> _attachments;
    private readonly string _mkvInfoPath;
    private static readonly SemaphoreSlim MKVINFO_THROTTLE_LOCK = new SemaphoreSlim(MAX_CONCURRENT_MKVINFO, MAX_CONCURRENT_MKVINFO);
    private readonly string _mkvExtractPath;
    private static readonly SemaphoreSlim MKVEXTRACT_THROTTLE_LOCK = new SemaphoreSlim(MAX_CONCURRENT_MKVEXTRACT, MAX_CONCURRENT_MKVEXTRACT);
    private readonly ProcessPriorityClass _priorityClass = ProcessPriorityClass.BelowNormal;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a list of attachments, is created after <see cref="ReadAttachmentsAsync"/> was called once.
    /// </summary>
    public IList<MatroskaConsts.MatroskaAttachment> Attachments
    {
      get { return _attachments; }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructs a new <see cref="MatroskaInfoReader"/>.
    /// </summary>
    /// <param name="lfsra">
    /// <see cref="ILocalFsResourceAccessor"/> pointing to the MKV file to extract information from; The caller
    /// is responsible that it is valid while this class is used and that it is disposed afterwards.
    /// </param>
    public MatroskaInfoReader(ILocalFsResourceAccessor lfsra)
    {
      _lfsra = lfsra;
      _mkvInfoPath = FileUtils.BuildAssemblyRelativePathForArchitecture("mkvinfo.exe");
      _mkvExtractPath = FileUtils.BuildAssemblyRelativePathForArchitecture("mkvextract.exe");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Reads all tags from matroska file and parses the XML for the requested tags (<paramref name="tagsToExtract"/>).
    /// </summary>
    /// <param name="tagsToExtract">Dictionary with tag names as keys.</param>
    public async Task ReadTagsAsync(IDictionary<string, IList<string>> tagsToExtract)
    {
      ProcessExecutionResult executionResult = null;
      // Calling EnsureLocalFileSystemAccess not necessary; access for external process ensured by ExecuteWithResourceAccess
      var arguments = string.Format("tags \"{0}\"", _lfsra.LocalFileSystemPath);
      await MKVEXTRACT_THROTTLE_LOCK.WaitAsync().ConfigureAwait(false);
      try
      {
        executionResult = await _lfsra.ExecuteWithResourceAccessAsync(_mkvExtractPath, arguments, _priorityClass, PROCESS_TIMEOUT_MS).ConfigureAwait(false);
      }
      catch (TaskCanceledException)
      {
        ServiceRegistration.Get<ILogger>().Warn("MatroskaInfoReader.ReadTags: External process aborted due to timeout: Executable='{0}', Arguments='{1}'", _mkvExtractPath, arguments);
      }
      finally
      {
        MKVEXTRACT_THROTTLE_LOCK.Release();
      }
      if (executionResult != null && executionResult.Success && !string.IsNullOrEmpty(executionResult.StandardOutput))
      {
        XDocument doc = XDocument.Parse(executionResult.StandardOutput);

        foreach (string key in new List<string>(tagsToExtract.Keys))
        {
          string[] parts = key.Split('.');
          int? targetType = null;
          string tagName;
          if (parts.Length == 2)
          {
            targetType = int.Parse(parts[0]);
            tagName = parts[1];
          }
          else
            tagName = parts[0];

          var result = from simpleTag in GetTagsForTargetType(doc, targetType).Elements("Simple")
                       let nameElement = simpleTag.Element("Name")
                       let stringElement = simpleTag.Element("String")
                       where nameElement != null && nameElement.Value == tagName &&
                             stringElement != null && !string.IsNullOrWhiteSpace(stringElement.Value)
                       select stringElement.Value;

          var resultList = result.ToList();
          if (resultList.Any())
            tagsToExtract[key] = resultList.ToList();
        }
      }
    }

    /// <summary>
    /// Reads the attachment information from the matroska file.
    /// </summary>
    public async Task ReadAttachmentsAsync()
    {
      // Only read attachments once
      if (_attachments != null)
        return;

      _attachments = new List<MatroskaConsts.MatroskaAttachment>();

      // Structure of mkvinfo attachment output
      // |+ Attachments
      // | + Attached
      // |  + File name: cover.jpg
      // |  + Mime type: image/jpeg
      // |  + File data, size: 132908
      // |  + File UID: 1495003044
      ProcessExecutionResult executionResult = null;
      // Calling EnsureLocalFileSystemAccess not necessary; access for external process ensured by ExecuteWithResourceAccess
      var arguments = string.Format("--ui-language en --output-charset UTF-8 \"{0}\"", _lfsra.LocalFileSystemPath);
      await MKVINFO_THROTTLE_LOCK.WaitAsync().ConfigureAwait(false);
      try
      {
        executionResult = await _lfsra.ExecuteWithResourceAccessAsync(_mkvInfoPath, arguments, _priorityClass, PROCESS_TIMEOUT_MS).ConfigureAwait(false);
      }
      catch (TaskCanceledException)
      {
        ServiceRegistration.Get<ILogger>().Warn("MatroskaInfoReader.ReadAttachments: External process aborted due to timeout: Executable='{0}', Arguments='{1}'", _mkvExtractPath, arguments);
      }
      finally
      {
        MKVINFO_THROTTLE_LOCK.Release();
      }
      if (executionResult != null && executionResult.Success)
      {
        StringReader reader = new StringReader(executionResult.StandardOutput);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          // Start of an attachment section
          if (line.Contains("Attached"))
            _attachments.Add(new MatroskaConsts.MatroskaAttachment());

          if (line.Contains("File name"))
            _attachments[_attachments.Count - 1].FileName = line.Substring(line.LastIndexOf(": ") + 2);

          if (line.Contains("Mime type"))
            _attachments[_attachments.Count - 1].MimeType = line.Substring(line.LastIndexOf(": ") + 2);

          if (line.Contains("File data, size"))
            _attachments[_attachments.Count - 1].FileSize = Int32.Parse(line.Substring(line.LastIndexOf(": ") + 2));
        }
      }
    }

    /// <summary>
    /// Reads the stereoscopic information from the matroska file.
    /// </summary>
    public async Task<MatroskaConsts.StereoMode> ReadStereoModeAsync()
    {
      // Structure of mkvinfo attachment output
      // |+ Attachments
      // | + Attached
      // |  + File name: cover.jpg
      // |  + Mime type: image/jpeg
      // |  + File data, size: 132908
      // |  + File UID: 1495003044
      ProcessExecutionResult executionResult = null;
      // Calling EnsureLocalFileSystemAccess not necessary; access for external process ensured by ExecuteWithResourceAccess
      var arguments = string.Format("--ui-language en --output-charset UTF-8 \"{0}\"", _lfsra.LocalFileSystemPath);
      await MKVINFO_THROTTLE_LOCK.WaitAsync().ConfigureAwait(false);
      try
      {
        executionResult = await _lfsra.ExecuteWithResourceAccessAsync(_mkvInfoPath, arguments, _priorityClass, PROCESS_TIMEOUT_MS).ConfigureAwait(false);
      }
      catch (TaskCanceledException)
      {
        ServiceRegistration.Get<ILogger>().Warn("MatroskaInfoReader.ReadStereoMode: External process aborted due to timeout: Executable='{0}', Arguments='{1}'", _mkvExtractPath, arguments);
      }
      finally
      {
        MKVINFO_THROTTLE_LOCK.Release();
      }
      if (executionResult != null && executionResult.Success)
      {
        StringReader reader = new StringReader(executionResult.StandardOutput);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
          if (line.Contains("Stereo mode"))
          {
            int stereoMode = 0;
            if (int.TryParse(line.Substring(line.LastIndexOf(": ") + 2, 2), out stereoMode))
            {
              return (MatroskaConsts.StereoMode)stereoMode;
            }
          }
        }
      }
      return MatroskaConsts.StereoMode.Mono;
    }

    /// <summary>
    /// Tries to extract an embedded cover from the matroska file. It checks attachments for a matching filename (cover.jpg/png).
    /// <para>
    /// <seealso cref="http://www.matroska.org/technical/cover_art/index.html"/>:
    /// The way to differentiate between all these versions is by the filename. The default filename is cover.(png/jpg) for backward compatibility reasons. 
    /// That is the "big" version of the file (600) in square or portrait mode. It should also be the first file in the attachments. 
    /// The smaller resolution should be prefixed with "small_", ie small_cover.(jpg/png). The landscape variant should be suffixed with "_land", ie cover_land.jpg. 
    /// The filenames are case sensitive and should all be lower case.
    /// In the end a file could contain these 4 basic cover art files:
    ///  cover.jpg (portrait/square 600)
    ///  small_cover.png (portrait/square 120)
    ///  cover_land.png (landscape 600)
    ///  small_cover_land.jpg (landscape 120)
    /// </para>
    /// </summary>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public Task<byte[]> GetCoverAsync()
    {
      return GetAttachmentByNameAsync("cover."); // Could be .png or .jpg
    }

    /// <summary>
    /// Tries to extract an embedded landscape cover from the matroska file. <seealso cref="GetCoverAsync"/> for more information about naming conventions.
    /// </summary>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public Task<byte[]> GetCoverLandscape()
    {
      return GetAttachmentByNameAsync("cover_land."); // Could be .png or .jpg
    }

    /// <summary>
    /// Tries to extract an attachment by its name.
    /// </summary>
    /// <param name="fileNamePart">Beginn of filename</param>
    /// <returns>Returns the binary data if successful, else <c>null</c></returns>
    public async Task<byte[]> GetAttachmentByNameAsync(string fileNamePart)
    {
      await ReadAttachmentsAsync().ConfigureAwait(false);
      for (int c = 0; c < Attachments.Count; c++)
        if (Attachments[c].FileName.ToLowerInvariant().StartsWith(fileNamePart))
          return await ExtractAttachmentAsync(c).ConfigureAwait(false);

      return null;
    }

    /// <summary>
    /// Tries to extract an attachment by its index. The index is zero-based and refers to the <see cref="Attachments"/> collection index.
    /// </summary>
    /// <param name="attachmentIndex">Index</param>
    /// <param name="binaryData">Returns the binary data</param>
    /// <returns>True if successful</returns>
    public async Task<byte[]> ExtractAttachmentAsync(int attachmentIndex)
    {
      string tempFileName = Path.GetTempFileName();
      bool success = false;
      // Calling EnsureLocalFileSystemAccess not necessary; access for external process ensured by ExecuteWithResourceAccess
      var arguments = string.Format("attachments \"{0}\" {1}:\"{2}\"", _lfsra.LocalFileSystemPath, attachmentIndex + 1, tempFileName);
      await MKVEXTRACT_THROTTLE_LOCK.WaitAsync().ConfigureAwait(false);
      try
      {
        success = (await _lfsra.ExecuteWithResourceAccessAsync(_mkvExtractPath, arguments, _priorityClass, PROCESS_TIMEOUT_MS).ConfigureAwait(false)).Success;
      }
      catch (TaskCanceledException)
      {
        ServiceRegistration.Get<ILogger>().Warn("MatroskaInfoReader.ExtractAttachment: External process aborted due to timeout: Executable='{0}', Arguments='{1}'", _mkvExtractPath, arguments);
      }
      finally
      {
        MKVEXTRACT_THROTTLE_LOCK.Release();
      }

      if (!success)
        return null;

      long fileSize = _attachments[attachmentIndex].FileSize;
      FileInfo fileInfo = new FileInfo(tempFileName);
      if (!fileInfo.Exists || fileInfo.Length != fileSize)
        return null;

      byte[] binaryData = FileUtils.ReadFile(tempFileName);
      fileInfo.Delete();
      return binaryData;
    }

    #endregion

    #region Private methods

    private static IEnumerable<XElement> GetTagsForTargetType(XDocument doc, int? targetTypeValue)
    {
      if (targetTypeValue.HasValue)
        return from simpleTag in doc.Descendants("Tags").Descendants("Tag")
               let targetsElement = simpleTag.Element("Targets")
               where targetsElement != null && targetsElement.HasElements
               let targetTypeValueElement = targetsElement.Element("TargetTypeValue")
               where targetTypeValueElement != null && Convert.ToInt32(targetTypeValueElement.Value) == targetTypeValue.Value
               select simpleTag;

      return from simpleTag in doc.Descendants("Tags").Descendants("Tag")
             let targetsElement = simpleTag.Element("Targets")
             where !targetsElement.HasElements
             select simpleTag;
    }

    #endregion
  }
}
