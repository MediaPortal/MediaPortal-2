using System.Collections.Generic;

namespace MediaPortal.Extensions.MediaServer.Objects.Basic
{
    public class Container : BaseSomething, IDirectoryContainer
    {
        public Container()
        {
            Resources = new List<IDirectoryResource>();
            CreateClass = new List<IDirectoryCreateClass>();
            SearchClass = new List<IDirectorySearchClass>();
        }

        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Title { get; set; }

        public string Creator { get; set; }

        public IList<IDirectoryResource> Resources { get; set; }

        public string Class { get { return "object.container"; } }

        public bool Restricted { get; set; }

        public string WriteStatus { get; set; }

        public int ChildCount { get; set; }

        public IList<IDirectoryCreateClass> CreateClass { get; set; }

        public IList<IDirectorySearchClass> SearchClass { get; set; }

        public bool Searchable { get; set; }

        public override void Init()
        {
            
        }
    }
}
