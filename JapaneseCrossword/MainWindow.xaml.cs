using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace JapaneseCrossword
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
	        InitializeComponent();
	        _dlg = new OpenFileDialog
	        {
		        DefaultExt = ".png",
		        Filter = "All Files|*.*|PNG files|*.png|JPEG files|*.jpeg|JPG files|*.jpg"
	        };
        }

	    const int RECT_SIZE = 20;

        Bitmap _curimg;
        Bitmap _curimgScaled;
        private void ScaleCurImage()
        {
            if (_curimg == null)
                return;

            var scale = ScaleSlider.Value;
            if (Math.Abs(scale - 1) < 0f)
            {
                _curimgScaled = _curimg;
                InputSizeLabel.Content = string.Format("Input Size: {0}x{1}", _curimg.Width, _curimg.Height);
                ScaledSizeLabel.Content = string.Format("Scaled Size: {0}x{1}", _curimg.Width, _curimg.Height);
                return;
            }
            var neww = (int)Math.Round(_curimg.Width * scale);
            var newh = (int)Math.Round(_curimg.Height * scale);

            if(ClampCheckbox.IsChecked != null && ClampCheckbox.IsChecked.Value)
            {
                neww = ((int)Math.Round(neww / 5d)) * 5;
                newh = ((int)Math.Round(newh / 5d)) * 5;
            }

            neww = Math.Max(neww, 5);
            newh = Math.Max(newh, 5);

            InputSizeLabel.Content = string.Format("Input Size: {0}x{1}", _curimg.Width, _curimg.Height);
            ScaledSizeLabel.Content = string.Format("Scaled Size: {0}x{1}", neww, newh);

            _curimgScaled = ImageUtilities.ResizeImage(_curimg, neww, newh);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
	        var result = _dlg.ShowDialog();

	        if (result != true) return;
	        var filename = _dlg.FileName;
	        FilepathTextBox.Text = filename;

	        try
	        {
		        _curimg = (Bitmap)Image.FromFile(filename);
	        }
	        catch
	        {
		        // ignored
	        }

	        ScaleCurImage();
	        UpdatePreview();
        }

/*
        private static Action _emptyDelegate = delegate { };
*/
        private void SetPreviewBitmap(Bitmap bmp)
        {
            var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            PreviewImage.Source = bi;
        }

/*
        private Color GetBwColor(Color inputclr)
        {
            var yiq = ((inputclr.R * sldr_red.Value) + (inputclr.G * sldr_green.Value) + (inputclr.B * sldr_blue.Value)) / (sldr_red.Value + sldr_green.Value + sldr_blue.Value);
            return yiq >= 128 ? Color.Black : Color.LightGray;
        }
*/

        private bool GetBwColorBool(Color inputclr)
        {
            if (inputclr.A == 0)
                return FlipCheckbox.IsChecked != null && !FlipCheckbox.IsChecked.Value;

            var yiq = ((inputclr.R * RedSlider.Value) + (inputclr.G * GreenSlider.Value) + (inputclr.B * BlueSlider.Value)) / (RedSlider.Value + GreenSlider.Value + BlueSlider.Value);
            if(FlipCheckbox.IsChecked != null && FlipCheckbox.IsChecked.Value)
                return yiq <= ContrastSlider.Value;
	        return yiq >= ContrastSlider.Value;
        }

        bool[,] _clrArray;
	    private readonly OpenFileDialog _dlg;

	    private void UpdatePreview()
        {
            if (_curimgScaled == null)
                return;

            var bmp = new Bitmap(_curimgScaled.Width * RECT_SIZE, _curimgScaled.Height * RECT_SIZE);
            var g = Graphics.FromImage(bmp);

            var rects = new RectangleF[_curimgScaled.Width * _curimgScaled.Height];
            _clrArray = new bool[_curimgScaled.Width, _curimgScaled.Height];
            for (var y = 0; y < _curimgScaled.Height; y++)
            {
                for (var x = 0; x < _curimgScaled.Width; x++)
                {
                    if (GetBwColorBool(_curimgScaled.GetPixel(x, y)))
                    {
                        rects[y * _curimgScaled.Width + x] = new RectangleF(x * RECT_SIZE, y * RECT_SIZE, RECT_SIZE, RECT_SIZE);
                        _clrArray[x, y] = true;
                    }
                    else
                    {
                        _clrArray[x, y] = false;
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

/*
        private int[] PopFill(Stack<int> stack)
        {
            var arr = new int[stack.Count];
            for (var i = 0; i < arr.Length; i++)
                arr[i] = stack.Pop();
            return arr;
        }
*/

        private void Button_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (_clrArray == null)
                UpdatePreview();

            if (_clrArray == null)
                return;

            var w = _clrArray.GetLength(0);
            var h = _clrArray.GetLength(1);

            var columns = new int[w][];
            var rows    = new int[h][];
            
            var curcnt = 0;
            var curlist = new List<int>();
            var startindex = 0;

            //Columns
            var longestcol = 0;
            var longestrow = 0;
            for(var x = 0; x < w; x++)
            {
                for(var y = h-1; y > 0; y--)
                {
                    var isBlack = _clrArray[x, y];

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
            for (var y = 0; y < h; y++)
            {
                for (var x = w-1; x > 0; x--)
                {
                    var isBlack = _clrArray[x, y];

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

            var imgW = _curimgScaled.Width * RECT_SIZE + longestrow * RECT_SIZE;
            var imgH = _curimgScaled.Height * RECT_SIZE + longestcol * RECT_SIZE;
	        var viewer = new OutputViewer();
	        var ms = new MemoryStream();
	        using (var bmp = new Bitmap(imgW, imgH))
	        {
		        using (var g = Graphics.FromImage(bmp))
		        {
			        using (Pen p = new Pen(Color.DarkGray), pfat = new Pen(Color.Black))
			        {
				        var z = 0;
				        for (var x = (longestrow*RECT_SIZE)-1; x <= imgW; x += RECT_SIZE)
				        {
					        g.DrawLine((z++)%5 == 0 ? pfat : p, x, 0, x, imgH - 1);
				        }

				        //Horizontal lines
				        z = 0;
				        for (var y = (longestcol * RECT_SIZE) - 1; y <= imgH; y += RECT_SIZE)
				        {
					        g.DrawLine((z++)%5 == 0 ? pfat : p, 0, y, imgW - 1, y);
				        }
			        }

			        /*
            Draw the numbers
            */
			        using (Brush b = new SolidBrush(Color.Black))
			        {
				        using (var f = new Font("Arial", 10))
				        {
					        for(var i = 0; i < w; i++)
					        {
						        var x = (longestrow * RECT_SIZE) + i * RECT_SIZE; // These coords are the topleft of the area we got to draw the number on
						        var y = longestcol * RECT_SIZE - RECT_SIZE;
						        for(var j = 0; j < columns[i].Length; j++)
						        {
							        g.DrawString(columns[i][j].ToString(), f, b, x+2, y+2);
							        y -= RECT_SIZE;
						        }
					        }
					        for (var i = 0; i < h; i++)
					        {
						        var x = longestrow * RECT_SIZE - RECT_SIZE; // These coords are the topleft of the area we got to draw the number on
						        var y = (longestcol * RECT_SIZE) + i * RECT_SIZE;
						        for (var j = 0; j < rows[i].Length; j++)
						        {
							        g.DrawString(rows[i][j].ToString(), f, b, x + 2, y + 2);
							        x -= RECT_SIZE;
						        }
					        }
				        }
			        }
		        }

		       
		        bmp.Save(ms, ImageFormat.Png);
	        }
	        ms.Position = 0;
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            viewer.img.Source = bi;
			viewer.Show();
        }

    }
}
