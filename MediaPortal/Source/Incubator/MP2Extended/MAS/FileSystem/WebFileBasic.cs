using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.MAS.FileSystem
{
    public class WebFileBasic : WebFilesystemItem
    {
        public long Size { get; set; }

        public override WebMediaType Type
        {
            get
            {
                return WebMediaType.File;
            }
        }
    }
}
