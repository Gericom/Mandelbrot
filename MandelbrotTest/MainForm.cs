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
using System.IO;

namespace MandelbrotTest
{
    public partial class MainForm : Form
    {
        MandelbrotGenerator mMandelbrotGenerator;
        Bitmap mMandelbrotBitmap = null;
        bool mMandelbrotBitmapX2 = false;

        RectangleAnimator mMandlebrotZoomer = null;

        MandelbrotPresets mPresets = new MandelbrotPresets();

        bool mWaitingForBrot = false;

        public MainForm()
        {
            InitializeComponent();
            mMandelbrotGenerator = new MandelbrotGenerator(Width, Height);
            mMandelbrotGenerator.Width = panel1.Width;
            mMandelbrotGenerator.Height = panel1.Height;
            mMandelbrotGenerator.FixAspect();
            mMandelbrotGenerator.MandlebrotReady += MMandelbrotGenerator_MandlebrotReady;
            mMandelbrotGenerator.MandlebrotPaletteChanged += MMandelbrotGenerator_MandlebrotPaletteChanged;
            MouseWheel += Form1_MouseWheel;
            UpdateMandelbrot();
            propertyGrid1.SelectedObject = mMandelbrotGenerator;
            //Load the presets from presets.xml (if it exists), and use the defaults otherwise
            string xmlpath = Path.GetDirectoryName(Application.ExecutablePath) + "\\presets.xml";
            if (File.Exists(xmlpath))
            {
                //try loading
                try
                {
                    mPresets = MandelbrotPresets.FromXml(File.ReadAllText(xmlpath));
                }
                catch
                {
                    mPresets = MandelbrotPresets.GenerateDefault();
                    SavePresets();
                }
            }
            else
            {
                mPresets = MandelbrotPresets.GenerateDefault();
                SavePresets();
            }
            UpdatePresets();
        }

        //Update the presets in the listbox
        private void UpdatePresets()
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (MandelbrotPresets.MandelbrotPreset p in mPresets.presets)
                listBox1.Items.Add(p.name);
            listBox1.EndUpdate();
        }

        //Save the presets to presets.xml
        private void SavePresets()
        {
            string xmlpath = Path.GetDirectoryName(Application.ExecutablePath) + "\\presets.xml";
            File.WriteAllText(xmlpath, mPresets.ToXml());
        }

        bool mMandelbrotPaletteChanged = false;
        private void MMandelbrotGenerator_MandlebrotPaletteChanged()
        {
            mMandelbrotPaletteChanged = true;
        }

        //This implements google maps like zooming with the scroll wheel
        //This means that the position of your mouse pointer will be the center of the zoom,
        //what you're pointing at won't change position.
        //To achieve this, first the new width and height (in pixels) of the area is calculated
        //and used to get coords with the mouse pointer as middle
        // ---------------
        //|               |
        //|       o       |
        //|               |
        // ---------------
        //Then these coordinates are moved in such a way that the point the mouse
        //pointer points at isn't in the middle, but at the mouse point instead
        // ↗             ↗
        // ---------------
        //|               |
        //|               |
        //|  o            |
        // ---------------
        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished) || mWaitingForBrot)
                return;
            //Calculate the real number of mouse wheel clicks
            int RealDelta = e.Delta / SystemInformation.MouseWheelScrollDelta;
            //Convert window relative to panel1 relative coordinates
            Point panel1lt = panel1.PointToClient(PointToScreen(e.Location));
            int realx = panel1lt.X;
            int realy = panel1lt.Y;
            if (RealDelta < 0) //zoom out
            {
                //Make the realdelta positive
                RealDelta = -RealDelta;
                //For each mouse wheel click, the visible area is doubled,
                //so for multiple clicks it's 2^RealDelta
                int scale = (int)Math.Pow(2, RealDelta);
                int newWidth = panel1.Width * scale;
                int newHeight = panel1.Height * scale;
                //Do the actual zoom with a nice animation
                AnimateZoom(
                        realx - newWidth / 2 - (realx - panel1.Width / 2) * scale,
                        realy - newHeight / 2 - (realy - panel1.Height / 2) * scale,
                        realx + newWidth / 2 - (realx - panel1.Width / 2) * scale,
                        realy + newHeight / 2 - (realy - panel1.Height / 2) * scale
                    );
            }
            else //zoom in
            {
                //For each mouse wheel click, the visible area is halved,
                //so for multiple clicks it's 2^RealDelta
                int scale = (int)Math.Pow(2, RealDelta);
                int newWidth = panel1.Width / scale;
                int newHeight = panel1.Height / scale;
                //Do the actual zoom with a nice animation
                AnimateZoom(
                        realx - newWidth / 2 - (realx - panel1.Width / 2) / scale,
                        realy - newHeight / 2 - (realy - panel1.Height / 2) / scale,
                        realx + newWidth / 2 - (realx - panel1.Width / 2) / scale,
                        realy + newHeight / 2 - (realy - panel1.Height / 2) / scale
                    );
            }
        }

        Point mMouseDownPoint;
        Point mCurMousePoint;
        bool mIsMouseDown = false;

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (mMandelbrotBitmap != null)
            {
                if (mMandlebrotZoomer != null)
                {
                    //Draw a zoomed mandelbrot image when the animation is running
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
            if (mIsMouseDown)
            {
                //Draw a nice box when zooming in by mouse drag
                int x1 = mMouseDownPoint.X;
                int x2 = mCurMousePoint.X;
                int y1 = mMouseDownPoint.Y;
                int y2 = mCurMousePoint.Y;
                CoordinateUtilities.FixRect(ref x1, ref y1, ref x2, ref y2);
                e.Graphics.DrawRectangle(Pens.White, x1, y1, x2 - x1, y2 - y1);
            }
        }

        //Update the mandelbrot when the size of the panel changed
        private void panel1_SizeChanged(object sender, EventArgs e)
        {
            if (mMandelbrotGenerator != null)
            {
                mMandelbrotGenerator.Width = panel1.Width;
                mMandelbrotGenerator.Height = panel1.Height;
                mMandelbrotGenerator.FixAspect();
                UpdateMandelbrot();
            }
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            mIsMouseDown = true;
            mMouseDownPoint = e.Location;
            mCurMousePoint = e.Location;
        }

        //Update the box when drag-zooming
        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mCurMousePoint = e.Location;
                panel1.Invalidate();
            }
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mIsMouseDown = false;
                //Finalize the drag-zoom by animating it
                AnimateZoom(mMouseDownPoint.X, mMouseDownPoint.Y, e.X, e.Y);
            }
        }

        //Displays the new image when one was being generated in the bg
        private void MMandelbrotGenerator_MandlebrotReady(Bitmap mandelbrot, bool is2Times)
        {
            //Wait for the animation to be finished (to prevent problems)
            while (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished) ;
            mMandlebrotZoomer = null;
            mWaitingForBrot = false;
            mMandelbrotBitmapX2 = is2Times;
            mMandelbrotBitmap = mandelbrot;
            panel1.Invalidate();
            //When the 2 times bigger picture hasn't been generated yet,
            //do so
            if (!is2Times)
                mMandelbrotGenerator.StartGenerateMandelbrot(true);
        }

        //Update the displayed mandelbrot image (async)
        private void UpdateMandelbrot()
        {
            propertyGrid1.SelectedObject = mMandelbrotGenerator;
            mWaitingForBrot = true;
            mMandelbrotGenerator.StartGenerateMandelbrot();
        }

        //Zoom the mandelbrot with a nice animation
        private void AnimateZoom(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2 || y1 == y2)
                return;
            //Make sure the top-left and bottom-right coords are not reversed
            CoordinateUtilities.FixRect(ref x1, ref y1, ref x2, ref y2);
            mMandelbrotGenerator.Crop(x1, y1, x2, y2);
            //Setup the RectangleAnimator
            mMandlebrotZoomer =
                new RectangleAnimator(
                    new Rectangle(0, 0, panel1.Width, panel1.Height),
                    new Rectangle(x1, y1, x2 - x1, y2 - y1), 10);
            //Start the animation thread with a high priority to get the smoothest animation possible
            new Thread(AnimThread) { Priority = ThreadPriority.Highest }.Start();
            UpdateMandelbrot();
        }

        //Runs the zoom animation in the bg
        private void AnimThread()
        {
            //Use a stopwatch to time the frames
            Stopwatch s = new Stopwatch();
            while (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished)
            {
                s.Reset();
                s.Start();
                mMandlebrotZoomer.AdvanceFrame();
                panel1.Invalidate();
                if (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished)
                {
                    //Wait for one frame to be complete
                    while (s.ElapsedMilliseconds < 60) ;
                }
            }
        }

        //Update the mandelbrot when something changes in the property grid
        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdateMandelbrot();
        } 

        //And when the palette has been changed and a different property grid item has been selected
        private void propertyGrid1_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            if (mMandelbrotPaletteChanged)
            {
                mMandelbrotPaletteChanged = false;
                UpdateMandelbrot();
            }
        }

        //Load the preset when one has been selected
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndices.Count == 0)
            {
                toolStripButton2.Enabled = false;
                return;
            }
            toolStripButton2.Enabled = true;
            mPresets.presets[listBox1.SelectedIndex].ApplyToGenerator(mMandelbrotGenerator);
            UpdateMandelbrot();
        }

        //When the reset presets button has been clicked
        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to result the presets to the defaults?", "Reset", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                mPresets = MandelbrotPresets.GenerateDefault();
                SavePresets();
                UpdatePresets();
            }
        }

        //When the add preset button has been clicked
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //Ask for a name
            string name = Microsoft.VisualBasic.Interaction.InputBox("Enter a name for the new preset:", "New Preset", null);
            if (name == null || name.Trim().Length == 0) return;
            name = name.Trim();//remove useless whitespace at the start and end of the name
            //Look if a name doesn't exist already (case-insensitive)
            foreach (var p in mPresets.presets)
            {
                if (p.name.Trim().ToLower() == name.ToLower())
                {
                    MessageBox.Show("This name is already in use!");
                    return;
                }
            }
            mPresets.presets.Add(new MandelbrotPresets.MandelbrotPreset(mMandelbrotGenerator, name));
            //Save and update the presets
            SavePresets();
            UpdatePresets();
        }

        //When the remove preset button has been clicked
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndices.Count == 0)
            {
                toolStripButton2.Enabled = false;
                return;
            }
            if (MessageBox.Show("Are you sure you want to remove this preset?", "Remove", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                mPresets.presets.RemoveAt(listBox1.SelectedIndex);
                SavePresets();
                UpdatePresets();
            }
        }

        //When the export button has been clicked
        private void exportToolStripButton_Click(object sender, EventArgs e)
        {
            //Create a new MandelExporterForm and show it
            new MandelExporterForm(mMandelbrotGenerator).ShowDialog();
        }
    }
}
