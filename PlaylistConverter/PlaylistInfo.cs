using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaylistConverter
{
    public class PlaylistInfo(string name, string creator, int songcount, string? description)
    {
        public string Name { get; set; } = name;
        public string? Description { get; set; } = description != string.Empty ? description : "No description found";
        public string Creator { get; set; } = creator;
        public int SongCount { get; set; } = songcount;
    }
}
