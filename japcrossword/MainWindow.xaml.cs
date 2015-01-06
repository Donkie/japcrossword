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
using japcrossword;

namespace sodukobilder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        const int RectSize = 20;

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

            if(chkbx_clamp.IsChecked.Value)
            {
                neww = ((int)Math.Round((double)neww / 5d)) * 5;
                newh = ((int)Math.Round((double)newh / 5d)) * 5;
            }

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
                return !chkbx_flip.IsChecked.Value;

            double yiq = (((double)inputclr.R * sldr_red.Value) + ((double)inputclr.G * sldr_green.Value) + ((double)inputclr.B * sldr_blue.Value)) / (sldr_red.Value + sldr_green.Value + sldr_blue.Value);
            if(chkbx_flip.IsChecked.Value)
                return yiq <= sldr_cntrst.Value;
            else
                return yiq >= sldr_cntrst.Value;
        }

        bool[,] clrArray;
        private void UpdatePreview()
        {
            if (curimg_scaled == null)
                return;

            Bitmap bmp = new Bitmap(curimg_scaled.Width * RectSize, curimg_scaled.Height * RectSize);
            Graphics g = Graphics.FromImage(bmp);

            RectangleF[] rects = new RectangleF[curimg_scaled.Width * curimg_scaled.Height];
            clrArray = new bool[curimg_scaled.Width, curimg_scaled.Height];
            for (int y = 0; y < curimg_scaled.Height; y++)
            {
                for (int x = 0; x < curimg_scaled.Width; x++)
                {
                    if (GetBWColorBool(curimg_scaled.GetPixel(x, y)))
                    {
                        rects[y * curimg_scaled.Width + x] = new RectangleF(x * RectSize, y * RectSize, RectSize, RectSize);
                        clrArray[x, y] = true;
                    }
                    else
                    {
                        clrArray[x, y] = false;
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

        private int[] PopFill(Stack<int> stack)
        {
            int[] arr = new int[stack.Count];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = stack.Pop();
            return arr;
        }

        private void Button_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (clrArray == null)
                UpdatePreview();

            if (clrArray == null)
                return;

            int w = clrArray.GetLength(0);
            int h = clrArray.GetLength(1);

            int[][] columns = new int[w][];
            int[][] rows    = new int[h][];
            
            int curcnt = 0;
            List<int> curlist = new List<int>();
            int startindex = 0;

            //Columns
            int longestcol = 0;
            int longestrow = 0;
            for(int x = 0; x < w; x++)
            {
                for(int y = h-1; y > 0; y--)
                {
                    bool isBlack = clrArray[x, y];

                    if(isBlack)
                    {
                        curcnt++;
                    }
                    else if(curcnt > 0)
                    {
                        curlist.Add(curcnt);
                        curcnt = 0;
                    }
                }

                if (curlist.Count > 0)
                {
                    longestcol = Math.Max(curlist.Count - startindex, longestcol);

                    columns[x] = curlist.GetRange(startindex, curlist.Count - startindex).ToArray();
                    startindex = curlist.Count;
                }
                else
                {
                    columns[x] = new int[0];
                }
            }

            //Rows
            for (int y = 0; y < h; y++)
            {
                for (int x = w-1; x > 0; x--)
                {
                    bool isBlack = clrArray[x, y];

                    if (isBlack)
                    {
                        curcnt++;
                    }
                    else if (curcnt > 0)
                    {
                        curlist.Add(curcnt);
                        curcnt = 0;
                    }
                }

                if (curlist.Count > 0)
                {
                    longestrow = Math.Max(curlist.Count - startindex, longestrow);

                    rows[y] = curlist.GetRange(startindex, curlist.Count - startindex).ToArray();
                    startindex = curlist.Count;
                }
                else
                {
                    rows[y] = new int[0];
                }
            }

            int imgW = curimg_scaled.Width * RectSize + longestrow * RectSize;
            int imgH = curimg_scaled.Height * RectSize + longestcol * RectSize;
            Bitmap bmp = new Bitmap(imgW,imgH);
            Graphics g = Graphics.FromImage(bmp);
            Pen p = new Pen(Color.DarkGray);
            Pen pfat = new Pen(Color.Black);

            /*
            Draw the grid
            */
            //Vertical lines
            int z = 0;
            for (int x = (longestrow*RectSize)-1; x <= imgW; x += RectSize)
            {
                if ((z++) % 5 == 0)
                    g.DrawLine(pfat, x, 0, x, imgH - 1);
                else
                    g.DrawLine(p, x, 0, x, imgH-1);
            }

            //Horizontal lines
            z = 0;
            for (int y = (longestcol * RectSize) - 1; y <= imgH; y += RectSize)
            {
                if ((z++) % 5 == 0)
                    g.DrawLine(pfat, 0, y, imgW - 1, y);
                else
                    g.DrawLine(p, 0, y, imgW - 1, y);
            }

            /*
            Draw the numbers
            */
            Brush b = new SolidBrush(Color.Black);
            Font f = new Font("Arial", 10);
            for(int i = 0; i < w; i++)
            {
                int x = (longestrow * RectSize) + i * RectSize; // These coords are the topleft of the area we got to draw the number on
                int y = longestcol * RectSize - RectSize;
                for(int j = 0; j < columns[i].Length; j++)
                {
                    g.DrawString(columns[i][j].ToString(), f, b, x+2, y+2);
                    y -= RectSize;
                }
            }
            for (int i = 0; i < h; i++)
            {
                int x = longestrow * RectSize - RectSize; // These coords are the topleft of the area we got to draw the number on
                int y = (longestcol * RectSize) + i * RectSize;
                for (int j = 0; j < rows[i].Length; j++)
                {
                    g.DrawString(rows[i][j].ToString(), f, b, x + 2, y + 2);
                    x -= RectSize;
                }
            }

            var Viewer = new OutputViewer();
            Viewer.Show();

            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            Viewer.img.Source = bi;
        }

    }
}
