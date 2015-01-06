using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace japcrossword
{
    /// <summary>
    /// Interaction logic for OutputViewer.xaml
    /// </summary>
    public partial class OutputViewer : Window
    {
        public OutputViewer()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Bild";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG Files|*.png";

            Nullable<bool> res = dlg.ShowDialog();
            if(res == true)
            {
                string filename = dlg.FileName;
                BitmapSource imgsrc = (BitmapSource)img.Source;
                
                using(FileStream fs = new FileStream(filename, FileMode.Create))
                {
                    PngBitmapEncoder png = new PngBitmapEncoder();
                    png.Frames.Add(BitmapFrame.Create(imgsrc));
                    png.Save(fs);
                }
            }
        }
    }
}
