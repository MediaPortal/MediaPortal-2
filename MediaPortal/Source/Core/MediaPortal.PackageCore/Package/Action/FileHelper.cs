using System;
using System.IO;
using System.Linq;

namespace MediaPortal.PackageCore.Package.Action
{
  public static class FileHelper
  {
    /// <summary>
    /// Copies a file and optionally prints to the context output.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="overwrite"><c>true</c> if the target should be overwritten, <c>false</c> if the operation should fail when the target already exists.</param>
    /// <param name="doLog"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void CopyFile(InstallContext context, string source, string target, bool overwrite, bool doLog)
    {
      if (doLog)
      {
        context.LogInfo("Copying file {0} to {1}", source, target);
      }
      try
      {
        File.Copy(source, target, overwrite);
      }
      catch (IOException ex)
      {
        if (doLog)
        {
          context.Log.Error("Failed", ex);
        }
        throw;
      }
    }

    /// <summary>
    /// Deletes a file and optionally prints to the context output.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="failIfNotExists"><c>true</c> if an error should be thrown if the file does not exist.</param>
    /// <param name="doLog"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void DeleteFile(InstallContext context, string target, bool failIfNotExists, bool doLog)
    {
      if (doLog)
      {
        context.LogInfo("Deleting file {0}", target);
      }
      try
      {
        if (File.Exists(target))
        {
          File.Delete(target);
        }
        else if (failIfNotExists)
        {
          throw new FileNotFoundException(String.Format("The file '{0}' does not exist", target));
        }
      }
      catch (IOException ex)
      {
        if (doLog)
        {
          context.Log.Error("Failed", ex);
        }
        throw;
      }
    }

    /// <summary>
    /// Copies a directory and optionally prints to the context output.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="source">Source directory path.</param>
    /// <param name="target">Target directory path.</param>
    /// <param name="overwrite"><c>true</c> if the target should be overwritten, <c>false</c> if the operation should fail when the target already exists.</param>
    /// <param name="topLevelDirectoryCanExist">When <paramref name="overwrite"/> is <c>false</c> and <paramref name="topLevelDirectoryCanExist"/> is true, then no error is thrown if the top level directory already exists.</param>
    /// <param name="recursive"><c>true</c> if all sub directories should be copied recursively; <c>false</c> if only the files in the directory should be copied.</param>
    /// <param name="doLog"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void CopyDirectory(InstallContext context, string source, string target, bool overwrite, bool topLevelDirectoryCanExist, bool recursive, bool doLog)
    {
      if (doLog)
      {
        context.LogInfo("Copying directory {0} to {1}", source, target);
      }
      try
      {
        if (Directory.Exists(target))
        {
          if (!overwrite && !topLevelDirectoryCanExist)
          {
            throw new IOException("The target directory does already exist");
          }
        }
        else
        {
          Directory.CreateDirectory(target);
        }
        foreach (var file in Directory.GetFiles(source))
        {
          string fileName = Path.GetFileName(file);
          string fileTarget = Path.Combine(target, fileName);
          if (doLog)
          {
            context.LogInfo("Copying file {0}", fileName);
          }
          CopyFile(context, file, fileTarget, overwrite, false);
        }
      }
      catch (IOException ex)
      {
        if (doLog)
        {
          context.LogError("Failed", ex);
        }
        throw;
      }
      if (recursive)
      {
        string[] directories;
        try
        {
          directories = Directory.GetDirectories(source);
        }
        catch (IOException ex)
        {
          if (doLog)
          {
            context.LogError("Failed", ex);
          }
          throw;
        }
        foreach (var directory in directories)
        {
          // ReSharper disable once AssignNullToNotNullAttribute
          CopyDirectory(context, directory, Path.Combine(target, Path.GetFileName(directory)), overwrite, false, true, doLog);
        }
      }
    }

    /// <summary>
    /// Deletes a file and optionally prints to the context output.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="skipInFotEmpty">Skip this directory if it is not empty.</param>
    /// <param name="failIfNotExists"><c>true</c> if an error should be thrown if the file does not exist.</param>
    /// <param name="doLog"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void DeleteDirectory(InstallContext context, string target, bool skipInFotEmpty, bool failIfNotExists, bool doLog)
    {
      if (doLog)
      {
        context.LogInfo("Deleting directory {0}", target);
      }
      try
      {
        if (Directory.Exists(target))
        {
          if ((Directory.GetFiles(target).Any() || Directory.GetDirectories(target).Any()) && skipInFotEmpty)
          {
            context.LogInfo("Directory is not empty: skip");
            return;
          }
          Directory.Delete(target, true);
        }
        else if (failIfNotExists)
        {
          throw new FileNotFoundException(String.Format("The file '{0}' does not exist", target));
        }
      }
      catch (IOException ex)
      {
        if (doLog)
        {
          context.Log.Error("Failed", ex);
        }
        throw;
      }
    }
  }
}