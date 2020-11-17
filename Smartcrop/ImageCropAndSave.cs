using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using System.IO;
using SkiaSharp;
using DlibDotNet;

namespace Smartcrop
{
    public class ImageCropAndSave
    {
        private string ratio;
        private bool debug;
        private int maxWidth;

        public ImageCropAndSave(string ratio, int maxWidth = 0, bool debug = false)
        {
            this.ratio = ratio;
            this.maxWidth = maxWidth;
            this.debug = debug;
        }

        public void CropAndSave(FileInfo imageFile, string destFilePath, params BoostArea[] boostAreas)
        {
            var watch = Stopwatch.StartNew();

            using (var bitmap = SKBitmap.Decode(imageFile.FullName))
            {
                var rationew = RatioUtil.ComputeRatio(bitmap.Width, bitmap.Height, this.ratio, this.maxWidth);
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

                    //using var resized = croppedBitmap.Resize(new SKImageInfo(options.Width, options.Height), SKFilterQuality.Medium);
                    
                    using (var image = SKImage.FromBitmap(croppedBitmap))
                    using (var stream = File.Open(destFilePath, FileMode.OpenOrCreate))
                    {
                        //var data = image.Encode(getFormat(imageFile), 75);
                        var data = image.Encode();
                        data.SaveTo(stream);
                        //stream.Seek(0, SeekOrigin.Begin);

                    }

                    

                };

                try
                {
                    ResizeUsingDlib(destFilePath, getFormat(imageFile), options.Width, options.Height);
                }
                catch
                {
                    // let it be
                }

                watch.Stop();

                Console.WriteLine($"drawCropped: {watch.ElapsedMilliseconds} ms");
            }

            
        }

        private SKEncodedImageFormat getFormat(FileInfo image)
        {
            string ext = image.Extension.ToUpperInvariant();
            return (ext) switch
            {
                ".JPG" => SKEncodedImageFormat.Jpeg,
                ".JPEG" => SKEncodedImageFormat.Jpeg,
                ".PNG" => SKEncodedImageFormat.Png,
                ".GIF" => SKEncodedImageFormat.Gif,
                ".BMP" => SKEncodedImageFormat.Bmp,
                _ => SKEncodedImageFormat.Jpeg
            };
        }

        // dlib seems to make it compressed better filesize
        private void ResizeUsingDlib(string filepath, SKEncodedImageFormat format, int width, int height)
        {
            
            using var img = Dlib.LoadImage<RgbPixel>(filepath);

            if (this.maxWidth > 0)  // specified
            {
                double size0 = img.Size;

                double size = width * height;
                double scale = Math.Sqrt(size / size0);

                Dlib.ResizeImage(img, scale);
            }
            

            if (format == SKEncodedImageFormat.Png)
                Dlib.SavePng(img, filepath);
            else
                Dlib.SaveJpeg(img, filepath);
            
        }

    }
}
