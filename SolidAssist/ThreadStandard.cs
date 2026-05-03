using System.Collections.Generic;

namespace SolidAssist
{
    // Metric coarse thread (DIN 13) tap drill + standard blind tap depths.
    // TapDrillMm = pre-drill diameter, MinShaftMm = min shaft diameter for safe tap.
    // DepthMm = standard blind tap depth (~2× nominal for steel).
    public static class ThreadStandard
    {
        public class Spec
        {
            public string Name;
            public double NominalMm;
            public double TapDrillMm;
            public double DepthMm;
            public double MinShaftMm;
        }

        public static readonly List<Spec> Specs = new List<Spec>
        {
            new Spec { Name = "M3",  NominalMm = 3,  TapDrillMm = 2.5,  DepthMm = 6,  MinShaftMm = 6  },
            new Spec { Name = "M4",  NominalMm = 4,  TapDrillMm = 3.3,  DepthMm = 8,  MinShaftMm = 8  },
            new Spec { Name = "M5",  NominalMm = 5,  TapDrillMm = 4.2,  DepthMm = 10, MinShaftMm = 10 },
            new Spec { Name = "M6",  NominalMm = 6,  TapDrillMm = 5.0,  DepthMm = 12, MinShaftMm = 12 },
            new Spec { Name = "M8",  NominalMm = 8,  TapDrillMm = 6.8,  DepthMm = 16, MinShaftMm = 16 },
            new Spec { Name = "M10", NominalMm = 10, TapDrillMm = 8.5,  DepthMm = 20, MinShaftMm = 20 },
            new Spec { Name = "M12", NominalMm = 12, TapDrillMm = 10.2, DepthMm = 24, MinShaftMm = 24 },
            new Spec { Name = "M16", NominalMm = 16, TapDrillMm = 14.0, DepthMm = 32, MinShaftMm = 32 },
            new Spec { Name = "M20", NominalMm = 20, TapDrillMm = 17.5, DepthMm = 40, MinShaftMm = 40 },
        };

        public static Spec PickForShaft(double shaftDiameterMm)
        {
            Spec last = null;
            foreach (var s in Specs)
                if (s.MinShaftMm <= shaftDiameterMm) last = s;
            return last;
        }
    }
}
