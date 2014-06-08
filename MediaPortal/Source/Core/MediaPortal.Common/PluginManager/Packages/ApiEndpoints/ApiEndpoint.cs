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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MediaPortal.Common.PluginManager.Packages.ApiEndpoints
{
  public class ApiEndpoint
  {
    // ReSharper disable once StaticFieldInGenericType
    private static readonly string[] EMPTY_PARAMETERS_ARRAY = new string[0];
    public HttpMethod HttpMethod { get; set; }
    public string UrlBasePath { get; set; }
    public string UrlResourcePath { get; set; }
    public string[] UrlParameterNames { get; set; }
    public Type ResultType { get; set; }
    public Type PostQueryType { get; set; }

    public bool HasResult
    {
      get { return ResultType != null; }
    }

    public string UrlFormat
    {
      get { return UrlBasePath + "/" + UrlResourcePath; }
    }

    public string GetUrl(object parameterValues = null)
    {
      if (parameterValues == null)
        return UrlFormat;
      var parameterDictionary = parameterValues.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .ToDictionary(propertyInfo => propertyInfo.Name, propertyInfo => propertyInfo.GetValue(parameterValues));
      return GetUrl(parameterDictionary);
    }

    public string GetUrl(IDictionary<string, object> parameterValues)
    {
      var url = UrlFormat;
      foreach (var parameterName in UrlParameterNames)
      {
        if (!parameterValues.ContainsKey(parameterName))
          throw new ArgumentException(string.Format("Missing required URL parameter '{0}' for resource '{1}'.", parameterName, UrlFormat));
        url = Regex.Replace(url, "{" + parameterName + "(:.*)?}", parameterValues[parameterName].ToString());
      }
      if (url.Contains("{"))
        throw new ArgumentException(string.Format("Invalid URL '{0}' generated, check the API definition for resource '{1}'.", url, UrlFormat));
      return url;
    }

    private ApiEndpoint(HttpMethod httpMethod, string basePath, string resourcePathFormat, string[] urlParameterNames, Type queryType = null, Type resultType = null)
    {
      HttpMethod = httpMethod;
      UrlBasePath = basePath;
      UrlResourcePath = resourcePathFormat;
      UrlParameterNames = urlParameterNames ?? EMPTY_PARAMETERS_ARRAY;
      PostQueryType = queryType;
      ResultType = resultType;
    }

    public static ApiEndpoint Get<TResult>(string basePath, string resourcePathFormat, string[] urlParameterNames = null)
    {
      return new ApiEndpoint(HttpMethod.Get, basePath, resourcePathFormat, urlParameterNames, null, typeof(TResult));
    }

    public static ApiEndpoint Post<TQuery>(string basePath, string resourcePathFormat, string[] urlParameterNames = null)
    {
      return new ApiEndpoint(HttpMethod.Post, basePath, resourcePathFormat, urlParameterNames, typeof(TQuery));
    }

    public static ApiEndpoint Post<TQuery, TResult>(string basePath, string resourcePathFormat, string[] urlParameterNames = null)
    {
      return new ApiEndpoint(HttpMethod.Post, basePath, resourcePathFormat, urlParameterNames, typeof(TQuery), typeof(TResult));
    }
  }
}