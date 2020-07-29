using System;
using System.Collections.Generic;
using System.Text;

namespace Audio_Convertor.AudioJson
{
    public class Tags
    {
        public string major_brand { get; set; }
        public string minor_version { get; set; }
        public string compatible_brands { get; set; }
        public DateTime creation_time { get; set; }
        public string genre { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public string album_artist { get; set; }
        public string album { get; set; }
        public string comment { get; set; }
        public string copyright { get; set; }
        public string date { get; set; }
    }

    public class Format
    {
        public string filename { get; set; }
        public int nb_streams { get; set; }
        public int nb_programs { get; set; }
        public string format_name { get; set; }
        public string format_long_name { get; set; }
        public string start_time { get; set; }
        public string duration { get; set; }
        public string size { get; set; }
        public string bit_rate { get; set; }
        public int probe_score { get; set; }
        public Tags tags { get; set; }
    }

    public class AudioFormat
    {
        public Format format { get; set; }
    }
}
