using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;

namespace FDMremote.GH_Design
{
    public class Visualize : GH_Component
    {
        List<Curve> edges;
        List<double> internalforces;
        List<double> forcedensities;
        Line[] externalforces;

        /// <summary>
        /// Initializes a new instance of the Visualize class.
        /// </summary>
        public Visualize()
          : base("Visualize Network", "Visualize",
              "Visualize a FDM network",
              "FDMremote", "Design")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "Network to visualize", GH_ParamAccess.item);
            pManager.AddVectorParameter("Loads", "P", "Applied loads", GH_ParamAccess.list, new Vector3d(0,0,0));
            pManager.AddNumberParameter("Load Scale", "Pscale", "Scale factor for length of arrows", GH_ParamAccess.item, 1.0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            Network network = new Network();
            List<Vector3d> loads = new List<Vector3d>();
            double scale = 1.0;

            //Assign
            if (!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, loads);
            DA.GetData(2, ref scale);

            externalforces = LoadMaker(network.Points, network.N, loads, scale);
            edges = network.Curves;

        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            args.Display.DrawArrows(externalforces, System.Drawing.Color.Coral);
        }

        public Line[] LoadMaker(List<Point3d> nodes, List<int> N, List<Vector3d> loads, double scale)
        {
            List<Line> loadvectors = new List<Line>();

            if (N.Count != loads.Count && loads.Count != 1) throw new ArgumentException("Length of force vectors must be 1 or match length of free nodes.");

            if (loads.Count == 1)
            {
                for (int i = 0; i < N.Count; i++)
                {
                    int index = N[i];

                    Point3d p = nodes[index];

                    loadvectors.Add(new Line(p, loads[0] * scale));
                }
            }
            else
            {
                for (int i = 0; i < N.Count; i++)
                {
                    int index = N[i];
                    Point3d p = nodes[i];
                    Vector3d l = loads[i];

                    loadvectors.Add(new Line(p, l * scale));
                }
            }

            return loadvectors.ToArray();
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5B9176B3-B940-4C2C-AFFE-BF4532FB2111"); }
        }
    }
}