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

using System;
using System.Linq;
using System.Reflection;
using MediaPortal.Common.PluginManager.Packages.DataContracts.Enumerations;
using MediaPortal.PackageServer.Domain.Entities;
using MediaPortal.PackageServer.Domain.Entities.Enumerations;
using MediaPortal.PackageServer.Domain.Entities.Helpers;
using MediaPortal.PackageServer.Domain.Infrastructure.Context;

namespace MediaPortal.PackageServer.Domain.Infrastructure.ContentProviders
{
  internal class PackageContentProvider : AbstractContentProvider
  {
    private readonly string loremIpsum = "Lorem ipsum dolor sit amet, in sed rebum utamur veritus, molestie percipit sadipscing his et. Mel unum definiebas ei, nec magna dolore salutandi et. No pri decore iudicabit, sea unum feugait consulatu te. Ocurreret interesset eum id, mei numquam ceteros posidonium in. Ius ei cetero placerat consectetuer, eum ut esse voluptatum, ex enim platonem has."
                                         + Environment.NewLine
                                         + "Vim ut sumo dolor suscipit, ad mei alterum omittam gloriatur, has possim erroribus et. In quas mollis prodesset est, ex vim prima expetenda. Assum adipisci ex vim. Ea his movet salutatus conclusionemque, inani docendi fierent cu quo. Insolens reprimique reprehendunt vis in, debet cetero suscipiantur eu ius. Errem delicata praesent qui ex."
                                         + Environment.NewLine
                                         + "Te atqui adolescens usu, ius no tollit referrentur, te pri cetero ornatus eligendi. Te pri epicurei evertitur, no novum commune mandamus his. His illum bonorum perpetua cu, etiam percipit et usu. Solum everti mnesarchum eos ea, nec an brute altera.";

    public PackageContentProvider() : base(40)
    {
    }

    public override void CreateContent(DataContext context)
    {
      var owner = context.Users.First(x => x.Alias == "admin");
      
      // create some sample data to populate the database with.. 
      CreateAbsurdlyNamedPackage(context, owner);
      CreateFarbrauschPackage(context, owner);
      CreateGargleBlasterPackage(context, owner);

      context.SaveChanges();
    }

    private void CreateAbsurdlyNamedPackage(DataContext context, User owner)
    {
      var package = new Package
      {
        Guid = Guid.NewGuid(),
        Name = "AbsurdlyLongNamedPackageWithExtraJazzJustForFun",
        Authors = "Guinnevere McDonald Johnson-Madison, The Bee",
        License = "CC 2.0",
        Description = loremIpsum,
        Created = DateTime.Now.AddMonths(-4).AddDays(-15),
        Modified = DateTime.Now.AddMonths(-1).AddDays(2),
        Owner = owner,
        PackageType = PackageType.Client,
      };
      context.Packages.Add(package);

      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Category.Information));
      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Product.Client));

      var release = new Release
      {
        ApiVersion = 1,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "AbsurdlyNamed-0.0.4-alpha.mp2x",
        PackageSize = 224400,
        Version = "0.0.4-alpha",
        Released = DateTime.Now.AddMonths(-4).AddDays(-15),
        Package = package
      };
      package.Releases.Add(release);
      release = new Release
      {
        ApiVersion = 1,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "AbsurdlyNamed-0.1.0.mp2x",
        PackageSize = int.MaxValue,
        Version = "0.1.0",
        Released = DateTime.Now.AddMonths(-1).AddDays(2),
        Package = package
      };
      package.Releases.Add(release);

      context.SaveChanges();
      package.CurrentRelease = release;
    }

    private void CreateFarbrauschPackage(DataContext context, User owner)
    {
      var package = new Package
      {
        Guid = Guid.NewGuid(),
        Name = "FarbrauschLiquidSkin",
        Authors = "Farbrausch",
        License = "Proprietary with usage rights",
        Description = loremIpsum.Substring(0, loremIpsum.IndexOf(Environment.NewLine)),
        Created = DateTime.Now.AddMonths(-3).AddDays(-8),
        Modified = DateTime.Now.AddMonths(-1).AddDays(-8),
        Owner = owner,
        PackageType = PackageType.Client,
      };
      context.Packages.Add(package);

      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Category.Skins));
      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Category.Plugins));
      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Product.Client));

      var release = new Release
      {
        ApiVersion = 1,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "FarbrauschLiquidSkin-1.0.0.mp2x",
        PackageSize = 1982638,
        Version = "1.0.0",
        Released = DateTime.Now.AddMonths(-3).AddDays(-8),
        Package = package
      };
      package.Releases.Add(release);
      release = new Release
      {
        ApiVersion = 1,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "FarbrauschLiquidSkin-1.0.1.mp2x",
        PackageSize = 201495,
        Version = "1.0.1",
        Released = DateTime.Now.AddMonths(-2).AddDays(-8),
        Package = package
      };
      package.Releases.Add(release);
      release = new Release
      {
        ApiVersion = 1,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "FarbrauschLiquidSkin-1.0.2.mp2x",
        PackageSize = 209345,
        Version = "1.0.3",
        Released = DateTime.Now.AddMonths(-1).AddDays(-8),
        Package = package
      };
      package.Releases.Add(release);
      
      context.SaveChanges();
      package.CurrentRelease = release;

      var review = new Review
      {
        User = owner,
        Created = DateTime.Now.AddDays(-14),
        Rating = 5,
        LanguageCulture = "en-US",
        Title = "Wow!!",
        Body = null
      };
      package.Reviews.Add(review);
      review = new Review
      {
        User = owner,
        Created = DateTime.Now.AddDays(-8),
        Rating = 4,
        LanguageCulture = "da-DK",
        Title = "Fantastisk",
        Body = "Hvis bare det var sandt..!"
      };
      package.Reviews.Add(review);
      review = new Review
      {
        User = owner,
        Created = DateTime.Now.AddDays(-1),
        Rating = 5,
        LanguageCulture = "de-DE",
        Title = "So ein ding...",
        Body = "muss ich auch haben!"
      };
      package.Reviews.Add(review);
    }

    private void CreateGargleBlasterPackage(DataContext context, User owner)
    {
      var package = new Package
      {
        Guid = Guid.NewGuid(),
        Name = "GargleBlaster",
        Authors = "Arthur Dent, Ford Prefect",
        License = "GPL 3.0",
        Description = loremIpsum,
        Created = DateTime.Now.AddMonths(-14).AddDays(-5),
        Modified = DateTime.Now.AddMonths(-2).AddDays(-2),
        Owner = owner,
        PackageType = PackageType.Client,
      };
      context.Packages.Add(package);

      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Category.OtherInputs));
      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Category.News));
      package.Tags.Add(context.Tags.First(x => x.Name == Tags.Product.Client));

      var release = new Release
      {
        ApiVersion = 42,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "GargleBlaster-1.0.mp2x",
        PackageSize = int.MaxValue,
        Version = "First version",
        Released = DateTime.Now.AddMonths(-14).AddDays(-5),
        Package = package
      };
      package.Releases.Add(release);
      release = new Release
      {
        ApiVersion = 42,
        IsAvailable = true,
        Metadata = "",
        PackageFileName = "GargleBlaster-1.2.mp2x",
        PackageSize = int.MaxValue,
        Version = "1.2-ish",
        Released = DateTime.Now.AddMonths(-2).AddDays(-2),
        Package = package
      };
      package.Releases.Add(release);

      context.SaveChanges();
      package.CurrentRelease = release;

      var review = new Review
      {
        User = owner,
        Created = DateTime.Now.AddMonths(-6),
        Rating = 5,
        LanguageCulture = "en-US",
        Title = "Can't remember a thing!",
        Body = "Would it save you a lot of time if I just gave up and went mad now?"
      };
      package.Reviews.Add(review);
    }
  }
}
