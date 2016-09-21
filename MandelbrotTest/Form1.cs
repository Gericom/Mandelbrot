using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MandelbrotTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        double mMandlebrotX = -2;
        double mMandlebrotY = -2;
        double mMandlebrotWidth = 4;
        double mMandlebrotHeight = 4;

        private void WindowCoordsToMandlebrot(int x, int y, out double xnew, out double ynew)
        {
            xnew = x * mMandlebrotWidth / Width + mMandlebrotX;
            ynew = y * mMandlebrotHeight / Height + mMandlebrotY;
        }

        private unsafe void Form1_Paint(object sender, PaintEventArgs e)
        {
            //use a bitmap and pointers to it's data for much faster drawing
            Bitmap bitmap = new Bitmap(Width, Height);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            uint* pBitmap = (uint*)bitmapData.Scan0;
            for (int y = 0; y < Height; y++)
            {
                uint* curLine = pBitmap;
                for (int x = 0; x < Width; x++)
                {
                    double xreal, yreal;
                    WindowCoordsToMandlebrot(x, y, out xreal, out yreal);
                    double a = 0;
                    double b = 0;
                    int count = 0;
                    do
                    {
                        double a_old = a;
                        a = a * a - b * b + xreal;
                        b = 2 * a_old * b + yreal;
                        count++;
                    }
                    while (a * a + b * b <= 2 * 2 && count <= 511);
                    uint r = (uint)(count & 7) * 32;
                    uint g = (uint)((count >> 3) & 7) * 32;
                    uint b_ = (uint)((count >> 6) & 7) * 32;
                    *curLine++ = 0xFF000000u | (r << 16) | (g << 8) | b_;
                }
                //next line (sometimes there is padding at the end, so use the stride and divide by 4, because it's a uint pointer)
                pBitmap += bitmapData.Stride / 4;
            }
            bitmap.UnlockBits(bitmapData);
            e.Graphics.DrawImage(bitmap, 0, 0);
        }

        Point mMouseDownPoint;
        bool mIsMouseDown = false;

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            mIsMouseDown = true;
            mMouseDownPoint = e.Location;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mIsMouseDown = false;
                double xreal_start, yreal_start, xreal_end, yreal_end;
                WindowCoordsToMandlebrot(mMouseDownPoint.X, mMouseDownPoint.Y, out xreal_start, out yreal_start);
                WindowCoordsToMandlebrot(e.X, e.Y, out xreal_end, out yreal_end);
                //make sure the crop won't get flipped 
                if (xreal_start > xreal_end)
                {
                    double tmp = xreal_end;
                    xreal_end = xreal_start;
                    xreal_start = tmp;
                }
                if (yreal_start > yreal_end)
                {
                    double tmp = yreal_end;
                    yreal_end = yreal_start;
                    yreal_start = tmp;
                }
                mMandlebrotX = xreal_start;
                mMandlebrotY = yreal_start;
                mMandlebrotWidth = xreal_end - xreal_start;
                mMandlebrotHeight = yreal_end - yreal_start;
                Invalidate();
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}
