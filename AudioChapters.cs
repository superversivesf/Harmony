using System;
using System.Collections.Generic;
using System.Text;

namespace Audio_Convertor.ChaptersJson
{
    public class Tags
    {
        public string title { get; set; }
    }

    public class Chapter
    {
        public int id { get; set; }
        public string time_base { get; set; }
        public object start { get; set; }
        public string start_time { get; set; }
        public object end { get; set; }
        public string end_time { get; set; }
        public Tags tags { get; set; }
    }

    public class AudioChapters
    {
        public IList<Chapter> chapters { get; set; }
    }
}
