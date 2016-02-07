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
using System.Collections;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Plugins.MediaServer.Parser;
using NUnit.Framework;

namespace Test.MediaServer
{
    [TestFixture]
    public class TestParser
    {
        [Test]
        public void TestUPnPSimple()
        {
            string text = "dc:title contains \"one\"";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(MediaAspect.ATTR_TITLE, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%one%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPTitle()
        {
            string text = "(dc:title contains \"two\")";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(MediaAspect.ATTR_TITLE, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%two%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPAlbumContains()
        {
            string text = "(upnp:class = \"object.container.album.musicAlbum\" and dc:title contains \"three\")";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(AudioAspect.ASPECT_ID, (ICollection)types, "Types");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(AudioAspect.ATTR_ALBUM, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%three%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPSongArtistContains()
        {
            string text = "(upnp:class derivedfrom \"object.item.audioItem\" and (dc:creator contains \"four\" or upnp:artist contains \"five \\\" six\"))";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(AudioAspect.ASPECT_ID, (ICollection)types, "Types");

            Assert.IsTrue(filter is BooleanCombinationFilter, "Top level");
            BooleanCombinationFilter topFilter = filter as BooleanCombinationFilter;
            Assert.AreEqual(BooleanOperator.Or, topFilter.Operator, "Top level operator");

            Assert.IsTrue(topFilter.Operands is IList<IFilter>, "Top level");
            IList<IFilter> operands = (IList<IFilter>)topFilter.Operands;

            Assert.IsTrue(operands[0] is LikeFilter, "First operand");
            LikeFilter filter1 = (LikeFilter)operands[0];
            Assert.AreEqual(AudioAspect.ATTR_ARTISTS, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%four%", filter1.Expression, "Expression");

            Assert.IsTrue(operands[0] is LikeFilter, "Second operand");
            LikeFilter filter2 = (LikeFilter)operands[1];
            Assert.AreEqual(AudioAspect.ATTR_ARTISTS, filter2.AttributeType, "Attribute");
            Assert.AreEqual("%five \" six%", filter2.Expression, "Expression");
        }

        [Test]
        public void TestUPnPArtistContains()
        {
            string text = "(upnp:class = \"object.container.person.musicArtist\" and dc:title contains \"seven\")";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(AudioAspect.ASPECT_ID, (ICollection)types, "Types");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(AudioAspect.ATTR_ARTISTS, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%seven%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPVideoContains()
        {
            string text = "(upnp:class derivedfrom \"object.item.videoItem\" and dc:title contains \"eight\")";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(VideoAspect.ASPECT_ID, (ICollection)types, "Types");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(MediaAspect.ATTR_TITLE, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%eight%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPTrackContains()
        {
            string text = "(upnp:class derivedfrom \"object.item.audioItem\" and dc:title contains \"nine\")";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(AudioAspect.ASPECT_ID, (ICollection)types, "Types");

            Assert.IsTrue(filter is LikeFilter, "Filter");
            LikeFilter filter1 = filter as LikeFilter;
            Assert.AreEqual(MediaAspect.ATTR_TITLE, filter1.AttributeType, "Attribute");
            Assert.AreEqual("%nine%", filter1.Expression, "Expression");
        }

        [Test]
        public void TestUPnPImage()
        {
            string text = "upnp:class derivedfrom \"object.item.imageItem\" and @refID exists false";
            SearchExp exp = SearchParser.Parse(text);
            IList<Guid> types = new List<Guid>();
            IFilter filter = SearchParser.Convert(exp, types);
            Console.WriteLine(text + " -> filter=" + filter + " types=[" + string.Join(",", types) + "]");

            Assert.Contains(ImageAspect.ASPECT_ID, (ICollection)types, "Types");
        }
    }
}
