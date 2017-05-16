using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PhotoViewerCSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private StorageFile imageFile;
        private int lastAddedStreamID = -1;
        private List<IRandomAccessStreamWithContentType> layers; // all dragged items
        //OR all objects extracted from current image + dragged items


        public MainPage()
        {
            this.InitializeComponent();
            layers = new List<IRandomAccessStreamWithContentType>();

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(MainPage), null); //DropMe
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }

        private async Task<StorageFile> PickImageFileAsync()
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".jpeg");

            return await openPicker.PickSingleFileAsync();
        }

        private async void SelectImage_Click(object sender, RoutedEventArgs e)
        {
            this.SelectImage.IsEnabled = false;

            this.imageFile = await PickImageFileAsync();
            await ShowImage();

            this.SelectImage.IsEnabled = true;
        }
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                imageFile = items[0] as StorageFile;
                if (items.Count > 0)
                {
                    var storageFile = items[0] as StorageFile;
                    var bitmapImage = new BitmapImage();
                    IRandomAccessStreamWithContentType newStream = await imageFile.OpenReadAsync();
                    lastAddedStreamID++;
                    layers.Add(newStream);
                    AddDraggedStream();
                }
            }
            else
            if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference data = await e.DataView.GetBitmapAsync();
                IRandomAccessStreamWithContentType newStream = await data.OpenReadAsync();
                lastAddedStreamID++;
                layers.Add(newStream);
                AddDraggedStream();
            }
        }

        private void AddDraggedStream()
        {

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(layers[lastAddedStreamID]);
            Image img = new Image();
            img.Source = bitmapImage;
            double maxDim = 500;
            if (img.Width > img.Height)
            {
                img.Width = maxDim;
            }
            else
            {
                img.Height = maxDim;
            }
            Image.Children.Add(img);
        }

        private async Task ShowImage()
        {
            BitmapImage bitmapimage = new BitmapImage();
            IRandomAccessStreamWithContentType newStream = await imageFile.OpenReadAsync();

            //AddNewStream(newStream);
            await bitmapimage.SetSourceAsync(newStream);

            Image image = new Image();
            image.Source = bitmapimage;
            double maxDim = 700;
            if (image.Width > image.Height)
            {
                image.Width = maxDim;
            }
            else
            {
                image.Height = maxDim;
            }

            //image.PointerPressed += new Windows.UI.Xaml.Input.PointerEventHandler(SaveSelectedImage_PointerPressed);
            GridView grid = new GridView();
            grid.Margin = new Thickness(5, 5, 5, 5);

            grid.Items.Add(image);
            Results.Items.Add(grid);
        }

        private async void Image_DragStarting(UIElement sender, DragStartingEventArgs args) // put alpha channels to max here to pixels dragged 
        {
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            //args.Data.SetStorageItems(files);
            if (imageFile!=null) { 
                var str = await imageFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
                args.Data.SetBitmap(RandomAccessStreamReference.CreateFromStream(str));
            }
            else
            {
                args.Data.SetBitmap(RandomAccessStreamReference.CreateFromStream(layers[lastAddedStreamID]));
            }
        }

        private void Grid_DragOverCustomized(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drag here"; // Sets custom UI text
            e.DragUIOverride.SetContentFromBitmapImage(null); // Sets a custom glyph
            e.DragUIOverride.IsCaptionVisible = true; // Sets if the caption is visible
            e.DragUIOverride.IsContentVisible = true; // Sets if the dragged content is visible
            e.DragUIOverride.IsGlyphVisible = true; // Sets if the glyph is visibile
        }

        private async void SaveSelectedImage_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint ptrPt=e.GetCurrentPoint(Results);
            Size size = new Windows.Foundation.Size(200, 200);

            Image img = new Image();
            img.Source= await GetCroppedBitmapAsync(ptrPt.Position, size, 1);

            double maxDim = 700;
            if (img.Width > img.Height)
            {
                img.Width = maxDim;
            }
            else
            {
                img.Height = maxDim;
            }
 
           // grid.Items.Add(img);
            //Results.Items.Add(grid);
        }

        async public Task<ImageSource> GetCroppedBitmapAsync(
    Point startPoint, Size corpSize, double scale)
        {
            if (double.IsNaN(scale) || double.IsInfinity(scale))
            {
                scale = 1;
            }


            // Convert start point and size to integer. 
            uint startPointX = (uint)Math.Floor(startPoint.X * scale);
            uint startPointY = (uint)Math.Floor(startPoint.Y * scale);
            uint height = (uint)Math.Floor(corpSize.Height * scale);
            uint width = (uint)Math.Floor(corpSize.Width * scale);


            using (IRandomAccessStream stream = await imageFile.OpenReadAsync())
            {
                // Create a decoder from the stream. With the decoder, we can get  
                // the properties of the image. 
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // The scaledSize of original image. 
                uint scaledWidth = (uint)Math.Floor(decoder.PixelWidth * scale);
                uint scaledHeight = (uint)Math.Floor(decoder.PixelHeight * scale);



                // Refine the start point and the size.  
                if (startPointX + width > scaledWidth)
                {
                    startPointX = scaledWidth - width;
                }


                if (startPointY + height > scaledHeight)
                {
                    startPointY = scaledHeight - height;
                }


                // Create cropping BitmapTransform and define the bounds. 
                BitmapTransform transform = new BitmapTransform();
                BitmapBounds bounds = new BitmapBounds();
                bounds.X = startPointX;
                bounds.Y = startPointY;
                bounds.Height = height;
                bounds.Width = width;
                transform.Bounds = bounds;


                transform.ScaledWidth = scaledWidth;
                transform.ScaledHeight = scaledHeight;

                // Get the cropped pixels within the bounds of transform. 
                PixelDataProvider pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb);
                byte[] pixels = pix.DetachPixelData();


                // Stream the bytes into a WriteableBitmap 
                WriteableBitmap cropBmp = new WriteableBitmap((int)width, (int)height);
                Stream pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int)(width * height * 4));

                return cropBmp;
            }
        }
    }
}
