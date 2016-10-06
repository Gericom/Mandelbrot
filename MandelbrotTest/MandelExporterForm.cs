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

namespace Mandelbrot
{
    public partial class MandelExporterForm : Form
    {
        MandelbrotGenerator mMandelbrotGeneratorTemplate;

        public MandelExporterForm(MandelbrotGenerator generatorTemplate)
        {
            mMandelbrotGeneratorTemplate = generatorTemplate;
            InitializeComponent();
        }

        private void exportButton_Click(object sender, EventArgs e)
        {
            //Generate it 4 times as big, to scale it down later and have nice anti-aliasing
            MandelbrotGenerator mg = new MandelbrotGenerator(mMandelbrotGeneratorTemplate, (int)widthUpDown.Value * 4, (int)heightUpDown.Value * 4);
            mg.FixAspect();
            mg.MandlebrotReady += delegate (Bitmap mandelbrot, bool is2Times)
            {
                mandelbrot = new Bitmap(mandelbrot, (int)widthUpDown.Value, (int)heightUpDown.Value);
                mandelbrot.Save(pathTextBox.Text, ImageFormat.Png);
                //Controls can't be changed from a different thread
                progressBar.Invoke((Action)delegate
                {
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = 100;
                });
                DialogResult = DialogResult.OK;
            };
            progressBar.Enabled = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            cancelButton.Enabled = false;
            exportButton.Enabled = false;
            groupBox1.Enabled = false;
            groupBox2.Enabled = false;
            mg.StartGenerateMandelbrot();
        }

        private void browseButton_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK
                && saveFileDialog1.FileName.Length > 0)
            {
                pathTextBox.Text = saveFileDialog1.FileName;
                exportButton.Enabled = true;
            }
        }
    }
}
