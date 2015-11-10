namespace MediaPortal.Plugins.MP2Extended.TAS.Tv
{
    public class WebDiskSpaceInformation
    {
        public string Disk { get; set; }
        public float Size { get; set; } // in GiB
        public float Available { get; set; } // in GiB
        public float Used { get; set; } // in GiB
        public float PercentageUsed { get; set; }
    }
}
