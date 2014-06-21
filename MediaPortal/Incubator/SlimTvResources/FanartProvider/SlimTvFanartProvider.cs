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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;
using MediaPortal.LogoManager;
using MediaPortal.LogoManager.Design;
using MediaPortal.Plugins.SlimTv.SlimTvResources.Settings;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Plugins.SlimTv.SlimTvResources.FanartProvider
{
  public class SlimTvFanartProvider : IFanArtProvider
  {
    protected readonly SlimTvLogoSettings _settings;
    protected string _dataFolder;

    public SlimTvFanartProvider()
    {
      _settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvLogoSettings>();
      _dataFolder = ServiceRegistration.Get<IPathManager>().GetPath("<DATA>\\Logos\\");
    }

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
    public bool TryGetFanArt(FanArtConstants.FanArtMediaType mediaType, FanArtConstants.FanArtType fanArtType, string name, int maxWidth, int maxHeight, bool singleRandom, out IList<string> result)
    {
      result = null;
      if (mediaType != FanArtConstants.FanArtMediaType.Channel)
        return false;

      try
      {
        string designsFolder = FileUtils.BuildAssemblyRelativePath("Designs");

        ThemeHandler themeHandler = new ThemeHandler();
        Theme theme = themeHandler.Load(Path.Combine(designsFolder, _settings.LogoTheme));

        string logoFolder = Path.Combine(_dataFolder, string.Format("{0}-{1}", theme.DesignName, theme.ThemeName));
        string logoFileName = Path.Combine(logoFolder, FileUtils.GetSafeFilename(string.Format("{0}.png", name)));

        if (!Directory.Exists(logoFolder))
          Directory.CreateDirectory(logoFolder);

        if (File.Exists(logoFileName))
        {
          result = new[] { logoFileName };
          return true;
        }

        LogoProcessor processor = new LogoProcessor { DesignsFolder = designsFolder };

        // From repository
        using (var repo = new LogoRepository { RepositoryUrl = _settings.RepositoryUrl })
        {
          var stream = repo.Download(name);
          using (stream)
            if (processor.CreateLogo(theme, stream, logoFileName))
            {
              result = new[] { logoFileName };
              return true;
            }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTv Logos: Error processing logo image.", ex);
      }
      return false;
    }
  }
}
