using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace sodukobilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Stopwatch watch;
        public MainWindow()
        {
            InitializeComponent();
            watch = new Stopwatch();
        }

        Bitmap curimg;
        Bitmap curimg_scaled;
        private void ScaleCurImage()
        {
            if (curimg == null)
                return;

            double scale = sldr_scale.Value;
            if (scale == 1)
            {
                curimg_scaled = curimg;
                lbl_inputsize.Content = String.Format("Input Size: {0}x{1}", curimg.Width, curimg.Height);
                lbl_scaledsize.Content = String.Format("Scaled Size: {0}x{1}", curimg.Width, curimg.Height);
                return;
            }
            int neww = (int)Math.Round((double)curimg.Width * scale);
            int newh = (int)Math.Round((double)curimg.Height * scale);

            neww = Math.Max(neww, 5);
            newh = Math.Max(newh, 5);

            lbl_inputsize.Content = String.Format("Input Size: {0}x{1}", curimg.Width, curimg.Height);
            lbl_scaledsize.Content = String.Format("Scaled Size: {0}x{1}", neww, newh);

            curimg_scaled = ImageUtilities.ResizeImage(curimg, neww, newh);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".png";
            dlg.Filter = "All Files|*.*|PNG files|*.png|JPEG files|*.jpeg|JPG files|*.jpg";

            Nullable<bool> result = dlg.ShowDialog();

            if(result == true)
            {
                string filename = dlg.FileName;
                txtbx_filepath.Text = filename;

                try
                {
                    curimg = (Bitmap)Bitmap.FromFile(filename);
                }
                catch{}

                ScaleCurImage();
                UpdatePreview();
            }
        }

        private static Action EmptyDelegate = delegate() { };
        private void SetPreviewBitmap(Bitmap bmp)
        {
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            img_preview.Source = bi;
        }

        private Color GetBWColor(Color inputclr)
        {
            double yiq = (((double)inputclr.R * sldr_red.Value) + ((double)inputclr.G * sldr_green.Value) + ((double)inputclr.B * sldr_blue.Value)) / (sldr_red.Value + sldr_green.Value + sldr_blue.Value);
            return yiq >= 128 ? Color.Black : Color.LightGray;
        }

        private bool GetBWColorBool(Color inputclr)
        {
            if (inputclr.A == 0)
                return false;

            double yiq = (((double)inputclr.R * sldr_red.Value) + ((double)inputclr.G * sldr_green.Value) + ((double)inputclr.B * sldr_blue.Value)) / (sldr_red.Value + sldr_green.Value + sldr_blue.Value);
            if(chkbx_flip.IsChecked.Value)
                return yiq <= sldr_cntrst.Value;
            else
                return yiq >= sldr_cntrst.Value;
        }

        private void UpdatePreview()
        {
            if (curimg_scaled == null)
                return;

            Bitmap bmp = new Bitmap(curimg_scaled.Width * 5, curimg_scaled.Height * 5);
            Graphics g = Graphics.FromImage(bmp);

            RectangleF[] rects = new RectangleF[curimg_scaled.Width * curimg_scaled.Height];
            for (int y = 0; y < curimg_scaled.Height; y++)
            {
                for (int x = 0; x < curimg_scaled.Width; x++)
                {
                    if (GetBWColorBool(curimg_scaled.GetPixel(x, y)))
                    {
                        rects[y * curimg_scaled.Width + x] = new RectangleF(x * 5, y * 5, 5, 5);
                    }
                }
            }

            Brush b = new SolidBrush(Color.Black);
            g.FillRectangles(b, rects);

            SetPreviewBitmap(bmp);
        }

        private void sldr_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreview();
        }

        private void sldr_scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ScaleCurImage();
            UpdatePreview();
        }

        private void CheckBox_Flip_Checked(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void CheckBox_Clamp_Checked(object sender, RoutedEventArgs e)
        {
            ScaleCurImage();
            UpdatePreview();
        }

    }
}
