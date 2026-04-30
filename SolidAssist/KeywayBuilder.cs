using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidAssist
{
    public static class KeywayBuilder
    {
        // Adds a Form A keyway (slot with rounded ends) to the currently active SolidWorks part.
        // All dimensions in millimeters. Edge distance is measured from the Z=0 end of the shaft
        // to the leftmost (start) point of the keyway.
        public static void AddKeyway(
            double shaftDiameterMm,
            double shaftLengthMm,
            double edgeDistanceMm,
            KeywayStandard.Dimensions key)
        {
            SldWorks swApp = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
            if (swApp == null) throw new InvalidOperationException("SolidWorks açık değil.");

            ModelDoc2 swDoc = (ModelDoc2)swApp.ActiveDoc;
            if (swDoc == null) throw new InvalidOperationException("Aktif SolidWorks belgesi yok. Önce mili oluşturun.");

            FeatureManager swFeatMgr = swDoc.FeatureManager;
            SketchManager swSketchMgr = swDoc.SketchManager;

            // Convert to meters (SolidWorks API unit)
            double radiusM = (shaftDiameterMm / 2.0) / 1000.0;
            double depthM = key.DepthMm / 1000.0;
            double widthM = key.WidthMm / 1000.0;
            double lengthM = key.LengthMm / 1000.0;
            double edgeM = edgeDistanceMm / 1000.0;
            double halfW = widthM / 2.0;

            // Validate fit
            if (edgeDistanceMm + key.LengthMm > shaftLengthMm)
                throw new InvalidOperationException(
                    $"Kanal mile sığmıyor: kenar mesafesi ({edgeDistanceMm}) + kama uzunluğu ({key.LengthMm}) > şaft uzunluğu ({shaftLengthMm}).");

            // 1) Create reference plane parallel to Top Plane, offset by +radius (tangent to top of cylinder)
            string topPlane = ShaftBuilder.GetPlaneName(swDoc, 1); // 1 = Top
            if (topPlane == null) throw new InvalidOperationException("Üst düzlem bulunamadı.");

            swDoc.ClearSelection2(true);
            swDoc.Extension.SelectByID2(topPlane, "PLANE", 0, 0, 0, false, 0, null, 0);

            Feature refPlane = swFeatMgr.InsertRefPlane(
                (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance,
                radiusM,
                0, 0, 0, 0);
            if (refPlane == null) throw new InvalidOperationException("Referans düzlem oluşturulamadı.");

            // 2) Sketch the slot on this plane.
            // On a plane parallel to Top Plane: sketch X = world X, sketch Y = world Z (shaft axis).
            // Slot extends from y = edgeM to y = edgeM + lengthM, centered on x = 0.
            swDoc.ClearSelection2(true);
            refPlane.Select2(false, 0);
            swSketchMgr.InsertSketch(true);

            // On offset Top Plane: sketch +Y maps to world -Z, so negate to place slot at the
            // Z=0 end of the shaft (which extrudes into negative Z by default).
            double y1 = -(edgeM + halfW);
            double y2 = -(edgeM + lengthM - halfW);

            // Two parallel lines along the slot length
            swSketchMgr.CreateLine(-halfW, y1, 0, -halfW, y2, 0);
            swSketchMgr.CreateLine( halfW, y1, 0,  halfW, y2, 0);
            // Two semicircles capping the ends, bulging outward to close the contour.
            // y1 is the less-negative (near Z=0) end, so its cap bulges toward +Y.
            // y2 is the more-negative (far) end, so its cap bulges toward -Y.
            swSketchMgr.CreateArc(0, y1, 0, -halfW, y1, 0,  halfW, y1, 0, -1);
            swSketchMgr.CreateArc(0, y2, 0,  halfW, y2, 0, -halfW, y2, 0, -1);

            string sketchName = ShaftBuilder.GetLastSketchName(swDoc);
            swSketchMgr.InsertSketch(true);

            // 3) Cut-extrude blind with depth = keyway depth
            swDoc.ClearSelection2(true);
            swDoc.Extension.SelectByID2(sketchName, "SKETCH", 0, 0, 0, false, 0, null, 0);

            Feature cut = swFeatMgr.FeatureCut4(
                true, false, false,
                (int)swEndConditions_e.swEndCondBlind,
                (int)swEndConditions_e.swEndCondBlind,
                -depthM, 0,
                false, false, false, false,
                0, 0,
                false, false, false, false,
                false, false, false, false,
                false, false,
                (int)swStartConditions_e.swStartSketchPlane,
                0, false, false);

            if (cut == null) throw new InvalidOperationException("Kama kanalı kesimi başarısız.");

            swDoc.ClearSelection2(true);
            swDoc.ViewZoomtofit2();
        }
    }
}
