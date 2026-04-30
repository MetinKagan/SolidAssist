using System;

namespace SolidAssist
{
    public static class KeywayStandard
    {
        public class Dimensions
        {
            public double WidthMm;   // b
            public double DepthMm;   // t1 (depth machined into the shaft)
            public double LengthMm;  // l (overall slot length, including rounded ends)
        }

        // DIN 6885 Form A: width b and shaft groove depth t1 by diameter range (mm)
        // (lower exclusive, upper inclusive] except first row
        private static readonly (double dMin, double dMax, double b, double t1)[] Table = new (double, double, double, double)[]
        {
            (6, 8, 2, 1.2),
            (8, 10, 3, 1.8),
            (10, 12, 4, 2.5),
            (12, 17, 5, 3.0),
            (17, 22, 6, 3.5),
            (22, 30, 8, 4.0),
            (30, 38, 10, 5.0),
            (38, 44, 12, 5.0),
            (44, 50, 14, 5.5),
            (50, 58, 16, 6.0),
            (58, 65, 18, 7.0),
            (65, 75, 20, 7.5),
            (75, 85, 22, 9.0),
            (85, 95, 25, 9.0),
            (95, 110, 28, 10.0),
            (110, 130, 32, 11.0),
        };

        // DIN 6885 standard parallel key lengths
        private static readonly double[] StandardLengths = new double[]
        {
            6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32, 36, 40, 45, 50,
            56, 63, 70, 80, 90, 100, 110, 125, 140, 160, 180, 200, 220, 250, 280, 320, 360, 400
        };

        public static Dimensions GetForShaft(double diameterMm, double shaftLengthMm)
        {
            foreach (var row in Table)
            {
                if (diameterMm > row.dMin && diameterMm <= row.dMax)
                {
                    double targetLen = 1.5 * diameterMm;
                    double maxFit = shaftLengthMm - 2.0; // leave a bit of room
                    double picked = NearestStandard(targetLen, maxFit);
                    return new Dimensions { WidthMm = row.b, DepthMm = row.t1, LengthMm = picked };
                }
            }
            throw new ArgumentException(
                $"Çap {diameterMm} mm DIN 6885 tablosunun dışında (6–130 mm).");
        }

        private static double NearestStandard(double target, double maxAllowed)
        {
            double best = StandardLengths[0];
            double bestDiff = double.MaxValue;
            foreach (var l in StandardLengths)
            {
                if (l > maxAllowed) break;
                double diff = Math.Abs(l - target);
                if (diff < bestDiff) { bestDiff = diff; best = l; }
            }
            return best;
        }
    }
}
