namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebChannelGroup
    {
        public string GroupName { get; set; }
        public int Id { get; set; }
        public bool IsChanged { get; set; }
        public int SortOrder { get; set; }
        public bool IsRadio { get; set; }
        public bool IsTv { get; set; }
    }
}
