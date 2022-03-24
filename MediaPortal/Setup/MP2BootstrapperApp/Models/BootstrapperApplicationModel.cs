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

using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using MP2BootstrapperApp.ActionPlans;
using MP2BootstrapperApp.BootstrapperWrapper;
using MP2BootstrapperApp.ChainPackages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Xml.Linq;

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

    protected void SetVariables(IEnumerable<KeyValuePair<string, object>> variables)
    {
      foreach (KeyValuePair<string, object> variable in variables)
      {
        if (variable.Value is SecureString secureStringValue)
          BootstrapperApplication.SecureStringVariables[variable.Key] = secureStringValue;
        else if (variable.Value is string stringValue)
          BootstrapperApplication.StringVariables[variable.Key] = stringValue;
        else if (variable.Value is Version versionValue)
          BootstrapperApplication.VersionVariables[variable.Key] = versionValue;
        else if (variable.Value is long numericValue)
          BootstrapperApplication.NumericVariables[variable.Key] = numericValue;
        else
          LogMessage(LogLevel.Error, $"Unable to set variable {variable.Key}, variables of type {variable.Value?.GetType().Name ?? "null"} are not supported");
      }
    }

    private void DetectBegin(object sender, DetectBeginEventArgs e)
    {
      InstallState = InstallState.Detecting;
      DetectionState = e.Installed ? DetectionState.Present : DetectionState.Absent;
    }

    private void DetectRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
    {
      DetectionState = e.Operation == RelatedOperation.Downgrade ? DetectionState.Older : DetectionState.Newer;
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
          bundlePackage.CurrentInstallState = detectPackageCompleteEventArgs.State;

          // Evaluate any install condition, defaulting to true if no condition has been specified
          bundlePackage.EvaluatedInstallCondition = !string.IsNullOrEmpty(bundlePackage.InstallCondition) ? BootstrapperApplication.EvaluateCondition(bundlePackage.InstallCondition) : true;

          // Set the InstalledVersion of the package, this may get set elsewhere, e.g. in DetectRelatedMsiPackage, so don't overwrite if present
          if (bundlePackage.InstalledVersion == null)
          {
            // For msi packages that are present the installed version must be the same as the bundled version. This
            // is not necessarily the case for exe packages which use manual version checks and may be detected as present
            // if a higher or lower compatible version is detected on the system.
            if (bundlePackage is IBundleMsiPackage && detectPackageCompleteEventArgs.State == PackageState.Present)
              bundlePackage.InstalledVersion = bundlePackage.Version;
            // Else for exe packages the bundle should have defined a version variable in the form [PackageId]_Version as part
            // of detecting previous installations of the exe package, try and use this as the version.
            else
              bundlePackage.InstalledVersion = GetCurrentPackageVersionFromVariables(detectPackageCompleteEventArgs.PackageId);
          }
        }
      }
    }

    private void DetectMsiFeature(object sender, DetectMsiFeatureEventArgs e)
    {
      if (Enum.TryParse(e.PackageId, out PackageId detectedPackageId))
      {
        IBundleMsiPackage bundlePackage = BundlePackages.FirstOrDefault(pkg => pkg.PackageId == detectedPackageId) as IBundleMsiPackage;
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.FeatureName == e.FeatureId);
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
            IBundlePackageFeature bundleFeature = bundledPackage.Features.FirstOrDefault(f => f.FeatureName == feature.FeatureName);
            if (bundleFeature != null)
              bundleFeature.PreviousVersionInstalled = feature.State == Microsoft.Deployment.WindowsInstaller.InstallState.Local;
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
        IBundlePackageFeature bundleFeature = bundlePackage?.Features.FirstOrDefault(f => f.FeatureName == e.FeatureId);
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

    private void ResolveSource(object sender, ResolveSourceEventArgs e)
    {
      if (!string.IsNullOrEmpty(e.DownloadSource))
      {
        e.Result = Result.Download;
      }
      else
      {
        e.Result = Result.Ok;
      }
    }

    private Version GetCurrentPackageVersionFromVariables(string packageId)
    {
      string versionVariableName = packageId + "_Version";
      if (BootstrapperApplication.VersionVariables.Contains(versionVariableName))
        return BootstrapperApplication.VersionVariables[versionVariableName];
      else if (BootstrapperApplication.StringVariables.Contains(versionVariableName))
        return Version.TryParse(BootstrapperApplication.StringVariables[versionVariableName], out Version version) ? version : new Version();
      return null;
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
      BootstrapperApplication.ResolveSource += ResolveSource;
    }

    private void ComputeBundlePackages()
    {
      IList<IBundlePackage> packages = new List<IBundlePackage>();

      XNamespace manifestNamespace = "http://schemas.microsoft.com/wix/2010/BootstrapperApplicationData";

      string manifestPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
      if (manifestPath != null)
      {
        const string bootstrapperApplicationData = "BootstrapperApplicationData";
        const string xmlExtension = ".xml";
        string bootstrapperDataFilePath = Path.Combine(manifestPath, bootstrapperApplicationData + xmlExtension);
        XElement bundleManifestData;

        using (StreamReader reader = new StreamReader(bootstrapperDataFilePath))
        {
          string xml = reader.ReadToEnd();
          XDocument xDoc = XDocument.Parse(xml);
          bundleManifestData = xDoc.Element(manifestNamespace + bootstrapperApplicationData);
        }

        const string wixMbaPrereqInfo = "WixMbaPrereqInformation";
        IList<string> mbaPrereqPackages = bundleManifestData?.Descendants(manifestNamespace + wixMbaPrereqInfo)
          .Select(x => x.Attribute("PackageId")?.Value)
          .ToList();

        BundlePackageFactory bundlePackageFactory = new BundlePackageFactory();

        const string wixPackageProperties = "WixPackageProperties";
        packages = bundleManifestData?.Descendants(manifestNamespace + wixPackageProperties)
          .Where(x => mbaPrereqPackages.All(preReq => preReq != x.Attribute("Package")?.Value))
          .Select(x => bundlePackageFactory.CreatePackage(x)).ToList();

        const string wixPackageFeatureInfo = "WixPackageFeatureInfo";
        IEnumerable<IBundlePackageFeature> features = bundleManifestData?.Descendants(manifestNamespace + wixPackageFeatureInfo)
          .Select(x => bundlePackageFactory.CreatePackageFeature(x));
        foreach (IBundlePackageFeature feature in features)
        {
          IBundleMsiPackage parent = packages.FirstOrDefault(p => p.Id == feature.Package) as IBundleMsiPackage;
          if (parent != null)
            parent.Features.Add(feature);
        }
      }

      BundlePackages = packages != null
        ? new ReadOnlyCollection<IBundlePackage>(packages)
        : new ReadOnlyCollection<IBundlePackage>(new List<IBundlePackage>());
    }
  }
}
