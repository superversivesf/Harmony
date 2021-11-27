using Audio_Convertor.AudioJson;
using Audio_Convertor.ChaptersJson;
using Audio_Convertor.StreamsJson;
using CommandLine;
using FFMpegCore;
using FFMpegCore.Helpers;
using Instances;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using Xabe.FFmpeg.Downloader;

namespace Audio_Convertor
{
    class Program
    {
        class Options
        {
            [Option('b', "Bitrate", Required = false, HelpText = "The bitrate in kilobits for the output files. Defaults to 64k, specified as 64", Default = 64), ]
            public int bitrate { get; set; }

            [Option('i', "InputFolder", Required = false, HelpText = "The folderthat contains your AAX files"),]
            public string inputFolder { get; set; }

            [Option('o', "OutputFolder", Required = false, HelpText = "The folder that will contain your MP3 files"),]
            public string outputFolder { get; set; }

            [Option('s', "StorageFolder", Required = false, HelpText = "Folder that finished AAX files will be moved too"),]
            public string storageFolder { get; set; }

            [Option('w', "WorkingFolder", Required = false, HelpText = "Temp folder that will be used for processing everything. Emptied at start up"),]
            public string workingFolder { get; set; }

            [Option('a', "ActivationBytes", Required = false, HelpText = "Activation bytes for decoding your AAX File. See https://github.com/inAudible-NG/tables for details of how to obtain"),]
            public string activationBytes { get; set; }

            [Option('f', "FetchFFMpeg", Required = false, HelpText = "Just download versions of ffmpeg and ffprobe locally and exit"),]
            public bool fetchFFMpeg { get; set; }

            [Option('q', "QuietMode", Required = false, HelpText = "Disable all output", Default = false),]
            public bool quietMode { get; set; }
            [Option('l', "LoopMode", Required = false, HelpText = "Have the program run to completion then check and run again with a 5 minute sleep between each run", Default = false),]
            public bool loopMode { get; set; }
        }

        static void Main(string[] args)
        {
            // dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
            // dotnet publish -r linux-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
            // dotnet publish -r linux-arm -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
            // dotnet publish -r linux-arm64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
            // dotnet publish -r osx-x64 -c Release -p:PublishSingleFile=true -p:PublishTrimmed=true
            try
            {
                
                CommandLine.Parser.Default.ParseArguments<Options>(args)
                  .WithParsed(RunOptions)
                  .WithNotParsed(HandleParseError);
            }
            catch (Exception e)
            {
                Console.WriteLine("Processing failure -> " + e.Message + ":" + e.InnerException?.Message);
            }
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (Error e in errors)
            { 
                //Console.WriteLine(e.)
            }
        }

        static private void RunOptions(Options options)
        {
            var _activationBytes = options.activationBytes;
            var _bitrate = options.bitrate;
            var _inputFolder = options.inputFolder;
            var _outputFolder = options.outputFolder;
            var _storageFolder = options.storageFolder;
            var _quietMode = options.quietMode;
            var _workingFolder = options.workingFolder;
            var _logger = new Logger(_quietMode);
            var _loopMode = options.loopMode;

            _logger.WriteLine($"Harmony {Assembly.GetExecutingAssembly().GetName().Version.ToString()}\nCopyright(C) 2020 Harmony\n");

            if (options.fetchFFMpeg)
            {
                FFMpegOptions.Configure(new FFMpegOptions { RootDirectory = ".", TempDirectory = "." });
                _logger.Write("Fetching Latest FFMpeg ...  ");

                var fetchTask = FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                while (!fetchTask.IsCompleted)
                {
                    _logger.AdvanceSpinnder();
                    Thread.Sleep(50);
                }

                _logger.WriteLine("\bDone");

                return;
            }

            do
            {
                var _aaxProcessor = new AAXAudioConvertor(_activationBytes, _bitrate, _quietMode, _inputFolder, _outputFolder, _storageFolder, _workingFolder);
                _aaxProcessor.Execute();

                var _m4Processor = new M4AudioConvertor(_bitrate, _quietMode, _inputFolder, _outputFolder, _storageFolder, _workingFolder);
                _m4Processor.Execute();

                if (_loopMode)
                { 
                    _logger.WriteLine("Run complete, sleeping for 5 mins");
                    Thread.Sleep(600);
                }

            } while (_loopMode);

        }
    }



}

