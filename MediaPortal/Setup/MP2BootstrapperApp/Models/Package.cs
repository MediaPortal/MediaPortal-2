using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace MP2BootstrapperApp.Models
{
  public class Package
  {
    public string Name { get; set; }
    
    public string ImagePath { get; set; }
    
    public string InstalledVersion { get; set; }
    
    public string BundleVersion { get; set; }
    
    public RequestState RequestState { get; set; } 
    
    public PackageState PackageState { get; set; }
  }
}
