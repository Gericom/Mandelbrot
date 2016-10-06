using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MandelbrotTest
{
    [XmlRoot("mandelbrot")]
    public class MandelbrotPresets
    {
        public class MandelbrotPreset
        {
            public MandelbrotPreset()
            {

            }

            public MandelbrotPreset(MandelbrotGenerator template, String name)
            {
                this.name = name;
                x = template.MandelbrotX;
                y = template.MandelbrotY;
                width = template.MandelbrotWidth;
                height = template.MandelbrotHeight;
                maxCount = template.MandlebrotMaxCount;
                style = template.MandelbrotStyle;
                palette = new int[template.MandelbrotPalette.Count];
                for (int i = 0; i < template.MandelbrotPalette.Count; i++)
                    palette[i] = template.MandelbrotPalette[i].ToArgb();
            }

            public string name;
            public double x;
            public double y;
            public double width;
            public double height;
            public int maxCount;
            public MandelbrotGenerator.MandelbrotColorStyle style;
            //Use int instead of Color, because Color isn't saved in the xml for some reason
            [XmlArrayItem("entry")]
            public int[] palette;

            public void ApplyToGenerator(MandelbrotGenerator generator)
            {
                generator.MandelbrotX = x;
                generator.MandelbrotY = y;
                generator.MandelbrotWidth = width;
                generator.MandelbrotHeight = height;
                generator.MandelbrotStyle = style;
                generator.MandlebrotMaxCount = maxCount;
                generator.BeginUpdatePalette();
                generator.MandelbrotPalette.Clear();
                foreach (int c in palette)
                    generator.MandelbrotPalette.Add(Color.FromArgb(c));
                generator.EndUpdatePalette();
                generator.FixAspect();
            }
        }

        [XmlArrayItem("preset")]
        public List<MandelbrotPreset> presets = new List<MandelbrotPreset>();

        public static MandelbrotPresets FromXml(String xml)
        {
            return (MandelbrotPresets)new XmlSerializer(typeof(MandelbrotPresets)).Deserialize(new StringReader(xml));
        }

        //Uses a XmlSerializer to convert the presets to xml in an easy way
        public String ToXml()
        {
            TextWriter tw = new StringWriter();
            //Prevent writing of unnecessary xsi and xsd attributes
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            new XmlSerializer(typeof(MandelbrotPresets)).Serialize(tw, this, ns);
            return tw.ToString();
        }

        public static MandelbrotPresets GenerateDefault()
        {
            MandelbrotPresets result = new MandelbrotPresets();
            result.presets.Add(new MandelbrotPreset()
            {
                name = "Default",
                x = -2,
                y = -2,
                width = 4,
                height = 4,
                maxCount = 512,
                style = MandelbrotGenerator.MandelbrotColorStyle.SimpleRgb,
                palette = new int[0]
            });
            result.presets.Add(new MandelbrotPreset()
            {
                name = "Fire Leaf",
                x = -0.11202303901237315,
                y = -0.92944262954527346,
                width = 0.013835606102545174,
                height = 0.0089072648146142352,
                maxCount = 128,
                style = MandelbrotGenerator.MandelbrotColorStyle.Paletted,
                palette = new int[]
                    {
                        Color.Green.ToArgb(),
                        Color.Red.ToArgb(),
                        Color.Orange.ToArgb(),
                        Color.White.ToArgb(),
                        Color.Black.ToArgb()
                    }
            });
            result.presets.Add(new MandelbrotPreset()
            {
                name = "Neptune's Trident",
                x = -0.561895714249722,
                y = -0.64233713371809176,
                width = 1.185069712557534E-05,
                height = 7.62939453125E-06,
                maxCount = 256,
                style = MandelbrotGenerator.MandelbrotColorStyle.Paletted,
                palette = new int[]
                {
                    -16777152,
                    -64,
                    -8388480,
                    -16727872,
                    -1,
                    -16777216
                }
            });
            result.presets.Add(new MandelbrotPreset()
            {
                name = "Milkyway",
                x = 0.43918663254571694,
                y = -0.251801694741495,
                width = 9.4647755985933241E-05,
                height = 7.2604610487025933E-05,
                maxCount = 128,
                style = MandelbrotGenerator.MandelbrotColorStyle.Paletted,
                palette = new int[]
                {
                    -1,
                    -16777184,
                    -64,
                    -16777216,
                }
            });
            return result;
        }
    }
}
