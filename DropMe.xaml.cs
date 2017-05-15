using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace PhotoViewerCSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DropMe : Page
    {
        public DropMe()
        {
            this.InitializeComponent();
        }
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                RandomAccessStreamReference thumbnail = await e.DataView.GetBitmapAsync();
                IRandomAccessStreamWithContentType stream = await thumbnail.OpenReadAsync();
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.SetSource(stream);
                Image.Source = bitmapImage;
            }
        }

        private void Grid_DragOverCustomized(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Drop here to view"; // Sets custom UI text
            e.DragUIOverride.SetContentFromBitmapImage(null); // Sets a custom glyph
            e.DragUIOverride.IsCaptionVisible = true; // Sets if the caption is visible
            e.DragUIOverride.IsContentVisible = true; // Sets if the dragged content is visible
            e.DragUIOverride.IsGlyphVisible = true; // Sets if the glyph is visibile
        }
    }
}
