using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.IO;
using SkiaSharp;


namespace Smartcrop
{
    public class ImageCropAndSave
    {
        private string ratio;
        private bool debug;

        public ImageCropAndSave(string ratio, bool debug = false)
        {
            this.ratio = ratio;
            this.debug = debug;
        }

        public void CropAndSave(string imagePath, string destFilePath, params BoostArea[] boostAreas)
        {
            var watch = Stopwatch.StartNew();

            using (var bitmap = SKBitmap.Decode(imagePath))
            {
                var rationew = RatioUtil.ComputeRatio(bitmap.Width, bitmap.Height, this.ratio);
                var options = new Options(rationew.Width, rationew.Height)
                {
                    Debug = this.debug
                };

                var crop = new ImageCrop(options);

                // calculate the best crop area
                var result = crop.Crop(bitmap, boostAreas);
                watch.Stop();

                Console.WriteLine($"cropCompute: {watch.ElapsedMilliseconds} ms");

                watch.Restart();

                // crop the image
                SKRect cropRect = new SKRect(result.Area.Left, result.Area.Top, result.Area.Right, result.Area.Bottom);
                using (SKBitmap croppedBitmap = new SKBitmap((int)cropRect.Width, (int)cropRect.Height))
                using (SKCanvas canvas = new SKCanvas(croppedBitmap))
                {
                    SKRect source = new SKRect(cropRect.Left, cropRect.Top,
                                            cropRect.Right, cropRect.Bottom);
                    SKRect dest = new SKRect(0, 0, cropRect.Width, cropRect.Height);
                    canvas.DrawBitmap(bitmap, source, dest);

                    using (var image = SKImage.FromBitmap(croppedBitmap))
                    using (var stream = File.Open(destFilePath, FileMode.OpenOrCreate))
                    {
                        image.Encode().SaveTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                    }

                    watch.Stop();

                    Console.WriteLine($"drawCropped: {watch.ElapsedMilliseconds} ms");

                };
            }
        }
    }
}
