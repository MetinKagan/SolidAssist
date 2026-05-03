using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidAssist
{
    public static class ThreadBuilder
    {
        // Drills a blind tap-drill hole on one end of the shaft.
        // endIsStart: true → Z=0 face (origin end), false → Z=-shaftLength face (far end).
        public static void AddTappedHole(
            double shaftDiameterMm,
            double shaftLengthMm,
            bool endIsStart,
            ThreadStandard.Spec spec)
        {
            if (spec == null) throw new InvalidOperationException("Kılavuz standardı seçilmedi.");
            if (spec.TapDrillMm >= shaftDiameterMm)
                throw new InvalidOperationException(
                    $"{spec.Name} için ön delik çapı ({spec.TapDrillMm}mm) şaft çapından ({shaftDiameterMm}mm) küçük olmalı.");
            if (spec.DepthMm >= shaftLengthMm)
                throw new InvalidOperationException(
                    $"{spec.Name} derinliği ({spec.DepthMm}mm) şaft boyundan ({shaftLengthMm}mm) küçük olmalı.");

            SldWorks swApp = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
            if (swApp == null) throw new InvalidOperationException("SolidWorks açık değil.");

            ModelDoc2 swDoc = (ModelDoc2)swApp.ActiveDoc;
            if (swDoc == null) throw new InvalidOperationException("Aktif SolidWorks belgesi yok. Önce mili oluşturun.");

            FeatureManager swFeatMgr = swDoc.FeatureManager;
            SketchManager swSketchMgr = swDoc.SketchManager;

            double shaftLengthM = shaftLengthMm / 1000.0;
            double drillRadiusM = (spec.TapDrillMm / 2.0) / 1000.0;
            double depthM = spec.DepthMm / 1000.0;
            double shaftRadiusM = (shaftDiameterMm / 2.0) / 1000.0;
            // Slight off-axis offset to avoid origin/edge ambiguity in SelectByID2
            double sx = shaftRadiusM * 0.5;

            // 1) Select an end face. Try both Z signs since extrude direction depends on SW config.
            swDoc.ClearSelection2(true);
            bool faceSel = false;
            if (endIsStart)
            {
                faceSel = swDoc.Extension.SelectByID2("", "FACE", sx, 0, 0, false, 0, null, 0);
            }
            else
            {
                faceSel = swDoc.Extension.SelectByID2("", "FACE", sx, 0, -shaftLengthM, false, 0, null, 0);
                if (!faceSel)
                    faceSel = swDoc.Extension.SelectByID2("", "FACE", sx, 0,  shaftLengthM, false, 0, null, 0);
            }
            if (!faceSel) throw new InvalidOperationException(
                endIsStart ? "Başlangıç ucu yüzeyi seçilemedi." : "Bitiş ucu yüzeyi seçilemedi.");

            // 2) Open sketch on selected face
            swSketchMgr.InsertSketch(true);
            Sketch activeSketch = (Sketch)swSketchMgr.ActiveSketch;
            if (activeSketch == null) throw new InvalidOperationException("Çizim açılamadı.");

            // 3) Draw circle centered on face center (sketch origin)
            swSketchMgr.CreateCircleByRadius(0, 0, 0, drillRadiusM);

            swSketchMgr.InsertSketch(true);

            // 4) Re-select sketch then cut blind into the shaft
            swDoc.ClearSelection2(true);
            ((Feature)activeSketch).Select2(false, 0);

            // Cut depth is along face normal, into body. Try Dir=false first; fallback to Dir=true.
            Feature cut = TryCut(swFeatMgr, depthM, false);
            if (cut == null)
            {
                swDoc.ClearSelection2(true);
                ((Feature)activeSketch).Select2(false, 0);
                cut = TryCut(swFeatMgr, depthM, true);
            }
            if (cut == null) throw new InvalidOperationException(
                $"Kılavuz deliği açılamadı ({spec.Name}, {(endIsStart ? "başlangıç" : "bitiş")}).");

            swDoc.ClearSelection2(true);
            swDoc.ViewZoomtofit2();
        }

        private static Feature TryCut(FeatureManager mgr, double depthM, bool reverseDir)
        {
            try
            {
                return mgr.FeatureCut4(
                    true, false, reverseDir,
                    (int)swEndConditions_e.swEndCondBlind,
                    (int)swEndConditions_e.swEndCondBlind,
                    depthM, 0,
                    false, false, false, false,
                    0, 0,
                    false, false, false, false,
                    false, true, true, false, false, false,
                    (int)swStartConditions_e.swStartSketchPlane,
                    0, false, false);
            }
            catch
            {
                return null;
            }
        }
    }
}
