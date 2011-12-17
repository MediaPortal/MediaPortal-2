#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider
{
  public class NetworkNeighborhoodResourceProvider : IBaseResourceProvider
  {
    #region Consts

    public const string NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID_STR = "{03DD2DA6-4DA8-4D3E-9E55-80E3165729A3}";
    public static readonly Guid NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID = new Guid(NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID_STR);

    protected const string RES_RESOURCE_PROVIDER_NAME = "[NetworkNeighborhoodResourceProvider.Name]";
    protected const string RES_RESOURCE_PROVIDER_DESCRIPTION = "[NetworkNeighborhoodResourceProvider.Description]";

    #endregion

    #region Protected fields

    protected ResourceProviderMetadata _metadata;
    protected LocalFsResourceProvider _localFsProvider;

    #endregion

    #region Ctor

    public NetworkNeighborhoodResourceProvider()
    {
      _metadata = new ResourceProviderMetadata(NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, RES_RESOURCE_PROVIDER_NAME, RES_RESOURCE_PROVIDER_DESCRIPTION, false);
    }

    #endregion

    #region Protected members

    protected LocalFsResourceProvider LocalFsResourceProvider
    {
      get { return LocalFsResourceProvider.Instance; }
    }

    #endregion

    #region IBaseResourceProvider implementation

    public ResourceProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public bool IsResource(string path)
    {
      return NetworkNeighborhoodResourceAccessor.IsResource(path);
    }

    public IResourceAccessor CreateResourceAccessor(string path)
    {
      if (!IsResource(path))
        throw new ArgumentException(string.Format("Unable to access resource '{0}'", path));
      return new NetworkNeighborhoodResourceAccessor(this, path);
    }

    public ResourcePath ExpandResourcePathFromString(string pathStr)
    {
      if (string.IsNullOrEmpty(pathStr))
        return null;
      // The input string is given by the user. We can cope with three formats:
      // 1) A resource provider path which can be interpreted by the choosen resource provider itself (i.e. a path without the
      //    starting resource provider GUID)
      // 2) A resource path in the resource path syntax (i.e. {[Base-Provider-Id]}://[Base-Provider-Path])
      // 3) A dos path
      if (IsResource(pathStr))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(_metadata.ResourceProviderId, pathStr, true), 
          });
      string providerPath = LocalFsResourceProviderBase.ToProviderPath(pathStr);
      if (IsResource(providerPath))
        return new ResourcePath(new ProviderPathSegment[]
          {
              new ProviderPathSegment(_metadata.ResourceProviderId, providerPath, true), 
          });
      try
      {
        return ResourcePath.Deserialize(pathStr);
      }
      catch (ArgumentException)
      {
        return null;
      }
    }

    #endregion
  }
}
