using Audio_Convertor.AudioJson;
using Audio_Convertor.ChaptersJson;
using Audio_Convertor.StreamsJson;

namespace Audio_Convertor
{
    internal class AAXInfo
    {
        public AudioFormat Format { get; set; }
        public AudioChapters Chapters { get; set; }
        public AudioStreams Streams { get; set; }

        public AAXInfo(AudioFormat audioFormat, AudioChapters audioChapters, AudioStreams audioStreams)
        {
            Format = audioFormat;
            Chapters = audioChapters;
            Streams = audioStreams;
        }
    }
}