using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SWEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            SldWorks swApp = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
            if (swApp == null) { Console.WriteLine("SolidWorks not found!"); return; }
            Console.WriteLine("Connected: " + swApp.RevisionNumber());

            ModelDoc2 swDoc = (ModelDoc2)swApp.NewPart();
            FeatureManager swFeatMgr = swDoc.FeatureManager;
            SketchManager swSketchMgr = swDoc.SketchManager;

            // --- FEATURE 1: Base cylinder ---
            swDoc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            swSketchMgr.InsertSketch(true);
            swSketchMgr.CreateCircleByRadius(0, 0, 0, 0.025);
            swSketchMgr.InsertSketch(true);

            Feature baseFeat = swFeatMgr.FeatureExtrusion3(
                true, false, false,
                (int)swEndConditions_e.swEndCondBlind,
                (int)swEndConditions_e.swEndCondBlind,
                1, 0,
                false, false, false, false,
                0, 0,
                false, false, false, false,
                true, true, true,
                (int)swStartConditions_e.swStartSketchPlane,
                0, false
            );
            Console.WriteLine(baseFeat != null ? "Base extrusion OK" : "Base extrusion FAILED");

            // --- FEATURE 2: Cut hole through center ---
            swDoc.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
            swSketchMgr.InsertSketch(true);
            swSketchMgr.CreateCircleByRadius(0, 0, 0, 0.010);
            swSketchMgr.InsertSketch(true);

            Feature cutFeat = swFeatMgr.FeatureCut4(
                true, false, false,
                (int)swEndConditions_e.swEndCondThroughAll,
                (int)swEndConditions_e.swEndCondThroughAll,
                0, 0,
                false, false, false, false,
                0, 0,
                false, false, false, false,
                true, true, true, true,
                false, false,
                (int)swStartConditions_e.swStartSketchPlane,
                0, false, false
            );
            Console.WriteLine(cutFeat != null ? "Cut OK — hole created!" : "Cut FAILED");

            Console.ReadKey();
        }
    }
}