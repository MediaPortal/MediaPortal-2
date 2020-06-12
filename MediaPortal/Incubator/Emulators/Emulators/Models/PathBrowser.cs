using MediaPortal.Common.ResourceAccess;
using MediaPortal.UiComponents.SkinBase.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Models
{
  public class PathBrowser : PathBrowserService
  {
    public PathBrowser()
    {
      _enumerateFiles = true;
      ShowSystemResources = false;
      _validatePathDlgt = IsPathValid;
    }

    protected bool IsPathValid(ResourcePath resourcePath)
    {
      string chosenPath = LocalFsResourceProviderBase.ToDosPath(resourcePath);
      return DosPathHelper.GetFileName(chosenPath) != null;
    }

    public void UpdatePathTree()
    {
      UpdateResourceProviderPathTree();
    }
  }
}
