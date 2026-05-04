using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidAssist
{
    public static class ThreadBuilder
    {
        // Creates a tapped hole on one end of the shaft via the SOLIDWORKS Hole Wizard.
        // endIsStart: true → Z=0 face (origin end), false → far end.
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

            double depthM = spec.DepthMm / 1000.0;
            double nominalM = spec.NominalMm / 1000.0;
            double tapDrillM = spec.TapDrillMm / 1000.0;

            // 1) Find an end face by walking the body — robust against unknown extrude direction.
            Face2 endFace = FindEndFace(swDoc, endIsStart);
            if (endFace == null) throw new InvalidOperationException(
                endIsStart ? "Başlangıç ucu yüzeyi bulunamadı." : "Bitiş ucu yüzeyi bulunamadı.");

            swDoc.ClearSelection2(true);
            bool faceSel = ((Entity)endFace).Select4(false, null);
            if (!faceSel) throw new InvalidOperationException(
                endIsStart ? "Başlangıç ucu yüzeyi seçilemedi." : "Bitiş ucu yüzeyi seçilemedi.");

            // 2) Hole Wizard — face selected; SW positions hole at face geometric center.
            string lastErr = null;
            Feature hole = TryHoleWizard(swFeatMgr, spec, nominalM, tapDrillM, depthM, false, ref lastErr);
            if (hole == null)
            {
                swDoc.ClearSelection2(true);
                ((Entity)endFace).Select4(false, null);
                hole = TryHoleWizard(swFeatMgr, spec, nominalM, tapDrillM, depthM, true, ref lastErr);
            }
            if (hole == null) throw new InvalidOperationException(
                $"Delik sihirbazı başarısız ({spec.Name}, {(endIsStart ? "başlangıç" : "bitiş")}). Son hata: {lastErr ?? "(yok)"}");

            // Reset overridden custom-size + thread-depth values to standard database defaults.
            // ChangeStandard re-pulls all sizing from the SW Hole Wizard DB; RestoreDefaultValues
            // then clears any leftover custom-override flags ("Özel boyutlandırmayı göster").
            try
            {
                WizardHoleFeatureData2 data = (WizardHoleFeatureData2)hole.GetDefinition();
                if (data != null && data.AccessSelections(swDoc, null))
                {
                    int curStd = data.Standard2;
                    int curFt = data.FastenerType2;
                    string curSize = data.FastenerSize;
                    data.ChangeStandard(curStd, curFt, curSize);
                    data.RestoreDefaultValues();
                    data.DrillAngle = 118.0 * Math.PI / 180.0;
                    hole.ModifyDefinition(data, swDoc, null);
                }
            }
            catch
            {
                // varsayılana çekme başarısızsa feature yine de oluştu, devam.
            }

            swDoc.ClearSelection2(true);
            swDoc.ViewZoomtofit2();
        }

        private static Feature TryHoleWizard(
            FeatureManager mgr,
            ThreadStandard.Spec spec,
            double nominalM,
            double tapDrillM,
            double depthM,
            bool revDir,
            ref string lastErr)
        {
            // (StandardIndex, FastenerTypeIndex) pairs — must match the SW Hole Wizard database.
            int[][] stdAndType = new int[][]
            {
                new int[] { (int)swWzdHoleStandards_e.swStandardISO,         (int)swWzdHoleStandardFastenerTypes_e.swStandardISOTappedHole },
                new int[] { (int)swWzdHoleStandards_e.swStandardISO,         (int)swWzdHoleStandardFastenerTypes_e.swStandardISOTappedHoleBottoming },
                new int[] { (int)swWzdHoleStandards_e.swStandardAnsiMetric,  (int)swWzdHoleStandardFastenerTypes_e.swStandardAnsiMetricTappedHole },
                new int[] { (int)swWzdHoleStandards_e.swStandardAnsiMetric,  (int)swWzdHoleStandardFastenerTypes_e.swStandardAnsiMetricBottomingTappedHole },
                new int[] { (int)swWzdHoleStandards_e.swStandardDIN,         (int)swWzdHoleStandardFastenerTypes_e.swStandardDINTappedHole },
                new int[] { (int)swWzdHoleStandards_e.swStandardDIN,         (int)swWzdHoleStandardFastenerTypes_e.swStandardDINTappedHoleBottoming },
            };
            string[] sizes = new string[]
            {
                spec.Name,
                spec.Name + "x" + DefaultPitch(spec.Name),
            };

            short endBlind = (short)swEndConditions_e.swEndCondBlind;
            int holeType = (int)swWzdGeneralHoleTypes_e.swWzdTap;
            double threadDepth = depthM * 0.85;

            foreach (int[] pair in stdAndType)
            {
                int std = pair[0];
                int ft = pair[1];
                {
                    foreach (string size in sizes)
                    {
                        try
                        {
                            Feature f = mgr.HoleWizard5(
                                holeType,
                                std,
                                ft,
                                size,
                                endBlind,
                                nominalM,                        // Diameter
                                depthM,                          // Depth
                                threadDepth,                     // Length
                                118.0 * Math.PI / 180.0,         // Value1: drill point angle
                                tapDrillM,                       // Value2: tap drill diameter
                                0, 0, 0, 0, 0, 0, 0, 0, 0, 0,    // Value3..12 unused
                                "",                              // ThreadClass empty
                                revDir,
                                true,                            // FeatureScope
                                true,                            // AutoSelect
                                false,                           // AssemblyFeatureScope
                                false,                           // AutoSelectComponents
                                false);                          // PropagateFeatureToParts
                            if (f != null) return f;
                            lastErr = $"std={std} ft={ft} size={size}: null döndü";
                        }
                        catch (Exception ex)
                        {
                            lastErr = $"std={std} ft={ft} size={size}: {ex.Message}";
                        }
                    }
                }
            }
            return null;
        }

        // Walks the active part body, finds planar faces with normal along Z, and picks
        // the one closest to (start) or farthest from (end) the world origin.
        private static Face2 FindEndFace(ModelDoc2 doc, bool startEnd)
        {
            PartDoc part = doc as PartDoc;
            if (part == null) return null;
            object[] bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true);
            if (bodies == null || bodies.Length == 0) return null;

            Face2 bestFace = null;
            double bestKey = startEnd ? double.MaxValue : -1.0;

            foreach (object bo in bodies)
            {
                Body2 body = (Body2)bo;
                object[] faces = (object[])body.GetFaces();
                if (faces == null) continue;
                foreach (object fo in faces)
                {
                    Face2 f = (Face2)fo;
                    Surface s = (Surface)f.GetSurface();
                    if (s == null || !s.IsPlane()) continue;

                    double[] n = (double[])f.Normal;
                    if (n == null || n.Length < 3) continue;
                    if (Math.Abs(n[2]) < 0.95) continue; // not Z-aligned

                    double[] box = (double[])f.GetBox();
                    if (box == null || box.Length < 6) continue;
                    double cz = (box[2] + box[5]) / 2.0;
                    double absZ = Math.Abs(cz);

                    if (startEnd)
                    {
                        if (absZ < bestKey) { bestKey = absZ; bestFace = f; }
                    }
                    else
                    {
                        if (absZ > bestKey) { bestKey = absZ; bestFace = f; }
                    }
                }
            }
            return bestFace;
        }

        // ISO metric coarse pitches (DIN 13) for fallback "Mxxxxxx" size strings.
        private static string DefaultPitch(string name)
        {
            switch (name)
            {
                case "M3":  return "0.5";
                case "M4":  return "0.7";
                case "M5":  return "0.8";
                case "M6":  return "1.0";
                case "M8":  return "1.25";
                case "M10": return "1.5";
                case "M12": return "1.75";
                case "M16": return "2.0";
                case "M20": return "2.5";
                default:    return "1.0";
            }
        }
    }
}
