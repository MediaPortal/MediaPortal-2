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

using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.BundlePackages;
using MP2BootstrapperApp.BundlePackages.Features;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using WixToolset.Dtf.WindowsInstaller;
using WixToolset.Mba.Core;

namespace MP2BootstrapperApp.Models
{
  /// <summary>
  /// Model class for the <see cref="MP2BootstrapperApplication"/>
  /// </summary>
  public class BootstrapperApplicationModel : IBootstrapperApplicationModel
  {
    private IntPtr _hwnd;

    public BootstrapperApplicationModel(IBootstrapperApp bootstreApplication)
    {
      BootstrapperApplication = bootstreApplication;
      _hwnd = IntPtr.Zero;
      ComputeBundlePackages();
      WireUpEventHandlers();
    }

    public IBootstrapperApp BootstrapperApplication { get; }

    public IntPtr WindowHandle
    {
      get { return _hwnd; }
    }

    public int FinalResult { get; set; }

    public ReadOnlyCollection<IBundlePackage> BundlePackages { get; private set; }

    public DetectionState DetectionState { get; set; } = DetectionState.Absent;

    public IBundleMsiPackage MainPackage { get; protected set; }

    public bool IsDowngrade { get; set; }

    public IPlan ActionPlan { get; set; }

    public InstallState InstallState { get; set; } = InstallState.Initializing;

    public bool Cancelled { get; set; }

    public void SetWindowHandle(Window view)
    {
      _hwnd = new WindowInteropHelper(view).EnsureHandle();
    }

    public void PlanAction(IPlan actionPlan)
    {
      if (actionPlan == null)
        throw new ArgumentNullException(nameof(actionPlan), $"{nameof(PlanAction)} cannot be called with a null IPlan");

      InstallState = InstallState.Planning;
      ActionPlan = actionPlan;
      SetVariables(actionPlan.GetVariables());
      BootstrapperApplication.Plan(actionPlan.PlannedAction);
    }

    public void ApplyAction()
    {
      InstallState = InstallState.Applying;
      BootstrapperApplication.Apply(_hwnd);
    }

    public void LogMessage(LogLevel logLevel, string message)
    {
      BootstrapperApplication.Log(logLevel, message);
    }

    protected void SetVariables(IEnumerable<KeyValuePair<string, string>> variables)
    {
      foreach (KeyValuePair<string, string> variable in variables)
        BootstrapperApplication.Engine.SetVariableString(variable.Key, variable.Value, false);
    }

    private void DetectBegin(object sender, DetectBeginEventArgs e)
    {
      InstallState = InstallState.Detecting;
      DetectionState = e.RegistrationType == RegistrationType.Full ? DetectionState.Present : DetectionState.Absent;
    }

    private void DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
      if (BootstrapperApplication.Engine.CompareVersions(BootstrapperApplication.BundleVersion, e.Version) < 1)
        IsDowngrade = true;
    }

    protected void DetectPackageComplete(object sender, DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      UpdatePackageCurrentState(detectPackageCompleteEventArgs);
    }

    private void UpdatePackageCurrentState(DetectPackageCompleteEventArgs detectPackageCompleteEventArgs)
    {
      if (Enum.TryParse(detectPackageCompleteEventArgs.PackageId, out PackageId detectedPackageId))
      {
        IBundlePackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId);
        if (bundlePackage != null)
        {
          bundlePackage.CurrentInstallState = ApplyIncorrectPackageStateWorkaround(detectPackageCompleteEventArgs.State);
          // Installation will fail if trying to install an obsolete version, catch this early and mark this bundle as a downgrade
          if (bundlePackage.PackageId == MainPackage.PackageId && bundlePackage.CurrentInstallState == PackageState.Obsolete)
            IsDowngrade = true;

          // Evaluate any install condition, defaulting to true if no condition has been specified
          bundlePackage.EvaluatedInstallCondition = !string.IsNullOrEmpty(bundlePackage.InstallCondition) ? BootstrapperApplication.EvaluateCondition(bundlePackage.InstallCondition) : true;

          // Set the InstalledVersion of the package, this may get set elsewhere, e.g. in DetectRelatedMsiPackage, so don't overwrite if present
          if (bundlePackage.InstalledVersion == null)
          {
            // For msi packages that are present the installed version must be the same as the bundled version. This
            // is not necessarily the case for exe packages which use manual version checks and may be detected as present
            // if a higher or lower compatible version is detected on the system.
            if (bundlePackage is IBundleMsiPackage && bundlePackage.CurrentInstallState == PackageState.Present)
              bundlePackage.InstalledVersion = bundlePackage.Version;
            // Else for exe packages the bundle should have defined a version variable in the form [PackageId]_Version as part
            // of detecting previous installations of the exe package, try and use this as the version.
            else
              bundlePackage.InstalledVersion = GetCurrentPackageVersionFromVariables(detectPackageCompleteEventArgs.PackageId);
          }
        }
      }
    }

    /// <summary>
    /// There is possibly a bug in the WixToolset.Mba.Core package where the <see cref="PackageState"/> enums don't align
    /// with what the burn engine passes on the native side. It appears that <see cref="PackageState.Cached"/> was removed
    /// on the native side but not on the managed side so the managed side values are one less than the native side for all
    /// states above and including <see cref="PackageState.Cached"/>. This method manually bumps those enum values up by one
    /// for now. Issue is open here and awaiting response as of 13/4/23 - https://github.com/wixtoolset/issues/issues/7399
    /// </summary>
    /// <param name="packageState"></param>
    /// <returns></returns>
    PackageState ApplyIncorrectPackageStateWorkaround(PackageState packageState)
    {
      if (packageState == PackageState.Cached)
        return PackageState.Present;
      if (packageState == PackageState.Present)
        return PackageState.Superseded;
      return packageState;
    }

    private void DetectMsiFeature(object sender, DetectMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        IBundleMsiPackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId) as IBundleMsiPackage;
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.Id == e.FeatureId);
        if (bundleFeature != null)
          bundleFeature.CurrentFeatureState = e.State;
      }
    }

    private void DetectRelatedMsiPackage(object sender, DetectRelatedMsiPackageEventArgs e)
    {
      if (e.Operation == RelatedOperation.MajorUpgrade)
      {
        IBundleMsiPackage bundledPackage = BundlePackages.FirstOrDefault(p => p.Id == e.PackageId) as IBundleMsiPackage;
        if (bundledPackage != null)
        {
          ProductInstallation installedPackage = new ProductInstallation(e.ProductCode);
          bundledPackage.InstalledVersion = e.Version;
          foreach (FeatureInstallation feature in installedPackage.Features)
          {
            IBundlePackageFeature bundleFeature = bundledPackage.Features.FirstOrDefault(f => f.Id == feature.FeatureName);
            if (bundleFeature != null)
              bundleFeature.PreviousVersionInstalled = feature.State == WixToolset.Dtf.WindowsInstaller.InstallState.Local;
          }
        }        
      }
    }

    private void DetectComplete(object sender, DetectCompleteEventArgs e)
    {
      InstallState = Hresult.Succeeded(e.Status) ? InstallState.Waiting : InstallState.Failed;
    }

    protected void PlanPackageBegin(object sender, PlanPackageBeginEventArgs planPackageBeginEventArgs)
    {
      UpdatePackageRequestState(planPackageBeginEventArgs);
    }

    private void UpdatePackageRequestState(PlanPackageBeginEventArgs planPackageBeginEventArgs)
    {
      IBundlePackage bundlePackage = BundlePackages.FirstOrDefault(p => p.Id == planPackageBeginEventArgs.PackageId);
      // Packages not present in BundlePackages are bootstrapper prerequisite packages (currently only .net),
      // We shouldn't ever need to take action on these packages, they are already installed before the bootstrapper
      // app is run and repairing .net takes a long time, often require a restart, and is unlikely to be the cause
      // of issues with the MediaPortal 2 installation.
      if (bundlePackage == null)
      {
        planPackageBeginEventArgs.State = RequestState.None;
        return;
      }

      RequestState? requestState = ActionPlan.GetRequestedInstallState(bundlePackage);
      if (requestState.HasValue)
        planPackageBeginEventArgs.State = requestState.Value;
    }

    private void PlanMsiFeature(object sender, PlanMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        IBundleMsiPackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId) as IBundleMsiPackage;
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.Id == e.FeatureId);
        if (bundleFeature != null)
        {
          FeatureState? featureState = ActionPlan.GetRequestedInstallState(bundleFeature);
          if (featureState.HasValue)
            e.State = featureState.Value;
        }
      }
    }

    private void PlanRelatedBundle(object sender, PlanRelatedBundleEventArgs e)
    {
      // When installing always remove related bundles, this works around an issue with the Burn engine
      // not removing installed bundles when installing a different bundle with an identical version number.
      if (ActionPlan.PlannedAction == LaunchAction.Install)
        e.State = RequestState.Absent;
    }

    private void PlanComplete(object sender, PlanCompleteEventArgs e)
    {
      if (!Cancelled && Hresult.Succeeded(e.Status))
      {
        InstallState = InstallState.Applying;
        ApplyAction();
      }
      else
      {
        InstallState = InstallState.Failed;
      }
    }

    protected void ApplyComplete(object sender, ApplyCompleteEventArgs e)
    {
      FinalResult = e.Status;
      InstallState = Hresult.Succeeded(e.Status) ? InstallState.Applied : InstallState.Failed;
    }

    //private void ResolveSource(object sender, ResolveSourceEventArgs e)
    //{
    //  if (!string.IsNullOrEmpty(e.DownloadSource))
    //  {
    //    e.Result = Result.Download;
    //  }
    //  else
    //  {
    //    e.Result = Result.Ok;
    //  }
    //}

    private string GetCurrentPackageVersionFromVariables(string packageId)
    {
      string versionVariableName = packageId + "_Version";
      if (!BootstrapperApplication.Engine.ContainsVariable(versionVariableName))
        return null;
      return BootstrapperApplication.Engine.GetVariableString(versionVariableName);
    }

    private void WireUpEventHandlers()
    {
      BootstrapperApplication.DetectBegin += DetectBegin;
      BootstrapperApplication.DetectRelatedBundle += DetectRelatedBundle;
      BootstrapperApplication.DetectPackageComplete += DetectPackageComplete;
      BootstrapperApplication.DetectMsiFeature += DetectMsiFeature;
      BootstrapperApplication.DetectRelatedMsiPackage += DetectRelatedMsiPackage;
      BootstrapperApplication.DetectComplete += DetectComplete;
      BootstrapperApplication.ApplyComplete += ApplyComplete;
      BootstrapperApplication.PlanPackageBegin += PlanPackageBegin;
      BootstrapperApplication.PlanMsiFeature += PlanMsiFeature;
      BootstrapperApplication.PlanRelatedBundle += PlanRelatedBundle;
      BootstrapperApplication.PlanComplete += PlanComplete;
      //BootstrapperApplication.ResolveSource += ResolveSource;
    }

    private void ComputeBundlePackages()
    {
      IList<IBundlePackage> packages = new List<IBundlePackage>();

      string manifestPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (manifestPath != null)
      {
        const string bootstrapperApplicationData = "BootstrapperApplicationData";
        const string xmlExtension = ".xml";
        string bootstrapperDataFilePath = Path.Combine(manifestPath, bootstrapperApplicationData + xmlExtension);

        string xml;
        using (StreamReader reader = new StreamReader(bootstrapperDataFilePath))
          xml = reader.ReadToEnd();

        BundlePackageFactory bundlePackageFactory = new BundlePackageFactory(new AssemblyFeatureMetadataProvider());
        packages = bundlePackageFactory.CreatePackagesFromXmlString(xml);
      }

      IBundleMsiPackage mainPackage = packages.FirstOrDefault(p => p.PackageId == PackageId.MediaPortal2) as IBundleMsiPackage;
      if (mainPackage == null)
        throw new InvalidOperationException($"BootstrapperApplicationData.xml does not contain the main installation package with id {PackageId.MediaPortal2}");

      MainPackage = mainPackage;

      BundlePackages = packages != null
        ? new ReadOnlyCollection<IBundlePackage>(packages)
        : new ReadOnlyCollection<IBundlePackage>(new List<IBundlePackage>());
    }
  }
}
