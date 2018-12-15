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
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.LogoManager;
using MediaPortal.LogoManager.ChannelManagerService;
using MediaPortal.LogoManager.Design;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Common.FanArt;
using MediaPortal.Plugins.SlimTv.Interfaces.Settings;

namespace MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider
{
  public class SlimTvFanartProvider : IFanArtProvider
  {
    protected readonly SlimTvLogoSettings _settings;
    protected string _dataFolder;
    protected RegionInfo _country;
    // Allow cached logos to be updated every 2 weeks
    protected TimeSpan MAX_CACHE_DURATION = TimeSpan.FromDays(14);

    private readonly string _designsFolder;

    public SlimTvFanartProvider()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvLogoSettings>();
      _dataFolder = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>\\Logos\\");
      var currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _country = new RegionInfo(currentCulture.Name);
    }

    public SlimTvFanartProvider(string designsFolder) 
      : this()
    {
      _designsFolder = designsFolder;
    }

    public FanArtProviderSource Source { get { return FanArtProviderSource.File; } }

    /// <summary>
    /// Gets a list of <see cref="FanArtImage"/>s for a requested <paramref name="mediaType"/>, <paramref name="fanArtType"/> and <paramref name="name"/>.
    /// The name can be: Series name, Actor name, Artist name depending on the <paramref name="mediaType"/>.
    /// </summary>
    /// <param name="mediaType">Requested FanArtMediaType</param>
    /// <param name="fanArtType">Requested FanArtType</param>
    /// <param name="name">Requested name of Series, Actor, Artist...</param>
    /// <param name="maxWidth">Maximum width for image. <c>0</c> returns image in original size.</param>
    /// <param name="maxHeight">Maximum height for image. <c>0</c> returns image in original size.</param>
    /// <param name="singleRandom">If <c>true</c> only one random image URI will be returned</param>
    /// <param name="result">Result if return code is <c>true</c>.</param>
    /// <returns><c>true</c> if at least one match was found.</returns>
    public bool TryGetFanArt(string mediaType, string fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<IResourceLocator> result)
    {
      result = null;
      if (mediaType != FanArtMediaTypes.ChannelTv && mediaType != FanArtMediaTypes.ChannelRadio)
        return false;

      try
      {
        string designsFolder = _designsFolder ?? FileUtils.BuildAssemblyRelativePath("Designs");

        ChannelType logoChannelType = mediaType == FanArtMediaTypes.ChannelTv ? ChannelType.Tv : ChannelType.Radio;
        ThemeHandler themeHandler = new ThemeHandler();
        Theme theme = themeHandler.Load(Path.Combine(designsFolder, _settings.LogoTheme));

        string logoFolder = Path.Combine(_dataFolder, string.Format("{0}-{1}-{2}", logoChannelType, theme.DesignName, theme.ThemeName));
        string logoFileName = Path.Combine(logoFolder, FileUtils.GetSafeFilename(string.Format("{0}.png", name)));

        if (!Directory.Exists(logoFolder))
          Directory.CreateDirectory(logoFolder);

        if (File.Exists(logoFileName) && IsCacheValid(theme, logoFileName))
          return BuildLogoResourceLocatorAndReturn(ref result, logoFileName);

        LogoProcessor processor = new LogoProcessor { DesignsFolder = designsFolder };

        // From repository
        using (var repo = new LogoRepository { RepositoryUrl = _settings.RepositoryUrl })
        {
          var stream = repo.Download(name, logoChannelType, _country.TwoLetterISORegionName);
          if (stream == null)
            return BuildLogoResourceLocatorAndReturn(ref result, logoFileName);
          using (stream)
          {
            // First download and process the new logo, but keep the existing file if something fails.
            string tmpLogoFileName = Path.ChangeExtension(logoFileName, ".tmplogo");
            if (processor.CreateLogo(theme, stream, tmpLogoFileName))
            {
              if (File.Exists(logoFileName))
                File.Delete(logoFileName);
              File.Move(tmpLogoFileName, logoFileName);
            }
            return BuildLogoResourceLocatorAndReturn(ref result, logoFileName);
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTv Logos: Error processing logo image.", ex);
      }
      return false;
    }

    private static bool BuildLogoResourceLocatorAndReturn(ref IList<IResourceLocator> result, string logoFileName)
    {
      if (!File.Exists(logoFileName))
        return false;
      result = new List<IResourceLocator> { new ResourceLocator(ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, logoFileName)) };
      return true;
    }

    /// <summary>
    /// Checks if the cached logo is still valid, if it is too old it will be re-downloaded. Logo themes
    /// can prevent this behavior by setting the <see cref="Theme.SkipOnlineUpdate"/> to <c>true</c>.
    /// </summary>
    /// <param name="theme">Current logo theme</param>
    /// <param name="logoFileName">Cached logo name</param>
    /// <returns><c>true</c> if logo exists and cache duration is valid</returns>
    private bool IsCacheValid(Theme theme, string logoFileName)
    {
      FileInfo fi = new FileInfo(logoFileName);
      return theme.SkipOnlineUpdate || DateTime.Now - fi.CreationTime <= MAX_CACHE_DURATION;
    }
  }
}
