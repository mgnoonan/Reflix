using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Reflix.Models
{
    public class MovieTitle
    {
        [Required]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Synopsis { get; set; }
        public List<MoviePerson> Cast { get; set; }
        public List<MoviePerson> Directors { get; set; }
        public List<string> Genres { get; set; }
        public string BoxArt { get; set; }
        public int ReleaseYear { get; set; }
        public string Rating { get; set; }
        public int Runtime { get; set; }
    }
}
