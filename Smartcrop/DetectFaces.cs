using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DlibDotNet;

namespace Smartcrop
{
    public class DetectFaces
    {
        private FrontalFaceDetector detector;

        public DetectFaces()
        {
            this.detector = Dlib.GetFrontalFaceDetector();
        }


        public List<BoostArea> FindBoostAreas(string imageFilePath)
        {
            var rects = FindFaces(imageFilePath);
            return rects.Select(r => new BoostArea(r, 0.99f)).ToList();
        }

        private List<Rectangle> FindFaces(string imageFilePath)
        {
            var result = new List<Rectangle>();

            //using (var detector = Dlib.GetFrontalFaceDetector())
            //{
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
                    //Dlib.DrawRectangle(img, face, color: new RgbPixel(255, 0, 0), thickness: 4);
                    Console.WriteLine($"left: {face.Left}, top: {face.Top}, width: {face.Width}, height: {face.Height}");

                    int left = (int)(face.Left * scale);
                    int top = (int)(face.Top * scale);
                    int width = (int)(face.Width * scale);
                    int height = (int)(face.Height * scale);
                    var actualRect = new DlibDotNet.Rectangle(left, top, left + width, top + height);
                    Console.WriteLine($"rect in origin img left: {actualRect.Left}, top: {actualRect.Top}, width: {actualRect.Width}, height: {actualRect.Height}");

                    result.Add(dlibRectToRect(actualRect));
                }

                //if (faces.Length > 0)
                //{

                //    Dlib.ResizeImage(img, scale);
                //    Dlib.SaveJpeg(img, $"{getFileName(imageFilePath)}-faces.jpg");

                //}
            //}

            return result;
        }

        private Rectangle dlibRectToRect(DlibDotNet.Rectangle rect)
        {
            return new Rectangle(rect.Left, rect.Top, (int)rect.Width, (int)rect.Height);
        }
    }
}
