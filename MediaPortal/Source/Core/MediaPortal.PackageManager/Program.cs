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
using MediaPortal.Common.Logging;
using MediaPortal.PackageManager.Core;
using MediaPortal.PackageManager.Options;
using MediaPortal.PackageManager.Options.Authors;
using MediaPortal.PackageManager.Options.Shared;
using MediaPortal.PackageManager.Options.Users;

namespace MediaPortal.PackageManager
{
	public class Program
	{
		private static readonly ILogger LOG = new BasicConsoleLogger(LogLevel.All);

		public static void Main( string[] args )
		{
			try
			{
 				var options = new CommandLineOptions();
				var parser = new CommandLine.Parser(with => with.HelpWriter = Console.Out);
			  parser.ParseArgumentsStrict( args, options, Dispatcher );
			}
			catch( Exception ex )
			{
				LOG.Error( ex.Message );
			}
		}

    private static void Dispatcher( string verb, object options )
    {
      if( string.IsNullOrEmpty( verb ) || options == null )
        return; // invalid verb or no options

      Operation operation;
      if( !Enum.TryParse( verb.Replace( "-", "" ), true, out operation ) )
        return; // unknown operation

      switch( operation )
      {
        case Operation.CreateUser:
        case Operation.RevokeUser:
          PackageAdmin.Dispatch( LOG, operation, options );
          return;
        case Operation.Create:
          PackageBuilder.Dispatch( LOG, operation, options );          
          return;
        case Operation.Publish:
        case Operation.Recall:
          PackagePublisher.Dispatch( LOG, operation, options );
          return;
        default:
          PackageInstaller.Dispatch( LOG, operation, options );
          return;
      }
    }
	}
}
