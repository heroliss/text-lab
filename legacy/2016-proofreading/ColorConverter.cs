using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 改错字
{
    static class ColorConverter
    {
        /// <summary>
        /// HSB转RGB
        /// </summary>
        /// <param name="hue">色调</param>
        /// <param name="saturation">饱和度</param>
        /// <param name="brightness">亮度</param>
        /// <returns>返回：Color</returns>
        public static Color HSBtoRGB(float hue, float saturation, float brightness)
        {
            int r = 0, g = 0, b = 0;
            if (saturation == 0)
            {
                r = g = b = (int)(brightness * 255.0f + 0.5f);
            }
            else
            {
                float h = (hue - (float)Math.Floor(hue)) * 6.0f;
                float f = h - (float)Math.Floor(h);
                float p = brightness * (1.0f - saturation);
                float q = brightness * (1.0f - saturation * f);
                float t = brightness * (1.0f - (saturation * (1.0f - f)));
                switch ((int)h)
                {
                    case 0:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(t * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int)(q * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(q * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int)(t * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }
            return Color.FromArgb(Convert.ToByte(255), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }
    }
    //public static System.Drawing.Color HSB2RGB(double hue, double sat, double bri)
    //{
    //    double r = 0;
    //    double g = 0;
    //    double b = 0;

    //    if (sat == 0)
    //    {
    //        r = g = b = bri;
    //    }
    //    else
    //    {
    //        // the color wheel consists of 6 sectors. Figure out which sector you're in.
    //        double sectorPos = hue / 60.0;
    //        int sectorNumber = (int)(Math.Floor(sectorPos));
    //        // get the fractional part of the sector
    //        double fractionalSector = sectorPos - sectorNumber;

    //        // calculate values for the three axes of the color. 
    //        double p = bri * (1.0 - sat);
    //        double q = bri * (1.0 - (sat * fractionalSector));
    //        double t = bri * (1.0 - (sat * (1 - fractionalSector)));

    //        // assign the fractional colors to r, g, and b based on the sector the angle is in.
    //        switch (sectorNumber)
    //        {
    //            case 0:
    //                r = bri;
    //                g = t;
    //                b = p;
    //                break;
    //            case 1:
    //                r = q;
    //                g = bri;
    //                b = p;
    //                break;
    //            case 2:
    //                r = p;
    //                g = bri;
    //                b = t;
    //                break;
    //            case 3:
    //                r = p;
    //                g = q;
    //                b = bri;
    //                break;
    //            case 4:
    //                r = t;
    //                g = p;
    //                b = bri;
    //                break;
    //            case 5:
    //                r = bri;
    //                g = p;
    //                b = q;
    //                break;
    //        }
    //    }
    //    return System.Drawing.Color.FromArgb
    //    (
    //        Convert.ToInt32(r * 255),
    //        Convert.ToInt32(g * 255),
    //        Convert.ToInt32(b * 255)
    //    );
    //}
}
