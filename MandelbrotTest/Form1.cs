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

        MandelbrotGenerator mMandelbrotGenerator = new MandelbrotGenerator();

        private unsafe void Form1_Paint(object sender, PaintEventArgs e)
        {
            mMandelbrotGenerator.Width = Width;// * 4;
            mMandelbrotGenerator.Height = Height;// * 4;
            mMandelbrotGenerator.FixAspect();
           // e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            e.Graphics.DrawImage(mMandelbrotGenerator.GenerateMandelbrot(), 0, 0, Width, Height);
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
                Invalidate();
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }
}
