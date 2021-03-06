﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Brush = System.Drawing.Brush;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using System.Globalization;

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
	        dlg = new OpenFileDialog
	        {
		        DefaultExt = ".png",
		        Filter = "All Files|*.*|PNG files|*.png|JPEG files|*.jpeg|JPG files|*.jpg"
	        };
        }

        Bitmap curimg;
        Bitmap curimgScaled;

        int padT = 0;
        int padR = 0;
        int padB = 0;
        int padL = 0;

        private void UpdateSizeText()
        {
            InputSizeLabel.Content = string.Format("Input Size: {0}x{1}", curimg.Width, curimg.Height);
            ScaledSizeLabel.Content = string.Format("Scaled Size: {0}x{1}", curimgScaled.Width, curimgScaled.Height);
        }

	    const int RECT_SIZE = 20;

        private void ScaleCurImage()
        {
            if (curimg == null)
                return;

            var scale = ScaleSlider.Value;
            var maxScale = 1.2d;
            
            if (curimg.Width > 200 || curimg.Height > 200) // I don't think anyone would want to solve a bigger crossword than this
            {
                maxScale = Math.Min(maxScale, 200d / (double)curimg.Width);
                maxScale = Math.Min(maxScale, 200d / (double)curimg.Height);
            }
            scale = Math.Min(scale,maxScale);
            ScaleSlider.Maximum = maxScale;
            ScaleSlider.Value = scale;

            var neww = (int)Math.Round(curimg.Width * scale);
            var newh = (int)Math.Round(curimg.Height * scale);

            if(ClampCheckbox.IsChecked != null && ClampCheckbox.IsChecked.Value)
            {
                neww = ((int)Math.Round(neww / 5d)) * 5;
                newh = ((int)Math.Round(newh / 5d)) * 5;
            }

            neww = Math.Max(neww, 5);
            newh = Math.Max(newh, 5);

            curimgScaled = ImageUtilities.ResizeImage(curimg, neww, newh);

            UpdateSizeText();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
	        var result = dlg.ShowDialog();

	        if (result != true) return;
	        var filename = dlg.FileName;
	        FilepathTextBox.Text = filename;

	        try
	        {
		        curimg = (Bitmap)Image.FromFile(filename);
	        }
	        catch
	        {
		        // ignored
	        }

	        ScaleCurImage();
	        UpdatePreview();
        }

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

        private bool GetBwColor(Color inputclr)
        {
            if (inputclr.A == 0)
                return FlipCheckbox.IsChecked != null && !FlipCheckbox.IsChecked.Value;

            var yiq = ((inputclr.R * RedSlider.Value) + (inputclr.G * GreenSlider.Value) + (inputclr.B * BlueSlider.Value)) / (RedSlider.Value + GreenSlider.Value + BlueSlider.Value);
            if(FlipCheckbox.IsChecked != null && FlipCheckbox.IsChecked.Value)
                return yiq <= ContrastSlider.Value;
	        return yiq >= ContrastSlider.Value;
        }

        bool[,] clrArray;
	    private readonly OpenFileDialog dlg;

	    private void UpdatePreview()
        {
            if (curimgScaled == null)
                return;

            var bmp = new Bitmap((curimgScaled.Width - (padL + padR)) * RECT_SIZE, (curimgScaled.Height - (padT + padB)) * RECT_SIZE);
            var g = Graphics.FromImage(bmp);

            var rects = new RectangleF[curimgScaled.Width * curimgScaled.Height];
            clrArray = new bool[(curimgScaled.Width - (padL + padR)), (curimgScaled.Height - (padT + padB))];

            for (var y = padT; y < curimgScaled.Height-padB; y++)
            {
                for (var x = padL; x < curimgScaled.Width-padR; x++)
                {
                    if (GetBwColor(curimgScaled.GetPixel(x, y)))
                    {
                        rects[y * curimgScaled.Width + x] = new RectangleF((x-padL) * RECT_SIZE, (y-padT) * RECT_SIZE, RECT_SIZE, RECT_SIZE);
                        clrArray[x-padL, y-padT] = true;
                    }
                    else
                    {
                        clrArray[x-padL, y-padT] = false;
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

        private void Button_Generate_Click(object sender, RoutedEventArgs e)
        {
            if (clrArray == null)
                UpdatePreview();

            if (clrArray == null)
                return;

            var w = clrArray.GetLength(0);
            var h = clrArray.GetLength(1);

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
                    var isBlack = clrArray[x, y];

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
                    var isBlack = clrArray[x, y];

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

            var imgW = w * RECT_SIZE + longestrow * RECT_SIZE;
            var imgH = h * RECT_SIZE + longestcol * RECT_SIZE;
	        var viewer = new OutputViewer();
	        var ms = new MemoryStream();
            using (var bmp = new Bitmap(imgW, imgH))
	        {
		        using (var g = Graphics.FromImage(bmp))
		        {
					g.Clear(Color.White);
					g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

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
							        var s = columns[i][j].ToString();
							        var tsize = g.MeasureString(s, f);
							        g.DrawString(s, f, b, x+(RECT_SIZE/2)-(tsize.Width/2), y+(RECT_SIZE / 2) - (tsize.Height / 2));
							        y -= RECT_SIZE;
						        }
					        }
					        for (var i = 0; i < h; i++)
					        {
						        var x = longestrow * RECT_SIZE - RECT_SIZE; // These coords are the topleft of the area we got to draw the number on
						        var y = (longestcol * RECT_SIZE) + i * RECT_SIZE;
						        for (var j = 0; j < rows[i].Length; j++)
						        {
							        var s = rows[i][j].ToString();
							        var tsize = g.MeasureString(s, f);
							        g.DrawString(s, f, b, x + (RECT_SIZE / 2) - (tsize.Width / 2), y + (RECT_SIZE / 2) - (tsize.Height / 2));
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
            viewer.PreviewImage.Source = bi;
			viewer.Show();
        }

        private int StringToInt(string str)
        {
            int output = 0;
            int.TryParse(str, NumberStyles.None, NumberFormatInfo.CurrentInfo, out output); // Numberstyle is used to prevent negative numbers
            return output;
        }

        private void Padding_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            padT = StringToInt(TextBoxPaddingTop.Text);
            padR = StringToInt(TextBoxPaddingRight.Text);
            padB = StringToInt(TextBoxPaddingBottom.Text);
            padL = StringToInt(TextBoxPaddingLeft.Text);
            UpdatePreview();
        }
    }
}
