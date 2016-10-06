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

namespace Mandelbrot
{
    public class MandelbrotGenerator
    {
        public enum MandelbrotColorStyle
        {
            SimpleRgb,
            Paletted
        }

        //Small struct to send some params to the mandelbrot generator tasks
        private unsafe struct MandelbrotContext
        {
            public uint* bitmapPtr;
            public int startLine;
            public int nrLines;
            public int stride;
            public int multiplier;
            public int width;
            public int height;
        }

        //Event that's fired when a mandelbrot picture is ready to be displayed
        public delegate void OnMandlebrotReadyEventHandler(Bitmap mandelbrot, bool is2Times);
        public event OnMandlebrotReadyEventHandler MandlebrotReady;

        //Event for handling the change of the mandelbrot palette
        //(because the propertygrid does not fire a PropertyValueChanged event for when a collection changes)
        public delegate void OnMandlebrotPaletteChangedEventHandler();
        public event OnMandlebrotPaletteChangedEventHandler MandlebrotPaletteChanged;

        private bool mCancelMandelbrot = false;
        private bool mIsGeneratingMandlebrot = false;

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

        //create a new MandelbrotGenerator based on another
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

        //To suppress the palette update event when updating from outside the ui
        public bool mIsUpdatingPalette = false;

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

        //To convert window coordinates (pixels) to mandelbrot coordinates
        private void WindowCoordsToMandlebrot(int x, int y, int multiplier, out double xnew, out double ynew)
        {
            xnew = x * MandelbrotWidth / (Width * multiplier) + MandelbrotX;
            ynew = y * MandelbrotHeight / (Height * multiplier) + MandelbrotY;
        }

        public unsafe void StartGenerateMandelbrot(bool generate2Times = false)
        {
            //When another mandelbrot is already being generated,
            //cancel it when a new mandelbrot is requested.
            //This is useful when a 2x version is generated in the bg,
            //while the use wants to zoom in already
            if (mIsGeneratingMandlebrot)
            {
                mCancelMandelbrot = true;
                //Wait till the currently being generated mandelbrot is canceled
                while (mIsGeneratingMandlebrot)
                    Thread.Sleep(1); //And sleep this thread for a little while, to give the mandelbrot threads a chance to cancel themselves
            }
            mCancelMandelbrot = false;
            mIsGeneratingMandlebrot = true;
            //make sure the palette contains at least 2 entries when using the paletted mandelbrot style
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
            //use a bitmap and pointers to it's data for much faster drawing
            int multiplier = generate2Times ? 2 : 1;
            Bitmap bitmap = new Bitmap(Width * multiplier, Height * multiplier);
            //Lock the bits of the bitmap to access the pixels using a pointer directly
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
            uint* pBitmap = (uint*)bitmapData.Scan0;
            //start multiple threads for much faster generating
            int nrthreads = 64;
            Task[] tasks = new Task[nrthreads];
            //calculate the number of lines that are calculated by each thread
            int nrlinesperthread = bitmap.Height / nrthreads;
            int y = 0;
            for (int i = 0; i < nrthreads; i++)
            {
                //Setup the params for the mandelbrot thread
                MandelbrotContext c = new MandelbrotContext();
                c.width = bitmap.Width;
                c.height = bitmap.Height;
                c.bitmapPtr = pBitmap;
                c.startLine = y;
                //In case of the last thread, make sure all lines left are being calculated,
                //because nrlinesperthread is always a whole number of lines. When the image
                //isn't a multiple of nrthreads, a couple of lines wouldn't be calculated otherwise
                if (i == nrthreads - 1)
                    c.nrLines = bitmap.Height - y;
                else
                    c.nrLines = nrlinesperthread;
                c.stride = bitmapData.Stride;
                c.multiplier = multiplier;
                y += nrlinesperthread;
                //Skip the number of lines that are calculated by this thread
                pBitmap += bitmapData.Stride / 4 * nrlinesperthread;
                //Start the thread (task)
                tasks[i] = Task.Factory.StartNew(MandelbrotThread, c);
            }
            //Wait for all tasks to be completed in a new thread,
            //to return from this method immediately
            new Thread((ThreadStart)delegate
            {
                Task.WaitAll(tasks);
                //Unlock the bitmap again (would cause a memory leak otherwise)
                bitmap.UnlockBits(bitmapData);
                if (!mCancelMandelbrot)
                {
                    mIsGeneratingMandlebrot = false;
                    //Invoke the ready event
                    if (MandlebrotReady != null)
                        MandlebrotReady.Invoke(bitmap, generate2Times);
                }
                else
                    mIsGeneratingMandlebrot = false;
            }).Start();
        }

        //Zoom in (or out) to the window (=pixel) coordinates specified
        public void Crop(int x1, int y1, int x2, int y2)
        {
            //Window coordinates to mandelbrot coordinates
            double xreal_start, yreal_start, xreal_end, yreal_end;
            WindowCoordsToMandlebrot(x1, y1, 1, out xreal_start, out yreal_start);
            WindowCoordsToMandlebrot(x2, y2, 1, out xreal_end, out yreal_end);
            //Make sure the crop won't get flipped
            CoordinateUtilities.FixRect(ref xreal_start, ref yreal_start, ref xreal_end, ref yreal_end);
            //Make sure you can't zoom out too much
            if (xreal_start < -2)
                xreal_start = -2;
            if (yreal_start < -2)
                yreal_start = -2;
            if (xreal_end > 2)
                xreal_end = 2;
            if (yreal_end > 2)
                yreal_end = 2;
            MandelbrotX = xreal_start;
            MandelbrotY = yreal_start;
            MandelbrotWidth = xreal_end - xreal_start;
            MandelbrotHeight = yreal_end - yreal_start;
            //Fix the aspect ratio of the mandelbrot figure
            FixAspect();
        }

        //Fixes the aspect ratio of the mandelbrot figure, centering the
        //currently visible part
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

        //Thread to generate a number of lines of a mandelbrot figure
        //Which lines is specified by the context that is passed through the arg parameter
        private unsafe void MandelbrotThread(object arg)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            MandelbrotContext c = (MandelbrotContext)arg;
            //Make copies of a couple of settings to prevent problems
            //when they are changed during the generating
            int maxmandel = MandelbrotStyle == MandelbrotColorStyle.Paletted ? MandlebrotMaxCount : 512;
            Color[] paletteCopy = new Color[MandelbrotPalette.Count];
            MandelbrotPalette.CopyTo(paletteCopy, 0);
            uint* pBitmap = c.bitmapPtr;
            double xStep = MandelbrotWidth / c.width;
            double yStep = MandelbrotHeight / c.height;
            double yreal = c.startLine * yStep + MandelbrotY;
            for (int y = c.startLine; y < c.startLine + c.nrLines; y++)
            {
                //make it possible to cancel generating
                if (mCancelMandelbrot)
                    return;
                uint* curLine = pBitmap;
                double xreal = MandelbrotX;
                for (int x = 0; x < c.width; x++)
                {
                    //calculate the mandelbrot number (count)
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
                    //instead of using sqrt (which is slow), simply square the other side of the equation
                    while (a * a + b * b <= 2 * 2 && count < (maxmandel - 1));
                    if (MandelbrotStyle == MandelbrotColorStyle.Paletted)
                    {
                        int idx = count * (paletteCopy.Length - 1) / maxmandel;
                        int rem = (count * (paletteCopy.Length - 1) % maxmandel) / (paletteCopy.Length - 1);
                        Color col = paletteCopy[idx];
                        if (rem != 0)
                        {
                            //Interpolate between two palette colors
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
                        //Make the maximum value (which depicts infinity to some extend) black,
                        //because it looks better
                        if (count == 511)
                            count = 0;
                        //Extract some bits and use them as r, g and b values (after scaling)
                        uint r = (uint)(count & 7) * 32;
                        uint g = (uint)((count >> 3) & 7) * 32;
                        uint b_ = (uint)((count >> 6) & 7) * 32;
                        //Set the current pixel in ARGB format
                        *curLine++ = 0xFF000000u | (r << 16) | (g << 8) | b_;
                    }
                    //next mandelbrot coordinate
                    xreal += xStep;
                }
                //next line (sometimes there is padding at the end, so use the stride and divide by 4, because it's a uint pointer)
                pBitmap += c.stride / 4;
                yreal += yStep;
            }
        }
    }
}
