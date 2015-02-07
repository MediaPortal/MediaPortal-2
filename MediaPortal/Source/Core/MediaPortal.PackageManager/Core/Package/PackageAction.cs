#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MediaPortal.PackageManager.Core.Package
{
  /// <summary>
  /// Abstract base class for all package actions.
  /// </summary>
  public abstract class PackageAction
  {
    /// <summary>
    /// Gets the action type name of this action.
    /// </summary>
    public string ActionType
    {
      get
      {
        var attr = GetType().GetCustomAttributes(typeof(PackageActionAttribute), false).FirstOrDefault() as PackageActionAttribute;
        return attr != null ? attr.Type : GetType().Name;
      }
    }

    /// <summary>
    /// Gets the description text of this action
    /// </summary>
    /// <remarks>The description is optional. By default it's printed to the context output before the action is executed.</remarks>
    public string Description { get; private set; }

    /// <summary>
    /// Parses the parameters from an XML element
    /// </summary>
    /// <param name="xAction">XML element of the action.</param>
    /// <param name="setOverwrite">If this actions performs a write action, and setOvewrite is <c>true</c>, then the action should overwrite always.
    /// If setOverwrite is <c>false</c> The default behavior or the XML parameters value should be used.</param>
    /// <remarks>
    /// The parameter <param name="setOverwrite"> is true, if the install action is used as update action.</param>
    /// Normally this method is not overwritten. Override <see cref="OnSetDefaultParameterValues(bool)"/> and
    /// <see cref="OnParseParameter(string, string, bool)"/> instead.
    /// </remarks>
    public virtual void ParseParameters(XElement xAction, bool setOverwrite)
    {
      OnSetDefaultParameterValues(setOverwrite);

      foreach (var xAttribute in xAction.Attributes())
      {
        if (!OnParseParameter(xAttribute.Name.LocalName, xAttribute.Value, setOverwrite))
        {
          throw new PackageParseException(
            String.Format("The attribute '{0}' is not supported for Action of type {1}", xAttribute.Name.LocalName, (string)xAction.Attribute("Type")),
            xAttribute);
        }
      }
    }

    /// <summary>
    /// Sets all parameters to default values.
    /// </summary>
    /// <param name="setOverwrite">If this actions performs a write action, and setOvewrite is <c>true</c>, then the action should overwrite always.
    /// If setOverwrite is <c>false</c> The default behavior or the XML parameters value should be used.</param>
    /// <remarks>
    /// Override this method to set all parameters th their default value.
    /// </remarks>
    protected virtual void OnSetDefaultParameterValues(bool setOverwrite)
    {
      Description = String.Empty;
    }

    /// <summary>
    /// Parses a parameter value and assigns it.
    /// </summary>
    /// <param name="name">Parameter name.</param>
    /// <param name="value">Invariant string representation of the parameter value.</param>
    /// <param name="setOverwrite">If this actions performs a write action, and setOvewrite is <c>true</c>, then the action should overwrite always.
    /// If setOverwrite is <c>false</c> The default behavior or the XML parameters value should be used.</param>
    /// <returns>Returns <c>true</c> if the parameter is known, or <c>false</c> If the parameter is unknown.</returns>
    /// <remarks>
    /// Override this method to parse parameter values.
    /// If the parameter is unknown, the base class must be called.
    /// </remarks>
    protected virtual bool OnParseParameter(string name, string value, bool setOverwrite)
    {
      switch (name)
      {
        case "Type":
          // ignore type
          return true;

        case "Description":
          Description = value;
          return true;

        default:
          return false;
      }
    }

    /// <summary>
    /// Checks if the action parameters are valid
    /// </summary>
    /// <param name="packageRoot">Package root</param>
    /// <param name="message">Message which describes why the action is invalid</param>
    /// <returns></returns>
    public abstract bool CheckValid(PackageRoot packageRoot, out string message);

    /// <summary>
    /// Print the start message of the action to the context output before the action is executed.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <remarks>When overridden the output can be customized. By default the <see cref="Description"/> or the 'Executing action [TypeName]' is printed.</remarks>
    protected virtual void PrintStartAction(ActionContext context)
    {
      context.Print(Description ?? String.Format("Executing action {0}", ActionType));
    }

    /// <summary>
    /// Print the finish message of the action to the context output after the action is executed.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <remarks>When overridden the output can be customized. By default nothing is printed.</remarks>
    protected virtual void PrintFinishAction(ActionContext context)
    { }

    /// <summary>
    /// Executes the action.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <remarks>Normally this method is not overridden. Override <see cref="DoExecute(ActionContext)"/> instead.</remarks>
    public virtual void Execute(ActionContext context)
    {
      PrintStartAction(context);
      DoExecute(context);
      PrintFinishAction(context);
    }

    /// <summary>
    /// Performs the actual execution of the action
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <remarks>By default only this method needs to be overridden.</remarks>
    protected abstract void DoExecute(ActionContext context);
  }

  /// <summary>
  /// Collection of <see cref="PackageAction"/>s.
  /// </summary>
  public class PackageActionCollection : Collection<PackageAction>
  { }

  /// <summary>
  /// Delegate for printing action messages.
  /// </summary>
  /// <param name="text">Text to print.</param>
  public delegate void PrintDelegate(string text);

  /// <summary>
  /// Context for execution of package actions.
  /// </summary>
  public class ActionContext
  {
    private readonly PrintDelegate _printCallback;
    private readonly IDictionary<string, string> _registredPaths;

    public ActionContext(PackageRoot packageRoot, PackageInstallType installyType, PrintDelegate printCallback, IDictionary<string, string> registredPaths)
    {
      _printCallback = printCallback;
      _registredPaths = registredPaths;
      PackageRoot = packageRoot;
      InstallType = installyType;
    }

    /// <summary>
    /// Prints a message to the log, console or whatever print target is attached
    /// </summary>
    /// <param name="format">Format of the message.</param>
    /// <param name="args">Arguments for the format string.</param>
    public void Print(string format, params string[] args)
    {
      if (format == null) throw new ArgumentNullException("format");
      if (args == null) throw new ArgumentNullException("args");

      if (_printCallback != null) 
        _printCallback(String.Format(format, args));
    }

    /// <summary>
    /// Gets the package instance.
    /// </summary>
    public PackageRoot PackageRoot { get; private set; }

    /// <summary>
    /// Gets the type of installation.
    /// </summary>
    public PackageInstallType InstallType { get; private set; }

    /// <summary>
    /// Resolves an named path.
    /// </summary>
    /// <param name="name">Name of the path.</param>
    /// <returns>Returns the full path or <c>null</c> if the name is unknown.</returns>
    public string GetPath(string name)
    {
      string path;
      if (_registredPaths.TryGetValue(name, out path))
        return path;
      return null;
    }
  }

  /// <summary>
  /// Package install types
  /// </summary>
  public enum PackageInstallType
  {
    /// <summary>
    /// Install package.
    /// </summary>
    Install,

    /// <summary>
    /// Update package.
    /// </summary>
    Update,

    /// <summary>
    /// Uninstall package.
    /// </summary>
    Uninstall
  }


  /// <summary>
  /// Attribute for package action implementations.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, Inherited = false)]
  public class PackageActionAttribute : Attribute
  {
    /// <summary>
    /// Creates a new instance of the attribute
    /// </summary>
    /// <param name="type">Type name of the Action.</param>
    public PackageActionAttribute(string type)
    {
      Type = type;
    }

    /// <summary>
    /// Gets the type name of the action.
    /// </summary>
    public string Type { get; private set; }
  }


  /// <summary>
  /// Base class for file copy actions.
  /// </summary>
  /// <remarks>Provides basic file and directory copy functions with optional printing to the context output.</remarks>
  public abstract class CopyBasePackageAction : PackageAction
  {
    /// <summary>
    /// Gets if this action overwrites the target if it already exists.
    /// </summary>
    public bool OverwriteTarget { get; protected set; }

    protected override void OnSetDefaultParameterValues(bool setOverwrite)
    {
      base.OnSetDefaultParameterValues(setOverwrite);

      OverwriteTarget = setOverwrite;
    }

    protected override bool OnParseParameter(string name, string value, bool setOverwrite)
    {
      switch (name)
      {
        case "OverwriteTarget":
          if (!setOverwrite)
          {
            OverwriteTarget = Boolean.Parse(value);
          }
          return true;

        default:
          return base.OnParseParameter(name, value, setOverwrite);
      }
    }

    /// <summary>
    /// Copies a file and optionally prints to the context output.
    /// </summary>
    /// <param name="context">Execution context.</param>
    /// <param name="source">Source file path.</param>
    /// <param name="target">Target file path.</param>
    /// <param name="overwrite"><c>true</c> if the target should be overwritten, <c>false</c> if the operation should fail when the target already exists.</param>
    /// <param name="print"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void CopyFile(ActionContext context, string source, string target, bool overwrite, bool print)
    {
      if (print)
      {
        context.Print("Copying file {0} to {1}", source, target);
      }
      try
      {
        File.Copy(source, target, overwrite);
      }
      catch (IOException ex)
      {
        if (print)
        {
          context.Print("Failed: {0}", ex.Message);
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
    /// <param name="print"><c>true</c> if a copy message should be printed to the context output.</param>
    public static void CopyDirectory(ActionContext context, string source, string target, bool overwrite, bool topLevelDirectoryCanExist, bool recursive, bool print)
    {
      if (print)
      {
        context.Print("Copying directory {0} to {1}", source, target);
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
          string fileTarget = Path.Combine(target, Path.GetFileName(file));
          if (print)
          {
            context.Print("Copying file {0} to {1}", file, fileTarget);
          }
          CopyFile(context, file, fileTarget, overwrite, false);
        }
      }
      catch (IOException ex)
      {
        if (print)
        {
          context.Print("Failed: {0}", ex.Message);
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
          if (print)
          {
            context.Print("Failed: {0}", ex.Message);
          }
          throw;
        }
        foreach (var directory in directories)
        {
          CopyDirectory(context, directory, Path.Combine(target, Path.GetFileName(directory)), overwrite, false, true, print);
        }
      }
    }
  }
}