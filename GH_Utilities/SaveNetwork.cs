using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using Newtonsoft.Json;

namespace FDMremote.GH_Utilities
{
    public class SaveNetwork : GH_Component
    {
        public List<Curve> curves = new List<Curve>();
        public List<Point3d> anchors = new List<Point3d>();
        public List<Vector3d> loads;
        public List<double> q = new List<double>();
        double tol;
        public string folder;
        public string file;
        public string data;
        Network network;
        Network outnetwork;
        Vector3d baseoffset;
        Vector3d offsetplus;

        /// <summary>
        /// Initializes a new instance of the SaveNetwork class.
        /// </summary>
        public SaveNetwork()
          : base("Freeze Network", "Freeze",
              "Save/freeze a network",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Save Network", "Freeze", "Click to freeze current state", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Network to save", "Network", "Network to save; used for output network of FDMlisten", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load(s)", "P", "Applied load to network", GH_ParamAccess.list, new Vector3d(0,0,0));
            pManager.AddTextParameter("Folder Directory", "Dir", @"Written like: C:\\Users\\folder\\", GH_ParamAccess.item, "C:\\Temp\\");
            pManager.AddTextParameter("File Name", "Name", "Name of file, ending in .json", GH_ParamAccess.item, "network.json");
            pManager.AddVectorParameter("SaveOffset", "Offset", "Offset of saved geometry", GH_ParamAccess.item, new Vector3d(0, 0, 0));
            

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Edges", "E", "Edges", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchors", "A", "Anchors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Force Densities", "q", "Force Densities", GH_ParamAccess.list);
            pManager.AddGenericParameter("Network", "Network", "Network", GH_ParamAccess.item);

            pManager.HideParameter(0);
            pManager.HideParameter(1);
            pManager[3].Optional = true ;
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool freeze = false;
            DA.GetData(0, ref freeze);
            offsetplus = new Vector3d();

            if (freeze)
            {
                file = "network.json";
                folder = "";
                loads = new List<Vector3d>();

                DA.GetData(1, ref network);
                DA.GetDataList(2, loads);
                DA.GetData(4, ref file);
                DA.GetData(5, ref offsetplus);

                GetOffset();
                GetGeo();
                //curves = network.Curves;
                //anchors = network.Anchors;
                q = new List<double>(network.ForceDensities);
                tol = network.Tolerance;
                
                outnetwork = new Network(anchors, curves, q, tol);

                try
                {
                    DA.GetData(3, ref folder);
                    var fn = Path.Combine(folder, file);

                    Matrix<double> P = Solver.PMaker(loads, network.N);
                    InformationObject obj = new InformationObject(network, P, 0);
                    data = JsonConvert.SerializeObject(obj);


                    System.IO.File.WriteAllText(fn, data);
                }
                catch { }

                
            }
            
            DA.SetDataList(0, curves);
            DA.SetDataList(1, anchors);
            DA.SetDataList(2, q);
            DA.SetData(3, outnetwork);

        }

        private void GetGeo()
        {
            curves = new List<Curve>();
            anchors = new List<Point3d>();

            foreach (Curve curve in network.Curves)
            {
                Curve newcurve = (Curve)curve.Duplicate();
                newcurve.Translate(baseoffset + offsetplus);

                curves.Add(newcurve);
            }

            foreach (Point3d point in network.Anchors)
            {
                Point3d newpoint = new Point3d(point) + baseoffset + offsetplus;
                anchors.Add(newpoint);
            }
        }

        /// <summary>
        /// get bounding box
        /// </summary>
        private void GetOffset()
        {
            var bb = new BoundingBox(network.Points);

            //get total height of geometry

            var pbl = bb.Corner(true, true, true);
            var pbr = bb.Corner(false, true, true);
            var ptl = bb.Corner(true, false, true);
            var pzl = bb.Corner(true, true, false);

            baseoffset = (pbr - pbl) * 1.2;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.export;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6F484B26-74BC-4347-88F2-2DFE2B863A50"); }
        }
    }
}