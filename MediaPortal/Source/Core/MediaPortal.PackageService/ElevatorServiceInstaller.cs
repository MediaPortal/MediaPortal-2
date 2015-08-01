using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace MediaPortal.PackageService
{
  [RunInstaller(true)]
  public class ElevatorServiceInstaller : Installer
  {
    /// <summary>
    /// Public Constructor for WindowsServiceInstaller.
    /// - Put all of your Initialization code here.
    /// </summary>
    public ElevatorServiceInstaller()
    {
      ServiceProcessInstaller serviceProcessInstaller =
        new ServiceProcessInstaller();
      ServiceInstaller serviceInstaller = new ServiceInstaller();

      //# Service Account Information
      serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
      serviceProcessInstaller.Username = null;
      serviceProcessInstaller.Password = null;

      //# Service Information
      serviceInstaller.DisplayName = "MediaPortal2 Package Manager Service";
      serviceInstaller.StartType = ServiceStartMode.Automatic;

      //# This must be identical to the WindowsService.ServiceBase name
      //# set in the constructor of WindowsService.cs
      serviceInstaller.ServiceName = "MP2PackageManager";

      Installers.Add(serviceProcessInstaller);
      Installers.Add(serviceInstaller);
    }
  }
}