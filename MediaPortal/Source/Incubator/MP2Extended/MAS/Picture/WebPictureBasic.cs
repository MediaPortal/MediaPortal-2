using System;
using System.Collections.Generic;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.General;

namespace MediaPortal.Plugins.MP2Extended.MAS.Picture
{
    public class WebPictureBasic : WebMediaItem, IDateAddedSortable, IPictureDateTakenSortable, ICategorySortable
    {
        public WebPictureBasic()
        {
            DateTaken = new DateTime(1970, 1, 1);
            Categories = new List<WebCategory>();
        }

        public IList<WebCategory> Categories { get; set; }
        public DateTime DateTaken { get; set; }

        public override WebMediaType Type
        {
            get
            {
                return WebMediaType.Picture;
            }
        }

        public override string ToString()
        {
            return Title;
        }
    }
}