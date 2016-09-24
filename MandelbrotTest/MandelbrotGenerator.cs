using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        }

        public int Width { get; set; }
        public int Height { get; set; }

        public double MandlebrotX { get; set; } = -2;
        public double MandlebrotY { get; set; } = -2;
        public double MandlebrotWidth { get; set; } = 4;
        public double MandlebrotHeight { get; set; } = 4;

        private void WindowCoordsToMandlebrot(int x, int y, out double xnew, out double ynew)
        {
            xnew = x * MandlebrotWidth / Width + MandlebrotX;
            ynew = y * MandlebrotHeight / Height + MandlebrotY;
        }

        public unsafe Bitmap GenerateMandelbrot()
        {
            //use a bitmap and pointers to it's data for much faster drawing
            Bitmap bitmap = new Bitmap(Width, Height);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            uint* pBitmap = (uint*)bitmapData.Scan0;
            //start threads
            int nrthreads = 16;
            Task[] tasks = new Task[nrthreads];
            int nrlinesperthread = Height / nrthreads;
            int y = 0;
            for (int i = 0; i < nrthreads; i++)
            {
                MandelbrotContext c = new MandelbrotContext();
                c.bitmapPtr = pBitmap;
                c.startLine = y;
                c.nrLines = nrlinesperthread;
                c.stride = bitmapData.Stride;
                y += nrlinesperthread;
                pBitmap += bitmapData.Stride / 4 * nrlinesperthread;
                tasks[i] = Task.Factory.StartNew(MandelbrotThread, c);
            }
            Task.WaitAll(tasks);
            bitmap.UnlockBits(bitmapData);
            return bitmap;
        }

        public void Crop(int x1, int y1, int x2, int y2)
        {
            double xreal_start, yreal_start, xreal_end, yreal_end;
            WindowCoordsToMandlebrot(x1, y1, out xreal_start, out yreal_start);
            WindowCoordsToMandlebrot(x2, y2, out xreal_end, out yreal_end);
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
                MandlebrotHeight = MandlebrotWidth * Height / Width;
            }
            else
            {
                 MandlebrotWidth = MandlebrotHeight * Width / Height;
            }
        }

        private unsafe void MandelbrotThread(object arg)
        {
            MandelbrotContext c = (MandelbrotContext)arg;
            uint* pBitmap = c.bitmapPtr;
            for (int y = c.startLine; y < c.startLine + c.nrLines; y++)
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
                pBitmap += c.stride / 4;
            }
        }
    }
}
