using System;
using System.Collections.Generic;
using System.Text;
using Audio_Convertor.AudioJson;
using Audio_Convertor.ChaptersJson;
using Audio_Convertor.StreamsJson;
using FFMpegCore;
using FFMpegCore.Helpers;
using Instances;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using TagLib;
using Xabe.FFmpeg.Downloader;

namespace Audio_Convertor
{
    class M4AudioConvertor
    {

        private int bitrate;
        private bool quietMode;
        private string inputFolder;
        private string outputFolder;
        private string storageFolder;
        private string workingFolder;

        public M4AudioConvertor(int bitrate, bool quietMode, string inputFolder, string outputFolder, string storageFolder, string workingFolder)
        {
            this.bitrate = bitrate;
            this.quietMode = quietMode;
            this.inputFolder = inputFolder;
            this.outputFolder = outputFolder;
            this.storageFolder = storageFolder;
            this.workingFolder = workingFolder;
        }

        internal void Execute()
        {
            var _logger = new Logger(this.quietMode);

            FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = ".", TempDirectory = "." });
            _logger.Write("Fetching Latest FFMpeg ...  ");

            var fetchTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
            while (!fetchTask.IsCompleted)
            {
                _logger.AdvanceSpinnder();
                Thread.Sleep(50);
            }

            _logger.WriteLine("\bDone");

            _logger.Write("Checking folders and purging working files ... ");
            CheckFolders();
            _logger.WriteLine("Done");
            var filePaths = Directory.GetFiles(this.inputFolder, "*.m4?").ToList();
            _logger.WriteLine($"Found {filePaths.Count} m4a/b files to process\n");
            ProcessM4Files(filePaths);
        }

        private void ProcessM4Files(List<string> filePaths)
        {
            foreach (var f in filePaths)
            {
                ProcessM4File(f);
            }
        }

        private void ProcessM4File(string f)
        {
            var _logger = new Logger(this.quietMode);
            var _storageFolder = this.storageFolder;

            _logger.WriteLine($"Processing {Path.GetFileName(f)}");
            var _aaxInfo = GetM4Info(f);

            // Write out relevant stats
            _logger.WriteLine($"Title: {CleanTitle(_aaxInfo.Format.format.tags.title)}");
            _logger.WriteLine($"Author(s): {_aaxInfo.Format.format.tags.artist}");

            double _duration = Double.Parse(_aaxInfo.Format.format.duration);
            int h = (int)_duration / 3600;
            int m = ((int)_duration - h * 3600) / 60;
            int s = ((int)_duration - h * 3600 - m * 60);

            _logger.WriteLine($"Length: {h.ToString("D2")}:{m.ToString("D2")}:{s.ToString("D2")}");
            _logger.WriteLine($"Chapters: {_aaxInfo.Chapters.chapters.Count}");

            var _intermediateFile = ProcessToMP3(f, _aaxInfo);
            var _coverFile = GenerateCover(f);
            var _outputDirectory = ProcessChapters(_intermediateFile, _aaxInfo, _coverFile);

            if (System.IO.File.Exists(_coverFile))
            {
                _logger.Write("Moving Cover file ... ");
                var _coverFileDestination = Path.Combine(_outputDirectory, "Cover.jpg");
                System.IO.File.Move(_coverFile, _coverFileDestination);
                _logger.WriteLine("Done");
            }
            else
            {
                _logger.WriteLine ("Cover file not found");
            }

            _logger.Write("Moving M4 file to storage ... ");
            var _storageFile = Path.Combine(_storageFolder, Path.GetFileName(f));
            System.IO.File.Move(f, _storageFile);
            _logger.WriteLine("Done");

            // Cleanup 
            _logger.Write("Cleaning up intermediate files ... ");
            System.IO.File.Delete(_intermediateFile);
            _logger.WriteLine("Done\n");

            //Console.WriteLine(instance.OutputData);
            //// https://github.com/inAudible-NG/tables

        }

        private void PurgeOutputDirectory(string outputDirectoy)
        {
            if (Directory.Exists(outputDirectoy))
            {
                System.IO.DirectoryInfo di = new DirectoryInfo(outputDirectoy);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
        }

        private string GenerateCover(string f)
        {
            var _logger = new Logger(this.quietMode);
            var _filePath = f;

            var _ffmpeg = FFMpegOptions.Options.FFmpegBinary();

            var _coverFile = Path.Combine(this.workingFolder, "Cover.jpg");

            _logger.Write("Writing Cover File ... ");

            var _arguments = $" -i \"{_filePath}\" -an -codec:v copy \"{_coverFile}\"";
            var _instance = new Instance(_ffmpeg, _arguments) { DataBufferCapacity = int.MaxValue };
            _instance.BlockUntilFinished();

            _logger.WriteLine("Done");

            return _coverFile;
        }

        private string ProcessChapters(string filepath, AAXInfo aaxInfo, string coverPath)
        {
            var _aaxInfo = aaxInfo;
            var _logger = new Logger(this.quietMode);
            var _ffmpeg = FFMpegOptions.Options.FFmpegBinary();
            var _outputFolder = this.outputFolder;
            var _filePath = filepath;
            var _coverPath = coverPath;
            var _title = CleanTitle(_aaxInfo.Format.format.tags.title);
            var _author = CleanAuthor(_aaxInfo.Format.format.tags.artist);
            var _outputDirectory = Path.Combine(_outputFolder, _author);
            _outputDirectory = Path.Combine(_outputDirectory, _title);
            var _m3uFileName = $"{_title}.m3u";

            var _invalidPathChars = Path.GetInvalidPathChars();
            foreach (var c in _invalidPathChars)
            {
                _outputDirectory = _outputDirectory.Replace(c, '_');
            }

            PurgeOutputDirectory(_outputDirectory);

            var _directoryInfo = Directory.CreateDirectory(_outputDirectory);
            var _m3uFilePath = Path.Combine(_outputDirectory, _m3uFileName);
            var _m3uFile = new StreamWriter(_m3uFilePath);
            var _chapterCount = _aaxInfo.Chapters.chapters.Count;
            var _formatString = "";

            if (_chapterCount > 100)
                _formatString = "D3";
            else if (_chapterCount > 10)
                _formatString = "D2";
            else _formatString = "D1";

            _logger.WriteLine($"Processing {_title} with {_chapterCount} Chapters");

            InitM3U(_m3uFile);

            foreach (var c in _aaxInfo.Chapters.chapters)
            {
                var _startChapter = c.start_time;
                var _endChapter = c.end_time;
                var _chapterNumber = c.id + 1; // zero based
                var _chapterFileTitle = c.tags.title.Trim();
                var _chapterFile = _title + "-" + _chapterNumber.ToString(_formatString) + "-" + _chapterFileTitle + ".mp3";
                var _chapterFilePath = Path.Combine(_outputDirectory, _chapterFile);
                _logger.Write($"\rWriting Chapter {c.id + 1} ...  ");

                var _arguments = $" -i \"{_filePath}\" -ss \"{_startChapter}\" -to \"{_endChapter}\" -acodec mp3 \"{_chapterFilePath}\"";
                var _instance = new Instance(_ffmpeg, _arguments) { DataBufferCapacity = int.MaxValue };
                var _encodeTask = _instance.FinishedRunning();
                while (!_encodeTask.IsCompleted)
                {
                    _logger.AdvanceSpinnder();
                    Thread.Sleep(100);
                }

                // Encode MP3 tags and cover here // write m3u file as well at the same time

                UpdateM3UAndTagFile(_m3uFile, _chapterFilePath, _aaxInfo, _coverPath, c);

                _logger.WriteLine("\bDone");
            }
            _m3uFile.Close();
            return _outputDirectory;
        }

        private void InitM3U(StreamWriter m3uFile)
        {
            m3uFile.WriteLine("# EXTM3U");
        }

        private void UpdateM3UAndTagFile(StreamWriter m3uFile, string chapterFile, AAXInfo _aaxInfo, string coverPath, Chapter chapter)
        {
            var _tagFile = TagLib.File.Create(chapterFile);
            var _title = CleanTitle(_aaxInfo.Format.format.tags.title);
            m3uFile.WriteLine($"# EXTINF:{_tagFile.Properties.Duration.TotalSeconds.ToString("0F")},{_aaxInfo.Format.format.tags.title} - {chapter.tags.title}");
            m3uFile.WriteLine(Path.GetFileName(chapterFile));

            if (System.IO.File.Exists(coverPath))
            {
                var _coverPicture = new TagLib.PictureLazy(coverPath);
                _tagFile.Tag.Pictures = new IPicture[] { _coverPicture };
            }
            _tagFile.Tag.Title = _title + " - " + chapter.tags.title;
            _tagFile.Tag.AlbumArtists = new string[] { _aaxInfo.Format.format.tags.artist };
            _tagFile.Tag.Album = _title;
            _tagFile.Tag.Track = (uint)chapter.id + 1;
            _tagFile.Tag.TrackCount = (uint)_aaxInfo.Chapters.chapters.Count;

            _tagFile.Tag.Copyright = _aaxInfo.Format.format.tags.copyright;
            _tagFile.Tag.DateTagged = _aaxInfo.Format.format.tags.creation_time;
            _tagFile.Tag.Comment = _aaxInfo.Format.format.tags.comment;
            _tagFile.Tag.Description = _aaxInfo.Format.format.tags.comment;
            if (!String.IsNullOrEmpty(_aaxInfo.Format.format.tags.genre))
                _tagFile.Tag.Genres = new string[] { _aaxInfo.Format.format.tags.genre };
            _tagFile.Tag.Publisher = "";
            _tagFile.Tag.Year = (uint)_aaxInfo.Format.format.tags.creation_time.Year;

            _tagFile.Save();

        }

        private string CleanAuthor(string name)
        {
            if (String.IsNullOrEmpty(name))
                return "Unknown";

            return name.Replace("Jr.", "Jr").Replace('"', '\'').Trim();
        }

        private string CleanTitle(string title)
        {
            return title.Replace("(Unabridged)", String.Empty).Replace(":", " -").Replace("'", String.Empty).Replace("?", String.Empty).Trim();
        }

        private string ProcessToMP3(string filePath, AAXInfo aaxInfo)
        {
            var _logger = new Logger(this.quietMode);
            var _filePath = filePath;
            var _aaxInfo = aaxInfo;
            var _bitrate = this.bitrate;

            var _ffmpeg = FFMpegOptions.Options.FFmpegBinary();

            var _intermediateFile = Path.GetFileName(filePath);
            _intermediateFile = Path.ChangeExtension(_intermediateFile, "mp3");
            _intermediateFile = Path.Combine(this.workingFolder, _intermediateFile);

            _logger.Write("Recoding to mp3 ...  ");

            var _arguments = $"-i \"{_filePath}\" -vn -codec:a mp3 -ab {_bitrate}k \"{_intermediateFile}\"";
            var _instance = new Instance(_ffmpeg, _arguments) { DataBufferCapacity = int.MaxValue };
            var _encodeTask = _instance.FinishedRunning();
            while (!_encodeTask.IsCompleted)
            {
                _logger.AdvanceSpinnder();
                Thread.Sleep(100);
            }

            _logger.WriteLine("\bDone");

            return _intermediateFile;

        }

        private AAXInfo GetM4Info(string f)
        {
            var _logger = new Logger(this.quietMode);
            FFProbeHelper.RootExceptionCheck(FFMpegOptions.Options.RootDirectory);
            var _filePath = f;
            var _ffprobe = FFMpegOptions.Options.FFProbeBinary();

            _logger.Write("Probing ");

            var _arguments = $"-print_format json -show_format \"{_filePath}\"";
            _logger.WriteLine("1");
            _logger.WriteLine("Filepath -> " + _filePath);
            var _instance = new Instance(_ffprobe, _arguments) { DataBufferCapacity = int.MaxValue };
            _logger.WriteLine("1");
            _instance.BlockUntilFinished();
            _logger.WriteLine("3");
            var _formatJson = string.Join(string.Empty, _instance.OutputData);
            _logger.WriteLine("4");

            _logger.Write(".");

            _arguments = $"-print_format json -show_streams \"{_filePath}\"";
            _instance = new Instance(_ffprobe, _arguments) { DataBufferCapacity = int.MaxValue };
            _instance.BlockUntilFinished();
            var _streamsJson = string.Join(string.Empty, _instance.OutputData);

            _logger.Write(".");

            _arguments = $"-print_format json -show_chapters \"{_filePath}\"";
            _instance = new Instance(_ffprobe, _arguments) { DataBufferCapacity = int.MaxValue };
            _instance.BlockUntilFinished();
            var _chaptersJson = string.Join(string.Empty, _instance.OutputData);

            _logger.Write(".");

            var _audioFormat = JsonSerializer.Deserialize<AudioFormat>(_formatJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var _audioChapters = JsonSerializer.Deserialize<AudioChapters>(_chaptersJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            var _audioStreams = JsonSerializer.Deserialize<AudioStreams>(_streamsJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var _result = new AAXInfo(_audioFormat, _audioChapters, _audioStreams);

            _logger.WriteLine(" Done");

            return _result;
        }

        private void CheckFolders()
        {
            if (!Directory.Exists(this.inputFolder))
                throw new Exception("Input folder does not exist: " + inputFolder);
            if (!Directory.Exists(this.outputFolder))
                throw new Exception("Output folder does not exist: " + inputFolder);
            if (!Directory.Exists(this.storageFolder))
                throw new Exception("Storage folder does not exist: " + inputFolder);
            if (!Directory.Exists(this.workingFolder))
                throw new Exception("Working folder does not exist: " + inputFolder);
            System.IO.DirectoryInfo di = new DirectoryInfo(this.workingFolder);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
        }
    }
}

