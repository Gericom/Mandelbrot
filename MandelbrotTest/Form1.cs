using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MandelbrotTest
{
    public partial class Form1 : Form
    {
        MandelbrotGenerator mMandelbrotGenerator = new MandelbrotGenerator();
        Bitmap mMandelbrotBitmap = null;

        public Form1()
        {
            InitializeComponent();
            mMandelbrotGenerator.Width = Width;
            mMandelbrotGenerator.Height = Height;
            mMandelbrotGenerator.FixAspect();
            mMandelbrotGenerator.MandlebrotReady += MMandelbrotGenerator_MandlebrotReady;
            UpdateMandelbrot();
        }

        private void MMandelbrotGenerator_MandlebrotReady(Bitmap mandelbrot, bool is4Times)
        {
            mMandelbrotBitmap = mandelbrot;
            Invalidate();
            if(!is4Times)
                mMandelbrotGenerator.StartGenerateMandelbrot(true);
        }

        private void UpdateMandelbrot()
        {
            mMandelbrotGenerator.StartGenerateMandelbrot();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            //e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            if(mMandelbrotBitmap != null)
                e.Graphics.DrawImage(mMandelbrotBitmap, 0, 0, Width, Height);
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
                mMandelbrotGenerator.Crop(mMouseDownPoint.X, mMouseDownPoint.Y, e.X, e.Y);
                UpdateMandelbrot();
                //Invalidate();
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            mMandelbrotGenerator.Width = Width;
            mMandelbrotGenerator.Height = Height;
            mMandelbrotGenerator.FixAspect();
            UpdateMandelbrot();
            //Invalidate();
        }
    }
}
