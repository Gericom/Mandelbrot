using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotTest
{
    //Generic class for animating. It supports all types that support the default math operators
    public class Animator<T>
    {
        public T StartValue { get; private set; }
        public T EndValue { get; private set; }
        public int NrFrames { get; private set; }

        public int CurFrame { get; private set; } = 0;

        private T mLatestValue;

        public Animator(T start, T end, int nrFrames)
        {
            StartValue = start;
            EndValue = end;
            NrFrames = nrFrames;
            mLatestValue = GetValueForFrame(0);
        }

        public T GetLatestValue()
        {
            return mLatestValue;
        }

        public T AdvanceFrame()
        {
            T val = GetValueForFrame(CurFrame);
            mLatestValue = val;
            CurFrame++;
            if(CurFrame > NrFrames - 1)
                CurFrame = NrFrames - 1;
            return val;
        }

        public virtual T GetValueForFrame(int frame)
        {
            if (frame > NrFrames - 1)
                frame = NrFrames - 1;
            else if (frame < 0)
                frame = 0;
            return StartValue + ((dynamic)EndValue - StartValue) * frame / (NrFrames - 1);
        }

        public bool IsFinished
        {
            get { return CurFrame == NrFrames - 1; }
        }
    }
}
