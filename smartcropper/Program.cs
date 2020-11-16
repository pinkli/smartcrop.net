using System;
using System.IO;
using Smartcrop;
using McMaster.Extensions.CommandLineUtils;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Diagnostics;

namespace smartcropper
{
    [Command(ExtendedHelpText = "face-aware smart image cropper")]
    public class Program
    {
        public static void Main(string[] args)
        {
            CommandLineApplication.Execute<Program>(args);
        }

        [Option("--ratio <RATIO>", Description = "Dest image aspect ratio. Default 16:9")]
        public string Ratio { get; set; }

        [Option("--out-dir <DIR>", Description = "Output dir")]
        public string OutDir { get; set; }

        [Option("--no-detect-face", Description = "Do not detect face")]
        public bool NoDetectFace { get; set; }

        [Option("--skip-failed", Description = "Skip the crop-failing image, otherwise copy it to output dir")]
        public bool SkipFailed { get; set; }

        [Argument(0, Description = "The image or folder to be cropped")]
        [Required]
        public string SourcePath { get; set; }

        static string[] EXTS = new[] { ".JPG", ".JPEG", ".PNG", ".BMP"};

        private DetectFaces facedetector = new DetectFaces();

        private void OnExecute()
        {
            if (string.IsNullOrWhiteSpace(Ratio))
            {
                Ratio = "16:9";
            }

            if (string.IsNullOrWhiteSpace(OutDir))
            {
                OutDir = "./";
            }

            Console.WriteLine($"ratio: {Ratio}");
            Console.WriteLine($"outdir: {OutDir}");
            Console.WriteLine($"noface: {NoDetectFace}");
            Console.WriteLine($"source path: {SourcePath}");

            if (!Directory.Exists(OutDir))
            {
                Directory.CreateDirectory(OutDir);
            }

            if (File.Exists(SourcePath))
            {
                var image = new FileInfo(SourcePath);
                if (!IsValidImage(image))
                {
                    Console.WriteLine($"{SourcePath} is not applicable image");
                    return;
                }

                CropImage(image);
            }
            else if (Directory.Exists(SourcePath))
            {
                var images = Directory.EnumerateFiles(SourcePath).Select(fi => new FileInfo(fi)).Where(fi => IsValidImage(fi));
                foreach (var image in images)
                {
                    CropImage(image);
                }
            }
        }

        private void CropImage(FileInfo imageFile)
        {
            Console.WriteLine($"process image: {imageFile.FullName}");

            try
            {
                // detect faces
                var watch = Stopwatch.StartNew();
                BoostArea[] boostAreas = Array.Empty<BoostArea>();
                if (!this.NoDetectFace)
                {
                    boostAreas = facedetector.FindBoostAreas(imageFile.FullName).ToArray();
                }
                watch.Stop();

                Console.WriteLine($"{boostAreas.Length} faces detected, takes: {watch.ElapsedMilliseconds} ms");

                // crop and save
                var crop = new ImageCropAndSave(Ratio, debug: false);
                string destFile = DestFile(imageFile);
                crop.CropAndSave(imageFile.FullName, destFile, boostAreas);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                if (!SkipFailed)
                {
                    Copy(imageFile);
                }
            }
        }

        private bool IsValidImage(FileInfo fi)
        {
            string ext = fi.Extension.ToUpperInvariant();
            return EXTS.Contains(ext);
        }

        private string DestFile(FileInfo imageFile)
        {
            return Path.Combine(this.OutDir, imageFile.Name);
        }

        private void Copy(FileInfo imageFile)
        {
            string dest = DestFile(imageFile);
            File.Copy(imageFile.FullName, dest, true);
        }
    }
}
