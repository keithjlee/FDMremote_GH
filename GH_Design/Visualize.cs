using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;
using Grasshopper.Kernel.Parameters;
using Grasshopper.GUI.Gradient;
using MathNet.Numerics.Integration;
using System.Runtime.InteropServices;
using Grasshopper.Kernel.Types.Transforms;

namespace FDMremote.GH_Design
{
    public class Visualize : GH_Component
    {
        Line[] edges;
        List<double> property;
        Line[] externalforces;
        Line[] reactionforces;
        System.Drawing.Color c0;
        System.Drawing.Color c1;
        GH_Gradient grad;
        int thickness;
        System.Drawing.Color cload;
        System.Drawing.Color creact;
        bool load;
        bool react;

        int prop;
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
            pManager.AddColourParameter("ColourMin", "Cmin", "Colour for minimum value", GH_ParamAccess.item, System.Drawing.Color.FromArgb(230,231,232));
            pManager.AddColourParameter("ColourMax", "Cmax", "Colour for maximum value",
                GH_ParamAccess.item, System.Drawing.Color.FromArgb(62, 168, 222));
            pManager.AddIntegerParameter("Color Property", "Property", "Property displayed by colour gradient", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Line Thickness", "Thickness", "Thickness of preview lines", GH_ParamAccess.item, 2);
            pManager.AddColourParameter("Load Colour", "Cload", "Colour for applied loads", GH_ParamAccess.item, System.Drawing.Color.Coral);
            pManager.AddBooleanParameter("Show Loads", "Load", "Show external loads in preview", GH_ParamAccess.item, true);
            pManager.AddColourParameter("Reaction Colour", "Creaction", "Colour for support reactions", GH_ParamAccess.item, System.Drawing.Color.FromArgb(71, 181, 116));
            pManager.AddBooleanParameter("Show Reactions", "Reaction", "Show anchor reactions in preview", GH_ParamAccess.item, false);

            Param_Integer param = pManager[5] as Param_Integer;
            param.AddNamedValue("Force", 0);
            param.AddNamedValue("Q", 1);

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
            ClearData();
            //Initialize
            Network network = new Network();
            List<Vector3d> loads = new List<Vector3d>();
            double scale = 1.0;
            c0 = System.Drawing.Color.FromArgb(230, 231, 232);
            c1 = System.Drawing.Color.FromArgb(62, 168, 222);
            cload = System.Drawing.Color.Coral;
            load = true;
            creact = System.Drawing.Color.FromArgb(71, 181, 116);
            react = false;

            //Assign
            if (!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, loads);
            DA.GetData(2, ref scale);
            DA.GetData(3, ref c0);
            DA.GetData(4, ref c1);
            DA.GetData(5, ref prop);
            DA.GetData(6, ref thickness);
            DA.GetData(7, ref cload);
            DA.GetData(8, ref load);
            DA.GetData(9, ref creact);
            DA.GetData(10, ref react);
            
            //Lines and forces
            externalforces = LoadMaker(network.Points, network.N, loads, scale);
            edges = ToLines(network.Curves);
            reactionforces = ReactionMaker(Solver.Reactions(network), network.Points, network.F, scale);

            //element-wise values
            if (prop == 0)
            {
                property = Solver.Forces(network);
                SetGradient(property.Min(), property.Max());
            }
            else
            {
                property = network.ForceDensities;
                SetGradient(property.Min(), property.Max());
            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (load) args.Display.DrawArrows(externalforces, cload);

            if (react) args.Display.DrawArrows(reactionforces, creact);

            for (int i = 0; i < edges.Length; i++)
            {
                args.Display.DrawLine(edges[i], grad.ColourAt(property[i]), thickness);
            }
        }

        public Line[] ToLines(List<Curve> curves)
        {
            Line[] lines = new Line[curves.Count];
            for (int i = 0; i < curves.Count; i++)
            {
                Curve curve = curves[i];
                Line line = new Line(curve.PointAtStart, curve.PointAtEnd);
                lines[i] = line;
            }

            return lines;
        }

        public GH_Gradient GetGradient(double min, double max)
        {
            GH_Gradient gradient = new GH_Gradient();
            gradient.AddGrip(min, c0);
            gradient.AddGrip(max, c1);

            return gradient;
        }

        public void SetGradient(double min, double max)
        {
            grad = new GH_Gradient();
            grad.AddGrip(min, c0);
            grad.AddGrip(max, c1);
        }

        public Line[] ReactionMaker(List<Vector3d> anchorforces, List<Point3d> points, List<int> F, double scale)
        {
            var mags = anchorforces.Select(p => p.Length).ToList();
            var normalizer = mags.Max();

            List<Line> reactions = new List<Line>();

            for (int i = 0; i < F.Count; i++)
            {
                var index = F[i];
                reactions.Add(new Line(points[index], anchorforces[i] * 2 * scale / normalizer));
            }

            return reactions.ToArray();
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

                    loadvectors.Add(new Line(p, loads[0] / loads[0].Length * scale));
                }
            }
            else
            {
                //extract magnitudes
                var lns = loads.Select(p => p.Length).ToList();
                var normalizer = lns.Max();
                
                for (int i = 0; i < N.Count; i++)
                {
                    int index = N[i];
                    Point3d p = nodes[i];
                    Vector3d l = loads[i];

                    loadvectors.Add(new Line(p, l * scale / normalizer));
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