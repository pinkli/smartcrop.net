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

        [Option("--no-detect-face", Description = "do not detect face")]
        public bool NoDetectFace { get; set; }

        [Argument(0, Description = "The image or folder to be cropped")]
        [Required]
        public string SourcePath { get; set; }

        static string[] EXTS = new[] { ".JPG", ".JPEG", ".PNG", ".BMP"};

        private DetectFaces facedetector = new DetectFaces();

        private void OnExecute()
        {
            if (String.IsNullOrWhiteSpace(Ratio))
            {
                Ratio = "16:9";
            }

            if (String.IsNullOrWhiteSpace(OutDir))
            {
                OutDir = "./";
            }

            Console.WriteLine($"ratio: {Ratio}");
            Console.WriteLine($"outdir: {OutDir}");
            Console.WriteLine($"noface: {NoDetectFace}");
            Console.WriteLine($"source path: {SourcePath}");

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
        }

        private void CropImage(FileInfo imageFile)
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

        private bool IsValidImage(FileInfo fi)
        {
            string ext = fi.Extension.ToUpperInvariant();
            return EXTS.Contains(ext);
        }

        private string DestFile(FileInfo imageFile)
        {
            return Path.Combine(this.OutDir, imageFile.Name);
        }
    }
}
