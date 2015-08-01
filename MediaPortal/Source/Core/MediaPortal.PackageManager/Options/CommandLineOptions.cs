#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using CommandLine;
using CommandLine.Text;
using MediaPortal.PackageManager.Options.Admin;
using MediaPortal.PackageManager.Options.Authors;
using MediaPortal.PackageManager.Options.Shared;
using MediaPortal.PackageManager.Options.Users;

namespace MediaPortal.PackageManager.Options
{
  internal class CommandLineOptions
  {
    #region Admin Actions

    [VerbOption("create-user", HelpText = "Create a user account for a package author.")]
    public CreateUserOptions CreateUserVerb { get; set; }

    [VerbOption("revoke-user", HelpText = "Revoke an existing user account (e.g. in case of abuse or malware).")]
    public RevokeUserOptions RevokeUserVerb { get; set; }

    #endregion

    #region Author Actions

    [VerbOption("create", HelpText = "Create a package from a source folder.")]
    public CreateOptions CreateVerb { get; set; }

    [VerbOption("publish", HelpText = "Publish a package to the MediaPortal package server.")]
    public PublishOptions PublishVerb { get; set; }

    [VerbOption("recall", HelpText = "Recall a previously published package.")]
    public RecallOptions RecallVerb { get; set; }

    #endregion

    #region User Actions

    [VerbOption("install", HelpText = "Install, update or remove packages.")]
    public InstallOptions InstallVerb { get; set; }

    [VerbOption("list", HelpText = "List packages.")]
    public ListOptions ListVerb { get; set; }

    #endregion

    #region Other Actions

    [VerbOption("listassemblies", HelpText = "List all assembies and additional files which are used by the tool.")]
    public NoOptions ListAssembliesVerb { get; set; }

    #endregion


    [HelpVerbOption]
    public string GetUsage(string verb)
    {
      return HelpText.AutoBuild(this, verb);
    }

    [ParserState]
    public IParserState LastParserState { get; set; }
  }
}