using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MandelbrotTest
{
    class CoordinateUtilities
    {
        public static void FixRect(ref double x1, ref double y1, ref double x2, ref double y2)
        {

            if ( x1 > x2 )
            {
                double tmp = x2;
                x2 = x1;
                x1 = tmp;
            }
            if ( y1 > y2 )
            {
                double tmp = y2;
                y2 = y1;
                y1 = tmp;
            }
        }

        public static void FixRect(ref int x1, ref int y1, ref int x2, ref int y2)
        {

            if ( x1 > x2 )
            {
                int tmp = x2;
                x2 = x1;
                x1 = tmp;
            }
            if ( y1 > y2 )
            {
                int tmp = y2;
                y2 = y1;
                y1 = tmp;
            }
        }

    }
}
