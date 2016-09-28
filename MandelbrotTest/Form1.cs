using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        bool mMandelbrotBitmapX2 = false;

        RectangleAnimator mMandlebrotZoomer = null;

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

        private void MMandelbrotGenerator_MandlebrotReady(Bitmap mandelbrot, bool is2Times)
        {
            while (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished) ;
            mMandlebrotZoomer = null;
            mMandelbrotBitmapX2 = is2Times;
            mMandelbrotBitmap = mandelbrot;
            panel1.Invalidate();
            if (!is2Times)
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
            {
                if (mMandlebrotZoomer != null)
                {
                    Rectangle bmRegion = mMandlebrotZoomer.GetLatestValue();
                    if (mMandelbrotBitmapX2)
                    {
                        bmRegion.X *= 2;
                        bmRegion.Y *= 2;
                        bmRegion.Width *= 2;
                        bmRegion.Height *= 2;
                    }
                    e.Graphics.DrawImage(mMandelbrotBitmap, new Rectangle(0, 0, panel1.Width, panel1.Height), bmRegion, GraphicsUnit.Pixel);
                }
                else
                    e.Graphics.DrawImage(mMandelbrotBitmap, 0, 0, panel1.Width, panel1.Height);
            }
        }

        private void panel1_SizeChanged(object sender, EventArgs e)
        {
            mMandelbrotGenerator.Width = panel1.Width;
            mMandelbrotGenerator.Height = panel1.Height;
            mMandelbrotGenerator.FixAspect();
            UpdateMandelbrot();
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
                int x1 = mMouseDownPoint.X;
                int y1 = mMouseDownPoint.Y;
                int x2 = e.X;
                int y2 = e.Y;
                if (x1 > x2)
                {
                    int tmp = x2;
                    x2 = x1;
                    x1 = tmp;
                }
                if (y1 > y2)
                {
                    int tmp = y2;
                    y2 = y1;
                    y1 = tmp;
                }

                mMandlebrotZoomer = 
                    new RectangleAnimator(
                        new Rectangle(0, 0, panel1.Width, panel1.Height), 
                        new Rectangle(x1, y1, x2 - x1, y2 - y1), 10);
                new Thread(AnimThread) { Priority = ThreadPriority.Highest }.Start();
                UpdateMandelbrot();
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            
        }

        private void AnimThread()
        {
            Stopwatch s = new Stopwatch();
            while (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished)
            {
                s.Reset();
                s.Start();
                mMandlebrotZoomer.AdvanceFrame();
                panel1.Invalidate();
                if (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished)
                {
                    while (s.ElapsedMilliseconds < 60);
                }
            }
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdateMandelbrot();
        }
    }
}
