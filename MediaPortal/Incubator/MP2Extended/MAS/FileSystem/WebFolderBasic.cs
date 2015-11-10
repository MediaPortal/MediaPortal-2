using MediaPortal.Plugins.MP2Extended.Common;

namespace MediaPortal.Plugins.MP2Extended.MAS.FileSystem
{
    public class WebFolderBasic : WebFilesystemItem
    {
        public override WebMediaType Type
        {
            get
            {
                return WebMediaType.Folder;
            }
        }
    }
}
