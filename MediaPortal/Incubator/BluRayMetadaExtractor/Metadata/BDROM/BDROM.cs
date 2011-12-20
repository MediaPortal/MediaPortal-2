//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace BDInfo
{
  public class BDROM
  {
    public DirectoryInfo DirectoryRoot = null;
    public DirectoryInfo DirectoryBDMV = null;
    public DirectoryInfo DirectoryBDJO = null;
    public DirectoryInfo DirectoryCLIPINF = null;
    public DirectoryInfo DirectoryPLAYLIST = null;
    public DirectoryInfo DirectorySNP = null;
    public DirectoryInfo DirectorySSIF = null;
    public DirectoryInfo DirectorySTREAM = null;

    public string VolumeLabel = null;
    public ulong Size = 0;
    public bool IsBDPlus = false;
    public bool IsBDJava = false;
    public bool IsDBOX = false;
    public bool IsPSP = false;
    public bool Is3D = false;

    public Dictionary<string, TSPlaylistFile> PlaylistFiles = new Dictionary<string, TSPlaylistFile>();
    public Dictionary<string, TSStreamClipFile> StreamClipFiles = new Dictionary<string, TSStreamClipFile>();
    public Dictionary<string, TSStreamFile> StreamFiles = new Dictionary<string, TSStreamFile>();
    public Dictionary<string, TSInterleavedFile> InterleavedFiles = new Dictionary<string, TSInterleavedFile>();

    public delegate bool OnStreamClipFileScanError(TSStreamClipFile streamClipFile, Exception ex);

    public event OnStreamClipFileScanError StreamClipFileScanError;

    public delegate bool OnStreamFileScanError(TSStreamFile streamClipFile, Exception ex);

    public event OnStreamFileScanError StreamFileScanError;

    public delegate bool OnPlaylistFileScanError(TSPlaylistFile playlistFile, Exception ex);

    public event OnPlaylistFileScanError PlaylistFileScanError;


    public static string[] EXT_CLPI = new string[] { "clpi", "CLPI", "CPI" };
    public static string[] EXT_BDMV = new string[] { "bdmv", "BDMV", "BDM" };
    public static string[] EXT_M2TS = new string[] { "m2ts", "M2TS", "MTS" };
    public static string[] EXT_MPLS = new string[] { "mpls", "MPLS", "MPL" };
    public static string[] EXT_SSIF = new string[] { "ssif", "SSIF" };

    public BDROM(string path)
      : this(path, true)
    { }

    public BDROM(string path, bool autoScan)
    {
      //
      // Locate BDMV directories.
      //
      DirectoryBDMV = GetDirectoryBDMV(path);

      if (DirectoryBDMV == null)
        throw new Exception("Unable to locate BD structure.");

      //
      // Initialize basic disc properties.
      //
      DirectoryRoot = DirectoryBDMV.Parent;
      VolumeLabel = GetVolumeLabel(DirectoryRoot);
      DirectoryCLIPINF = GetDirectory("CLIPINF", DirectoryBDMV, 0);
      DirectoryPLAYLIST = GetDirectory("PLAYLIST", DirectoryBDMV, 0);

      if (DirectoryCLIPINF == null || DirectoryPLAYLIST == null)
        throw new Exception("Unable to locate BD structure.");

      // Following methods can be very time consuming over our MP2 remote resource access (many files accesses)
      if (!autoScan)
        return;

      Size = (ulong) GetDirectorySize(DirectoryRoot);
      DirectoryBDJO = GetDirectory("BDJO", DirectoryBDMV, 0);
      DirectorySNP = GetDirectory("SNP", DirectoryRoot, 0);
      IsBDPlus = GetDirectory("BDSVM", DirectoryRoot, 0) != null ||  GetDirectory("SLYVM", DirectoryRoot, 0) != null ||  GetDirectory("ANYVM", DirectoryRoot, 0) != null;
      IsBDJava = DirectoryBDJO != null && DirectoryBDJO.GetFiles().Length > 0;
      IsPSP = DirectorySNP != null && (DirectorySNP.GetFiles("*.mnv").Length > 0 || DirectorySNP.GetFiles("*.MNV").Length > 0);
      IsDBOX = DirectoryRoot != null && File.Exists(Path.Combine(DirectoryRoot.FullName, "FilmIndex.xml"));

      //
      // Initialize file lists.
      //
      ScanPlaylists();
      ScanStreams();
      ScanStreamClipFiles();
      ScanInterleavedFiles();
    }

    public bool ScanPlaylists()
    {
      FileInfo[] files;
      if (!GetFiles(DirectoryPLAYLIST, EXT_MPLS, out files))
        return false;
      foreach (FileInfo file in files)
        PlaylistFiles.Add(file.Name.ToUpper(), new TSPlaylistFile(this, file));
      return true;
    }

    public bool ScanStreams()
    {
      DirectorySTREAM = GetDirectory("STREAM", DirectoryBDMV, 0);
      FileInfo[] files;
      if (!GetFiles(DirectorySTREAM, EXT_M2TS, out files))
        return false;
      foreach (FileInfo file in files)
        StreamFiles.Add(file.Name.ToUpper(), new TSStreamFile(file));
      return true;
    }

    public bool ScanStreamClipFiles()
    {
      FileInfo[] files;
      if (!GetFiles(DirectoryCLIPINF, EXT_CLPI, out files))
        return false;
      foreach (FileInfo file in files)
        StreamClipFiles.Add(file.Name.ToUpper(), new TSStreamClipFile(file));
      return true;
    }

    public bool ScanInterleavedFiles()
    {
      DirectorySSIF = GetDirectory("SSIF", DirectorySTREAM, 0);
      Is3D = DirectorySSIF != null && DirectorySSIF.GetFiles().Length > 0;
      FileInfo[] files;
      if (!GetFiles(DirectorySSIF, EXT_CLPI, out files))
        return false;
      foreach (FileInfo file in files)
        InterleavedFiles.Add(file.Name.ToUpper(), new TSInterleavedFile(file));
      return true;
    }

    public bool GetFiles(DirectoryInfo directoryInfo, string[] validExtensions, out FileInfo[] fileInfos)
    {
      fileInfos = null;
      if (directoryInfo == null || validExtensions == null)
        return false;
      foreach (string ext in validExtensions)
      {
        fileInfos = directoryInfo.GetFiles("*." + ext);
        if (fileInfos.Length != 0)
          return true;
      }
      return false;
    }

    public void Scan()
    {
      List<TSStreamClipFile> errorStreamClipFiles = new List<TSStreamClipFile>();
      foreach (TSStreamClipFile streamClipFile in StreamClipFiles.Values)
      {
        try
        {
          streamClipFile.Scan();
        }
        catch (Exception ex)
        {
          errorStreamClipFiles.Add(streamClipFile);
          if (StreamClipFileScanError != null)
          {
            if (StreamClipFileScanError(streamClipFile, ex))
              continue;
            break;
          }
          throw;
        }
      }

      foreach (TSStreamFile streamFile in StreamFiles.Values)
      {
        string ssifName = Path.GetFileNameWithoutExtension(streamFile.Name) + ".SSIF";
        if (InterleavedFiles.ContainsKey(ssifName))
          streamFile.InterleavedFile = InterleavedFiles[ssifName];
      }

      TSStreamFile[] streamFiles = new TSStreamFile[StreamFiles.Count];
      StreamFiles.Values.CopyTo(streamFiles, 0);
      Array.Sort(streamFiles, CompareStreamFiles);

      List<TSPlaylistFile> errorPlaylistFiles = new List<TSPlaylistFile>();
      foreach (TSPlaylistFile playlistFile in PlaylistFiles.Values)
      {
        try
        {
          playlistFile.Scan(StreamFiles, StreamClipFiles);
        }
        catch (Exception ex)
        {
          errorPlaylistFiles.Add(playlistFile);
          if (PlaylistFileScanError != null)
          {
            if (PlaylistFileScanError(playlistFile, ex))
              continue;
            break;
          }
          throw;
        }
      }

      List<TSStreamFile> errorStreamFiles = new List<TSStreamFile>();
      foreach (TSStreamFile streamFile in streamFiles)
      {
        try
        {
          List<TSPlaylistFile> playlists = new List<TSPlaylistFile>();
          foreach (TSPlaylistFile playlist in PlaylistFiles.Values)
            foreach (TSStreamClip streamClip in playlist.StreamClips)
              if (streamClip.Name == streamFile.Name)
              {
                playlists.Add(playlist);
                break;
              }
          streamFile.Scan(playlists, false);
        }
        catch (Exception ex)
        {
          errorStreamFiles.Add(streamFile);
          if (StreamFileScanError != null)
          {
            if (StreamFileScanError(streamFile, ex))
              continue;
            break;
          }
          throw;
        }
      }

      foreach (TSPlaylistFile playlistFile in PlaylistFiles.Values)
        playlistFile.Initialize();
    }

    private static DirectoryInfo GetDirectoryBDMV(string path)
    {
      DirectoryInfo dir = new DirectoryInfo(path);
      while (dir != null)
      {
        if (dir.Name == "BDMV")
          return dir;
        dir = dir.Parent;
      }
      return GetDirectory("BDMV", new DirectoryInfo(path), 0);
    }

    private static DirectoryInfo GetDirectory(string name, DirectoryInfo dir, int searchDepth)
    {
      if (dir != null)
      {
        DirectoryInfo[] children = dir.GetDirectories();
        foreach (DirectoryInfo child in children.Where(child => child.Name == name))
          return child;

        if (searchDepth > 0)
          foreach (DirectoryInfo child in children)
            GetDirectory(name, child, searchDepth - 1);
      }
      return null;
    }

    private static long GetDirectorySize(DirectoryInfo directoryInfo)
    {
      FileInfo[] pathFiles = directoryInfo.GetFiles();
      long size = pathFiles.Where(pathFile => pathFile.Extension.ToUpper() != ".SSIF").Sum(pathFile => pathFile.Length);

      DirectoryInfo[] pathChildren = directoryInfo.GetDirectories();
      size += pathChildren.Sum(pathChild => GetDirectorySize(pathChild));
      return size;
    }

    private static string GetVolumeLabel(DirectoryInfo dir)
    {
      uint serialNumber = 0;
      uint maxLength = 0;
      uint volumeFlags = new uint();
      StringBuilder volumeLabel = new StringBuilder(256);
      StringBuilder fileSystemName = new StringBuilder(256);
      string label = "";

      try
      {
        GetVolumeInformation(
          dir.Name,
          volumeLabel,
          (uint) volumeLabel.Capacity,
          ref serialNumber,
          ref maxLength,
          ref volumeFlags,
          fileSystemName,
          (uint) fileSystemName.Capacity);

        label = volumeLabel.ToString();
      }
      catch { }

      if (label.Length == 0)
        label = dir.Name;

      return label;
    }

    public static int CompareStreamFiles(TSStreamFile x, TSStreamFile y)
    {
      // TODO: Use interleaved file sizes
      if ((x == null || x.FileInfo == null) && (y == null || y.FileInfo == null))
        return 0;
      if ((x == null || x.FileInfo == null))
        return 1;
      if ((y == null || y.FileInfo == null))
        return -1;
      if (x.FileInfo.Length > y.FileInfo.Length)
        return 1;
      if (y.FileInfo.Length > x.FileInfo.Length)
        return -1;
      return 0;
    }

    [DllImport("kernel32.dll")]
    private static extern long GetVolumeInformation(
        string pathName,
        StringBuilder volumeNameBuffer,
        uint volumeNameSize,
        ref uint volumeSerialNumber,
        ref uint maximumComponentLength,
        ref uint fileSystemFlags,
        StringBuilder fileSystemNameBuffer,
        uint fileSystemNameSize);
  }
}
