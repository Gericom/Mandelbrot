using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Drawing.Design;
using System.Collections.ObjectModel;

namespace MandelbrotTest
{
    public class MandelbrotGenerator
    {
        public enum MandelbrotColorStyle
        {
            SimpleRgb,
            Paletted
        }

        private unsafe struct MandelbrotContext
        {
            public uint* bitmapPtr;
            public int startLine;
            public int nrLines;
            public int stride;
            public int multiplier;
        }

        public delegate void OnMandlebrotPaletteChangedEventHandler();

        public event OnMandlebrotPaletteChangedEventHandler MandlebrotPaletteChanged;

        [DisplayName("Center X")]
        [Description("The center X coordinate of the mandelbrot figure.")]
        public double CenterX
        {
            get
            {
                return MandelbrotX + MandelbrotWidth / 2.0;
            }
            set
            {
                MandelbrotX = value - MandelbrotWidth / 2.0;
            }
        }

        [DisplayName("Center Y")]
        [Description("The center Y coordinate of the mandelbrot figure.")]
        public double CenterY
        {
            get
            {
                return MandelbrotY + MandelbrotHeight / 2.0;
            }
            set
            {
                MandelbrotY = value - MandelbrotHeight / 2.0;
            }
        }

        /*public double Scale
        {
            get
            {
                return 0;
            }
            set
            {
                MandelbrotX = CenterX - 400.0 * value / 2.0;
                MandelbrotY = CenterY - 400.0 * value / 2.0;
                MandelbrotWidth = MandelbrotHeight = 400.0 * value;
            }
        }*/


        [Browsable(false)]
        public int Width { get; set; }
        [Browsable(false)]
        public int Height { get; set; }

        [Browsable(false)]
        public double MandelbrotX { get; set; } = -2;
        [Browsable(false)]
        public double MandelbrotY { get; set; } = -2;
        [DisplayName("Width")]
        [Description("The width of the figure in mandelbrot coordinates.")]
        public double MandelbrotWidth { get; set; } = 4;
        [DisplayName("Height")]
        [Description("The height of the figure in mandelbrot coordinates.")]
        public double MandelbrotHeight { get; set; } = 4;

        private int mMandlebrotMaxCount = 128;
        [DisplayName("Max Count")]
        [Description("The maximum number of iterations done.")]
        public int MandlebrotMaxCount
        {
            get
            {
                if (MandelbrotStyle == MandelbrotColorStyle.SimpleRgb)
                    return 512;
                else
                    return mMandlebrotMaxCount;
            }
            set
            {
                if (MandelbrotStyle == MandelbrotColorStyle.SimpleRgb && value != 512)
                    throw new Exception("This value is fixed to 512 in SimpleRgb mode!");
                mMandlebrotMaxCount = value;
            }
        }

        [DisplayName("Style")]
        [Description("The way the mandlebrot figure is colored.")]
        [Category("Coloring")]
        public MandelbrotColorStyle MandelbrotStyle { get; set; } = MandelbrotColorStyle.SimpleRgb;

        [DisplayName("Palette")]
        [Description("The palette for the paletted mandelbrot style.")]
        [Category("Coloring")]
        public ObservableCollection<Color> MandelbrotPalette { get; } = new ObservableCollection<Color>();

        public bool mIsUpdatingPalette = false;

        public MandelbrotGenerator(int width, int height)
        {
            Width = width;
            Height = height;
            MandelbrotPalette.Add(Color.DarkBlue);
            MandelbrotPalette.Add(Color.White);
            MandelbrotPalette.Add(Color.Orange);
            MandelbrotPalette.Add(Color.White);
            MandelbrotPalette.Add(Color.Black);
            MandelbrotPalette.CollectionChanged += MandelbrotPalette_CollectionChanged;
        }

        public void BeginUpdatePalette()
        {
            mIsUpdatingPalette = true;
        }

        public void EndUpdatePalette()
        {
            mIsUpdatingPalette = false;
        }

        private void MandelbrotPalette_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!mIsUpdatingPalette && e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (MandlebrotPaletteChanged != null)
                    MandlebrotPaletteChanged.Invoke();
            }
        }

        public MandelbrotGenerator(MandelbrotGenerator template, int width, int height)
            : this(width, height)
        {
            MandelbrotX = template.MandelbrotX;
            MandelbrotY = template.MandelbrotY;
            MandelbrotWidth = template.MandelbrotWidth;
            MandelbrotHeight = template.MandelbrotHeight;
            MandelbrotStyle = template.MandelbrotStyle;
            mMandlebrotMaxCount = template.mMandlebrotMaxCount;
            BeginUpdatePalette();
            MandelbrotPalette.Clear();
            foreach (Color c in template.MandelbrotPalette)
                MandelbrotPalette.Add(c);
            EndUpdatePalette();
        }

        private void WindowCoordsToMandlebrot(int x, int y, int multiplier, out double xnew, out double ynew)
        {
            xnew = x * MandelbrotWidth / (Width * multiplier) + MandelbrotX;
            ynew = y * MandelbrotHeight / (Height * multiplier) + MandelbrotY;
        }

        public delegate void OnMandlebrotReadyEventHandler(Bitmap mandelbrot, bool is2Times);

        public event OnMandlebrotReadyEventHandler MandlebrotReady;

        private bool mCancelMandelbrot = false;
        private bool mIsGeneratingMandlebrot = false;

        public unsafe void StartGenerateMandelbrot(bool generate2Times = false)
        {
            if (mIsGeneratingMandlebrot)
            {
                mCancelMandelbrot = true;
                while (mIsGeneratingMandlebrot)
                    Thread.Sleep(1);
            }
            mCancelMandelbrot = false;
            mIsGeneratingMandlebrot = true;
            if (MandelbrotStyle == MandelbrotColorStyle.Paletted && MandelbrotPalette.Count < 2)
            {
                BeginUpdatePalette();
                MandelbrotPalette.Clear();
                MandelbrotPalette.Add(Color.DarkBlue);
                MandelbrotPalette.Add(Color.White);
                MandelbrotPalette.Add(Color.Orange);
                MandelbrotPalette.Add(Color.White);
                MandelbrotPalette.Add(Color.Black);
                EndUpdatePalette();
            }
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
            MandelbrotX = xreal_start;
            MandelbrotY = yreal_start;
            MandelbrotWidth = xreal_end - xreal_start;
            MandelbrotHeight = yreal_end - yreal_start;
            FixAspect();
        }

        public void FixAspect()
        {
            if (Width < Height)
            {
                double newheight = MandelbrotWidth * Height / Width;
                MandelbrotY += (MandelbrotHeight - newheight) / 2.0;
                MandelbrotHeight = newheight;
            }
            else
            {
                double newwidth = MandelbrotHeight * Width / Height;
                MandelbrotX += (MandelbrotWidth - newwidth) / 2.0;
                MandelbrotWidth = newwidth;
            }
        }

        private unsafe void MandelbrotThread(object arg)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            MandelbrotContext c = (MandelbrotContext)arg;
            int maxmandel = MandelbrotStyle == MandelbrotColorStyle.Paletted ? MandlebrotMaxCount : 512;
            Color[] paletteCopy = new Color[MandelbrotPalette.Count];
            MandelbrotPalette.CopyTo(paletteCopy, 0);
            uint * pBitmap = c.bitmapPtr;
            double xStep = MandelbrotWidth / (Width * c.multiplier);
            double yStep = MandelbrotHeight / (Height * c.multiplier);
            double yreal = c.startLine * yStep + MandelbrotY;
            for (int y = c.startLine; y < c.startLine + c.nrLines; y++)
            {
                if (mCancelMandelbrot)
                    return;
                uint* curLine = pBitmap;
                double xreal = MandelbrotX;
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
                    while (a * a + b * b <= 2 * 2 && count < (maxmandel - 1));
                    if (MandelbrotStyle == MandelbrotColorStyle.Paletted)
                    {
                        int idx = count * (paletteCopy.Length - 1) / maxmandel;
                        int rem = (count * (paletteCopy.Length - 1) % maxmandel) / (paletteCopy.Length - 1);
                        Color col = paletteCopy[idx];
                        if (rem != 0)
                        {
                            Color col2 = paletteCopy[idx + 1];
                            col = Color.FromArgb(
                                col.R + (col2.R - col.R) * rem / (maxmandel / (paletteCopy.Length - 1)),
                                col.G + (col2.G - col.G) * rem / (maxmandel / (paletteCopy.Length - 1)),
                                col.B + (col2.B - col.B) * rem / (maxmandel / (paletteCopy.Length - 1)));
                        }
                        *curLine++ = (uint)col.ToArgb();
                    }
                    else
                    {
                        if (count == 511)
                            count = 0;
                        uint r = (uint)(count & 7) * 32;
                        uint g = (uint)((count >> 3) & 7) * 32;
                        uint b_ = (uint)((count >> 6) & 7) * 32;
                        *curLine++ = 0xFF000000u | (r << 16) | (g << 8) | b_;
                    }
                    xreal += xStep;
                }
                //next line (sometimes there is padding at the end, so use the stride and divide by 4, because it's a uint pointer)
                pBitmap += c.stride / 4;
                yreal += yStep;
            }
        }
    }
}
