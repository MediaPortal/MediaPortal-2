namespace MediaPortal.Extensions.MediaServer.Objects.Basic
{
    public class GenreContainer : Container, IDirectoryGenre
    {
        public string Genre { get; set; }

        public new string Class
        {
            get { return "object.container.genre"; }
        }

        public string LongDescription { get; set; }

        public string Description { get; set; }
    }
}
