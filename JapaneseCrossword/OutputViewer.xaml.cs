using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace JapaneseCrossword
{
    /// <summary>
    /// Interaction logic for OutputViewer.xaml
    /// </summary>
    public partial class OutputViewer
    {
	    private readonly SaveFileDialog _dlg;

	    public OutputViewer()
        {
	        InitializeComponent();
	        _dlg = new SaveFileDialog {FileName = "Build", DefaultExt = ".png", Filter = "PNG Files|*.png"};
        }

	    private void Button_Click(object sender, RoutedEventArgs e)
        {
		    var res = _dlg.ShowDialog();
		    if (res != true) return;
		    var filename = _dlg.FileName;
		    var imgsrc = (BitmapSource)PreviewImage.Source;
                
		    using(var fs = new FileStream(filename, FileMode.Create))
		    {
			    var png = new PngBitmapEncoder();
			    png.Frames.Add(BitmapFrame.Create(imgsrc));
			    png.Save(fs);
		    }
        }
    }
}
