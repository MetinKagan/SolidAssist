using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidAssist
{
    public static class ShaftBuilder
    {
        // Creates a cylindrical shaft in a new SolidWorks part.
        // diameterMm and lengthMm are in millimeters.
        public static void Create(double diameterMm, double lengthMm)
        {
            SldWorks swApp = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
            if (swApp == null) throw new InvalidOperationException("SolidWorks açık değil.");

            ModelDoc2 swDoc = (ModelDoc2)swApp.NewPart();
            if (swDoc == null) throw new InvalidOperationException("Yeni parça oluşturulamadı.");

            FeatureManager swFeatMgr = swDoc.FeatureManager;
            SketchManager swSketchMgr = swDoc.SketchManager;

            string frontPlane = GetPlaneName(swDoc, 0);
            if (frontPlane == null) throw new InvalidOperationException("Ön düzlem bulunamadı.");

            // SolidWorks API uses meters
            double radiusM = (diameterMm / 2.0) / 1000.0;
            double lengthM = lengthMm / 1000.0;

            swDoc.Extension.SelectByID2(frontPlane, "PLANE", 0, 0, 0, false, 0, null, 0);
            swSketchMgr.InsertSketch(true);
            swSketchMgr.CreateCircleByRadius(0, 0, 0, radiusM);
            swSketchMgr.InsertSketch(true);

            Feature shaft = swFeatMgr.FeatureExtrusion3(
                true, false, false,
                (int)swEndConditions_e.swEndCondBlind,
                (int)swEndConditions_e.swEndCondBlind,
                lengthM, 0,
                false, false, false, false,
                0, 0,
                false, false, false, false,
                true, true, true,
                (int)swStartConditions_e.swStartSketchPlane,
                0, false
            );

            if (shaft == null) throw new InvalidOperationException("Mil ekstrüzyonu başarısız.");

            swDoc.ClearSelection2(true);
            swDoc.ViewZoomtofit2();
        }

        // 0=Front, 1=Top, 2=Right (default plane order, language-independent)
        public static string GetPlaneName(IModelDoc2 doc, int index)
        {
            object[] feats = (object[])doc.FeatureManager.GetFeatures(true);
            int found = 0;
            foreach (object f in feats)
            {
                Feature ft = (Feature)f;
                if (ft.GetTypeName2() == "RefPlane")
                {
                    if (found == index) return ft.Name;
                    found++;
                }
            }
            return null;
        }

        // Returns the most recently created sketch's name (any language)
        public static string GetLastSketchName(IModelDoc2 doc)
        {
            object[] feats = (object[])doc.FeatureManager.GetFeatures(true);
            string last = null;
            foreach (object f in feats)
            {
                Feature ft = (Feature)f;
                if (ft.GetTypeName2() == "ProfileFeature") last = ft.Name;
            }
            return last;
        }
    }
}
