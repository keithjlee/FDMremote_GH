using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace FDMremote.GH_Utilities
{
    public class LoadNetwork : GH_Component
    {
        List<Curve> curves;
        List<Point3d> anchors;
        List<Point3d> points;
        List<double> q;
        List<Vector3d> loads;
        /// <summary>
        /// Initializes a new instance of the LoadNetwork class.
        /// </summary>
        public LoadNetwork()
          : base("LoadNetwork", "Load",
              "Load an FDM network .json file",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Folder", "Dir", @"Written like: C:\\Users\\folder\\", GH_ParamAccess.item);
            pManager.AddTextParameter("File Name", "Name", "File name, must end in .json", GH_ParamAccess.item) ;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Curves", "Curves of network", GH_ParamAccess.list) ; 
            pManager.AddPointParameter("Anchors", "Anchors", "Anchor points",
                GH_ParamAccess.list) ;
            pManager.AddNumberParameter("Force Densities", "q", "Force densities",
                GH_ParamAccess.list) ;
            pManager.AddVectorParameter("Loads", "P", "Load vectors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string folder = "";
            string file = "";

            if (!DA.GetData(0, ref folder)) return;
            if (!DA.GetData(1, ref file)) return;

            string fn = Path.Combine(folder, file);

            string data = System.IO.File.ReadAllText(fn);

            var info = JsonConvert.DeserializeObject<InformationObject>(data);

            GetPoints(info);
            GetCurves(info);
            q = info.Q;
            GetLoads(info);

            DA.SetDataList(0, curves);
            DA.SetDataList(1, anchors);
            DA.SetDataList(2, q);
            DA.SetDataList(3, loads);


        }

        private void GetPoints(InformationObject data)
        {
            points = new List<Point3d>();
            anchors = new List<Point3d>();

            for (int i = 0; i < data.X.Count; i++)
            {
                points.Add(new Point3d(data.X[i], data.Y[i], data.Z[i]));
            }

            for (int i = 0; i < data.Fjulia.Count; i++)
            {
                var index = data.Fjulia[i];
                anchors.Add(points[index]);
            }
        }

        private void GetLoads(InformationObject data)
        {
            loads = new List<Vector3d>();
            for (int i = 0; i < data.Px.Count(); i++)
            {
                loads.Add(new Vector3d(data.Px[i], data.Py[i], data.Pz[i]));
            }
        }

        private void GetCurves(InformationObject data)
        {
            curves = new List<Curve>();

            for (int i = 0; i < data.Ijulia.Count; i += 2)
            {
                var istart = data.Jjulia[i];
                var iend = data.Jjulia[i+1];

                Line edge = new Line(points[istart], points[iend]);

                curves.Add(edge.ToNurbsCurve());
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.import;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C249BB80-12DA-4595-B0FA-C2D79FF8A779"); }
        }
    }
}