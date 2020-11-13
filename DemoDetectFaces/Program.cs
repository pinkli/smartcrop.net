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
                foreach (var face in faces)
                {
                    // draw it
                    Dlib.DrawRectangle(img, face, color: new RgbPixel(255, 0, 0), thickness: 4);
                }

                if (faces.Length > 0)
                {
                    double size = img.Size;
                    double scale = Math.Sqrt(size0 / size);
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
