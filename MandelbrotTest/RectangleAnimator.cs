using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotTest
{
    //Implementation of Animator for the rectangle type, because it doesn't support
    //the math operators
    public class RectangleAnimator : Animator<Rectangle>
    {
        public RectangleAnimator(Rectangle start, Rectangle end, int nrFrames)
        : base(start, end, nrFrames)
        { }

        public override Rectangle GetValueForFrame(int frame)
        {
            if (frame > NrFrames - 1)
                frame = NrFrames - 1;
            else if (frame < 0)
                frame = 0;
            return new Rectangle(
                StartValue.X + (EndValue.X - StartValue.X) * frame / (NrFrames - 1),
                StartValue.Y + (EndValue.Y - StartValue.Y) * frame / (NrFrames - 1),
                StartValue.Width + (EndValue.Width - StartValue.Width) * frame / (NrFrames - 1),
                StartValue.Height + (EndValue.Height - StartValue.Height) * frame / (NrFrames - 1));
        }
    }
}
