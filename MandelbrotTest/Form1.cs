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
    public partial class Form1 : Form
    {
        MandelbrotGenerator mMandelbrotGenerator;
        Bitmap mMandelbrotBitmap = null;
        bool mMandelbrotBitmapX2 = false;

        RectangleAnimator mMandlebrotZoomer = null;

        MandelbrotPresets mPresets = new MandelbrotPresets();

        bool mWaitingForBrot = false;

        public Form1()
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

        private void UpdatePresets()
        {
            listBox1.BeginUpdate();
            listBox1.Items.Clear();
            foreach (MandelbrotPresets.MandelbrotPreset p in mPresets.presets)
                listBox1.Items.Add(p.name);
            listBox1.EndUpdate();
        }

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

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished) || mWaitingForBrot)
                return;
            int RealDelta = e.Delta / SystemInformation.MouseWheelScrollDelta;
            Point panel1lt = panel1.PointToClient(PointToScreen(e.Location));
            int realx = panel1lt.X;
            int realy = panel1lt.Y;
            if (RealDelta < 0) //uitzoomen 
            {
                RealDelta = -RealDelta;
                int scale = (int)Math.Pow(2, RealDelta);
                int newWidth = panel1.Width * scale;
                int newHeight = panel1.Height * scale;
                //bereken x1, y1
                AnimateZoom(
                        realx - newWidth / 2 - (realx - panel1.Width / 2) * scale,
                        realy - newHeight / 2 - (realy - panel1.Height / 2) * scale,
                        realx + newWidth / 2 - (realx - panel1.Width / 2) * scale,
                        realy + newHeight / 2 - (realy - panel1.Height / 2) * scale
                    );
            }
            else    // inzoomen
            {
                int scale = (int)Math.Pow(2, RealDelta);
                int newWidth = panel1.Width / scale;
                int newHeight = panel1.Height / scale;
                //bereken x1, y1
                AnimateZoom(
                        realx - newWidth / 2 - (realx - panel1.Width / 2) / scale,
                        realy - newHeight / 2 - (realy - panel1.Height / 2) / scale,
                        realx + newWidth / 2 - (realx - panel1.Width / 2) / scale,
                        realy + newHeight / 2 - (realy - panel1.Height / 2) / scale
                    );
            }
        }

        private void MMandelbrotGenerator_MandlebrotReady(Bitmap mandelbrot, bool is2Times)
        {
            while (mMandlebrotZoomer != null && !mMandlebrotZoomer.IsFinished) ;
            mMandlebrotZoomer = null;
            mWaitingForBrot = false;
            mMandelbrotBitmapX2 = is2Times;
            mMandelbrotBitmap = mandelbrot;
            panel1.Invalidate();
            if (!is2Times)
                mMandelbrotGenerator.StartGenerateMandelbrot(true);
        }

        private void UpdateMandelbrot()
        {
            propertyGrid1.SelectedObject = mMandelbrotGenerator;
            mWaitingForBrot = true;
            mMandelbrotGenerator.StartGenerateMandelbrot();
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
                e.Graphics.DrawRectangle(Pens.White, mMouseDownPoint.X, mMouseDownPoint.Y, mCurMousePoint.X - mMouseDownPoint.X, mCurMousePoint.Y - mMouseDownPoint.Y);
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
            mCurMousePoint = e.Location;
        }

        private void AnimateZoom(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2 || y1 == y2)
                return;
            mMandelbrotGenerator.Crop(x1, y1, x2, y2);
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


        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mIsMouseDown = false;
                AnimateZoom(mMouseDownPoint.X, mMouseDownPoint.Y, e.X, e.Y);
            }
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
                    while (s.ElapsedMilliseconds < 60) ;
                }
            }
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdateMandelbrot();
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (mIsMouseDown)
            {
                mCurMousePoint = e.Location;
                panel1.Invalidate();
            }
        }

        private void exportToolStripButton_Click(object sender, EventArgs e)
        {
            new MandelExporterForm(mMandelbrotGenerator).ShowDialog();
        }

        private void propertyGrid1_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            if (mMandelbrotPaletteChanged)
            {
                mMandelbrotPaletteChanged = false;
                UpdateMandelbrot();
            }
        }

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

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to result the presets to the defaults?", "Reset", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                mPresets = MandelbrotPresets.GenerateDefault();
                SavePresets();
                UpdatePresets();
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox("Enter a name for the new preset:", "New Preset", null);
            if (name == null || name.Trim().Length == 0) return;
            name = name.Trim();
            foreach (var p in mPresets.presets)
            {
                if (p.name.Trim().ToLower() == name.ToLower())
                {
                    MessageBox.Show("This name is already in use!");
                    return;
                }
            }
            mPresets.presets.Add(new MandelbrotPresets.MandelbrotPreset(mMandelbrotGenerator, name));
            SavePresets();
            UpdatePresets();
        }

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
    }
}
