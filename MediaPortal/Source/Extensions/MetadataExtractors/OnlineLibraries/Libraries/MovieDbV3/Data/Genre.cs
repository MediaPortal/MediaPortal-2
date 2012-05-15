using System.Runtime.Serialization;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data
{
//{
//      "id": 28,
//      "name": "Action"
//  },
  [DataContract]
  public class Genre
  {
    [DataMember(Name = "id")]
    public int Id { get; set; }
    
    [DataMember(Name = "name")]
    public string Name { get; set; }
    
    public override string ToString()
    {
      return Name;
    }
  }
}
