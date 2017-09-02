using System.Collections.Generic;

namespace MediaPortal.Plugins.MP2Extended.Common
{
    public class WebFilterOperator
    {
        public WebFilterOperator()
        {
            SuitableTypes = new List<string>();
        }

        public string Operator { get; set; }
        public string Title { get; set; }
        public IList<string> SuitableTypes { get; set; }
    }
}
