using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace Smartcrop.Sample.Wpf
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Func<string> fileSelector;

        private int cropWidth = 320;
        private int cropHeight = 180;
        private string sourceImagePath;
        private ImageSource debugImage;
        private ImageSource croppedImage;
        private string errorText;
        private bool useDetectFace;

        private DetectFaces facedetector = new DetectFaces();

        public ViewModel(Func<string> fileSelector)
        {
            this.fileSelector = fileSelector ?? throw new ArgumentNullException(nameof(fileSelector));
            this.SelectImageCommand = new SimpleCommand(this.SelectImage);

            var cropProperties = new[] 
            {
                nameof(this.SourceImagePath),
                nameof(this.CropWidth),
                nameof(this.CropHeight),
                nameof(this.UseDetectFace)
            };

            // create a new cropped image whenever one of these properties changes
            this.PropertyChanged += (s, e) =>
            {
                if (cropProperties.Any(p => p == e.PropertyName))
                {
                    this.Crop();
                }
            };
        }

        private void Crop()
        {
            try
            {
                // create options and image crop 
                var options = new Options(this.CropWidth, this.CropHeight)
                {
                    Debug = true
                };

                var crop = new ImageCrop(options);

                var watch = Stopwatch.StartNew();

                // detect faces
                BoostArea[] boostAreas = Array.Empty<BoostArea>();
                if (this.UseDetectFace)
                {
                    boostAreas = facedetector.FindBoostAreas(this.SourceImagePath).ToArray();
                }

                watch.Stop();

                string msg = $"{boostAreas.Length} faces detected, takes: {watch.ElapsedMilliseconds} ms; ";

                watch.Restart();
                // load the source image
                using (var bitmap = SKBitmap.Decode(this.SourceImagePath))
                {
                    // calculate the best crop area
                    var result = crop.Crop(bitmap, boostAreas);
                    watch.Stop();

                    msg += $"cropCompute: {watch.ElapsedMilliseconds} ms; ";

                    this.DebugImage = this.CreateImageSource(result.DebugInfo.Output);

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
                        watch.Stop();

                        msg += $"drawCropped: {watch.ElapsedMilliseconds} ms; ";

                        this.CroppedImage = this.CreateImageSource(croppedBitmap);
                    };
                }

                this.ErrorText = msg;
            }
            catch (Exception e)
            {
                this.ErrorText = e.Message;
            }
        }

        private ImageSource CreateImageSource(SKBitmap bitmap)
        {
            using (var image = SKImage.FromBitmap(bitmap))
            using (var stream = new MemoryStream())
            {
                image.Encode().SaveTo(stream);
                stream.Seek(0, SeekOrigin.Begin);

                return this.CreateImageSource(stream);
            }
        }

        private ImageSource CreateImageSource(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return this.CreateImageSource(stream);
            }
        }

        private ImageSource CreateImageSource(Stream stream)
        {         
            var imageSource = new BitmapImage();
            imageSource.BeginInit();
            imageSource.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            imageSource.CacheOption = BitmapCacheOption.OnLoad;
            imageSource.StreamSource = stream;
            imageSource.EndInit();
            imageSource.Freeze();

            return imageSource;
        }

        private void SelectImage()
        {
            try
            {
                var imagePath = this.fileSelector();
                if (imagePath != null)
                {
                    this.SourceImagePath = imagePath;
                }
            }
            catch (Exception e)
            {
                this.ErrorText = e.Message;
            }
        }

        public ICommand SelectImageCommand { get; }

        public int CropWidth
        {
            get => this.cropWidth;
            set => this.SetProperty(ref this.cropWidth, value);
        }

        public int CropHeight
        {
            get => this.cropHeight;
            set => this.SetProperty(ref this.cropHeight, value);
        }

        public string SourceImagePath
        {
            get => this.sourceImagePath;
            set => this.SetProperty(ref this.sourceImagePath, value);
        }

        public ImageSource DebugImage
        {
            get => this.debugImage;
            set => this.SetProperty(ref this.debugImage, value);
        }

        public ImageSource CroppedImage
        {
            get => this.croppedImage;
            set => this.SetProperty(ref this.croppedImage, value);
        }

        public string ErrorText
        {
            get => this.errorText;
            set => this.SetProperty(ref this.errorText, value);
        }

        public bool UseDetectFace
        {
            get => this.useDetectFace;
            set => this.SetProperty(ref this.useDetectFace, value);
        }

        private void SetProperty<T>(ref T field, T value, [CallerMemberName]string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(value, field))
            {
                field = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
