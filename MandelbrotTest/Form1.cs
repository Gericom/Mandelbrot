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
            //Set the protected DoubleBuffered property of the panel to true, by using reflection
            panel1.GetType()
                .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(panel1, true);
            mMandelbrotGenerator.Width = panel1.Width;
            mMandelbrotGenerator.Height = panel1.Height;
            mMandelbrotGenerator.FixAspect();
            mMandelbrotGenerator.MandlebrotReady += MMandelbrotGenerator_MandlebrotReady;
            UpdateMandelbrot();
            propertyGrid1.SelectedObject = mMandelbrotGenerator;
        }

        private void MMandelbrotGenerator_MandlebrotReady(Bitmap mandelbrot, bool is4Times)
        {
            mMandelbrotBitmap = mandelbrot;
            panel1.Invalidate();
            if(!is4Times)
                mMandelbrotGenerator.StartGenerateMandelbrot(true);
        }

        private void UpdateMandelbrot()
        {
            propertyGrid1.SelectedObject = mMandelbrotGenerator;
            mMandelbrotGenerator.StartGenerateMandelbrot();
        }

        Point mMouseDownPoint;
        bool mIsMouseDown = false;

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (mMandelbrotBitmap != null)
                e.Graphics.DrawImage(mMandelbrotBitmap, 0, 0, panel1.Width, panel1.Height);
        }

        private void panel1_SizeChanged(object sender, EventArgs e)
        {
            mMandelbrotGenerator.Width = panel1.Width;
            mMandelbrotGenerator.Height = panel1.Height;
            mMandelbrotGenerator.FixAspect();
            UpdateMandelbrot();
            //Invalidate();
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mIsMouseDown = true;
            mMouseDownPoint = e.Location;
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mIsMouseDown = false;
                mMandelbrotGenerator.Crop(mMouseDownPoint.X, mMouseDownPoint.Y, e.X, e.Y);
                UpdateMandelbrot();
            }
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdateMandelbrot();
        }
    }
}
