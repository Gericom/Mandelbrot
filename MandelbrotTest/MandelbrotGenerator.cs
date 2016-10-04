﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MandelbrotTest
{
    public class MandelbrotGenerator
    {
        private unsafe struct MandelbrotContext
        {
            public uint* bitmapPtr;
            public int startLine;
            public int nrLines;
            public int stride;
            public int multiplier;
        }

        [Browsable(false)]
        public int Width { get; set; }
        [Browsable(false)]
        public int Height { get; set; }

        public double MandlebrotX { get; set; } = -2;
        public double MandlebrotY { get; set; } = -2;
        public double MandlebrotWidth { get; set; } = 4;
        public double MandlebrotHeight { get; set; } = 4;

        public MandelbrotGenerator(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public MandelbrotGenerator(MandelbrotGenerator template, int width, int height)
        {
            MandlebrotX = template.MandlebrotX;
            MandlebrotY = template.MandlebrotY;
            MandlebrotWidth = template.MandlebrotWidth;
            MandlebrotHeight = template.MandlebrotHeight;
            Width = width;
            Height = height;
        }

        private void WindowCoordsToMandlebrot(int x, int y, int multiplier, out double xnew, out double ynew)
        {
            xnew = x * MandlebrotWidth / (Width * multiplier) + MandlebrotX;
            ynew = y * MandlebrotHeight / (Height * multiplier) + MandlebrotY;
        }

        public delegate void OnMandlebrotReadyEventHandler(Bitmap mandelbrot, bool is2Times);

        public event OnMandlebrotReadyEventHandler MandlebrotReady;

        //private CancellationTokenSource mCancelMandelbrotSource;
        private bool mCancelMandelbrot = false;
        private bool mIsGeneratingMandlebrot = false;

        public unsafe void StartGenerateMandelbrot(bool generate2Times = false)
        {
            if (mIsGeneratingMandlebrot)
            {
                //mCancelMandelbrotSource.Cancel();
                mCancelMandelbrot = true;
                while (mIsGeneratingMandlebrot)
                    Thread.Sleep(1);
            }
            mCancelMandelbrot = false;
            mIsGeneratingMandlebrot = true;
            //mCancelMandelbrotSource = new CancellationTokenSource();
            //use a bitmap and pointers to it's data for much faster drawing
            int multiplier = generate2Times ? 2 : 1;
            Bitmap bitmap = new Bitmap(Width * multiplier, Height * multiplier);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, Width * multiplier, Height * multiplier), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            uint* pBitmap = (uint*)bitmapData.Scan0;
            //start threads
            int nrthreads = 64;
            Task[] tasks = new Task[nrthreads];
            int nrlinesperthread = Height * multiplier / nrthreads;
            int y = 0;
            for (int i = 0; i < nrthreads; i++)
            {
                MandelbrotContext c = new MandelbrotContext();
                c.bitmapPtr = pBitmap;
                c.startLine = y;
                if (i == nrthreads - 1)
                    c.nrLines = (Height * multiplier) - y;
                else
                    c.nrLines = nrlinesperthread;
                c.stride = bitmapData.Stride;
                c.multiplier = multiplier;
                y += nrlinesperthread;
                pBitmap += bitmapData.Stride / 4 * nrlinesperthread;
                tasks[i] = Task.Factory.StartNew(MandelbrotThread, c);
            }
            new Thread((ThreadStart)delegate
            {
                Task.WaitAll(tasks);
                bitmap.UnlockBits(bitmapData);
                if (!mCancelMandelbrot)
                {
                    mIsGeneratingMandlebrot = false;
                    if (MandlebrotReady != null)
                        MandlebrotReady.Invoke(bitmap, generate2Times);
                }
                else
                    mIsGeneratingMandlebrot = false;
            }).Start();
        }

        public void Crop(int x1, int y1, int x2, int y2)
        {
            double xreal_start, yreal_start, xreal_end, yreal_end;
            WindowCoordsToMandlebrot(x1, y1, 1, out xreal_start, out yreal_start);
            WindowCoordsToMandlebrot(x2, y2, 1, out xreal_end, out yreal_end);
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
            MandlebrotX = xreal_start;
            MandlebrotY = yreal_start;
            MandlebrotWidth = xreal_end - xreal_start;
            MandlebrotHeight = yreal_end - yreal_start;
            FixAspect();
        }

        public void FixAspect()
        {
            if (Width < Height)
            {
                double newheight = MandlebrotWidth * Height / Width;
                MandlebrotY += (MandlebrotHeight - newheight) / 2.0;
                MandlebrotHeight = newheight;
            }
            else
            {
                double newwidth = MandlebrotHeight * Width / Height;
                MandlebrotX += (MandlebrotWidth - newwidth) / 2.0;
                MandlebrotWidth = newwidth;
            }
        }

        private unsafe void MandelbrotThread(object arg)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            MandelbrotContext c = (MandelbrotContext)arg;
            uint* pBitmap = c.bitmapPtr;
            double xStep = MandlebrotWidth / (Width * c.multiplier);
            double yStep = MandlebrotHeight / (Height * c.multiplier);
            double yreal = c.startLine * yStep + MandlebrotY;
            for (int y = c.startLine; y < c.startLine + c.nrLines; y++)
            {
                if (mCancelMandelbrot)
                    return;
                uint* curLine = pBitmap;
                double xreal = MandlebrotX;
                for (int x = 0; x < Width * c.multiplier; x++)
                {
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
                    xreal += xStep;
                }
                //next line (sometimes there is padding at the end, so use the stride and divide by 4, because it's a uint pointer)
                pBitmap += c.stride / 4;
                yreal += yStep;
            }
        }
    }
}
