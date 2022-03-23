#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Security;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.BootstrapperWrapper
{
  public interface IBootstrapperApp
  {
    LaunchAction LaunchAction { get; }
    Display Display { get; }
    string[] CommandLineArguments { get; }

    void Detect();
    void Plan(LaunchAction action);
    void Apply(IntPtr hwndParent);
    void Log(LogLevel level, string message);
    bool EvaluateCondition(string condition);

    /// <summary>
    /// Gets or sets string variables using <see cref="SecureString"/>.
    /// </summary>
    IVariables<SecureString> SecureStringVariables { get; }

    /// <summary>
    /// Gets or sets numeric variables.
    /// </summary>
    IVariables<long> NumericVariables { get; }

    /// <summary>
    /// Gets or sets version variables.
    /// </summary>
    IVariables<Version> VersionVariables { get; }

    /// <summary>
    /// Gets or sets string variables.
    /// </summary>
    IVariables<string> StringVariables { get; }

    /// <summary>
    /// Formats the specified string by expanding any variable references to their values.
    /// </summary>
    /// <param name="format">The string to format.</param>
    /// <returns>The formatted string.</returns>
    string FormatString(string format);

    event EventHandler<DetectBeginEventArgs> DetectBegin;
    event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;
    event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;
    event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;
    event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;
    event EventHandler<DetectCompleteEventArgs> DetectComplete;
    event EventHandler<PlanBeginEventArgs> PlanBegin;
    event EventHandler<PlanCompleteEventArgs> PlanComplete;
    event EventHandler<ApplyCompleteEventArgs> ApplyComplete;
    event EventHandler<ApplyBeginEventArgs> ApplyBegin;
    event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;
    event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;
    event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;
    event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;
    event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;
    event EventHandler<ResolveSourceEventArgs> ResolveSource;
    event EventHandler<ApplyPhaseCountArgs> ApplyPhaseCount;
    event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;
    event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;
    event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;
    event EventHandler<ErrorEventArgs> Error;
  }
}
