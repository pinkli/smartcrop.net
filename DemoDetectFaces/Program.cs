using System;
using DlibDotNet.Extensions;
using DlibDotNet;
using System.IO;

namespace DemoDetectFaces
{
    class Program
    {
        static void Main(string[] args)
        {
            string image = @"C:\Users\civil\Pictures\xhwapp\0051bb32e68c44a0a753c5d51b9e27d0.jpg";
            detect(image);
            Console.WriteLine("DONE");
        }

        static void detect(string imageFilePath)
        {
            using (var detector = Dlib.GetFrontalFaceDetector())
            {
                using var img = Dlib.LoadImage<RgbPixel>(imageFilePath);
                double size0 = img.Size;
                
                Dlib.PyramidUp(img);

                var faces = detector.Operator(img);
                Console.WriteLine($"faces detected: {faces.Length}");

                double size = img.Size;
                double scale = Math.Sqrt(size0 / size);

                foreach (var face in faces)
                {
                    // draw it
                    Dlib.DrawRectangle(img, face, color: new RgbPixel(255, 0, 0), thickness: 4);
                    Console.WriteLine($"left: {face.Left}, top: {face.Top}, width: {face.Width}, height: {face.Height}");

                    int left = (int)(face.Left * scale);
                    int top = (int)(face.Top * scale);
                    int width = (int)(face.Width * scale);
                    int height = (int)(face.Height * scale);
                    var actualRect = new Rectangle(left, top, left + width, top + height);
                    Console.WriteLine($"rect in origin img left: {actualRect.Left}, top: {actualRect.Top}, width: {actualRect.Width}, height: {actualRect.Height}");
                }

                if (faces.Length > 0)
                {
                    
                    Dlib.ResizeImage(img, scale);
                    Dlib.SaveJpeg(img, $"{getFileName(imageFilePath)}-faces.jpg");

                }
            }
        }

        static string getFileName(string filepath)
        {
            int idx = filepath.LastIndexOf('.');
            return filepath.Substring(0, idx);
        }
    }
}
