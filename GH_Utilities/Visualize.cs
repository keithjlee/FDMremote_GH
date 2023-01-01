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
using System.Data.SqlClient;

namespace FDMremote.GH_Analysis
{
    public class Visualize : GH_Component
    {
        //persistent data
        Line[] edges;
        List<double> property;
        Line[] externalforces;
        Line[] reactionforces;
        System.Drawing.Color c0;
        System.Drawing.Color cmed;
        System.Drawing.Color c1;
        GH_Gradient grad;
        int thickness;
        System.Drawing.Color cload;
        System.Drawing.Color creact;
        bool load;
        bool react;
        int prop;

        //default colours
        readonly System.Drawing.Color lightgray = System.Drawing.Color.FromArgb(230, 231, 232);
        readonly System.Drawing.Color blue = System.Drawing.Color.FromArgb(62, 168, 222);
        readonly System.Drawing.Color pink = System.Drawing.Color.FromArgb(255, 123, 172);
        readonly System.Drawing.Color green = System.Drawing.Color.FromArgb(71, 181, 116);
        readonly System.Drawing.Color red = System.Drawing.Color.FromArgb(235, 52, 73);


        /// <summary>
        /// Initializes a new instance of the Visualize class.
        /// </summary>
        public Visualize()
          : base("Visualize Network", "Visualize",
              "Visualize a FDM network",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "Network to visualize", GH_ParamAccess.item);
            pManager.AddVectorParameter("Loads", "P", "Applied loads", GH_ParamAccess.list, new Vector3d(0, 0, 0));
            pManager.AddNumberParameter("Load Scale", "Pscale", "Scale factor for length of arrows", GH_ParamAccess.item, 1.0);
            pManager.AddColourParameter("ColourMin", "Cmin", "Colour for minimum value", GH_ParamAccess.item, pink);
            pManager.AddColourParameter("ColourMed", "Cmed", "Colour for neutral value", GH_ParamAccess.item, lightgray);
            pManager.AddColourParameter("ColourMax", "Cmax", "Colour for maximum value",
                GH_ParamAccess.item, blue);
            pManager.AddIntegerParameter("Color Property", "Property", "Property displayed by colour gradient", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Line Thickness", "Thickness", "Thickness of preview lines", GH_ParamAccess.item, 2);
            pManager.AddColourParameter("Load Colour", "Cload", "Colour for applied loads", GH_ParamAccess.item, red);
            pManager.AddBooleanParameter("Show Loads", "Load", "Show external loads in preview", GH_ParamAccess.item, true);
            pManager.AddColourParameter("Reaction Colour", "Creaction", "Colour for support reactions", GH_ParamAccess.item, green);
            pManager.AddBooleanParameter("Show Reactions", "Reaction", "Show anchor reactions in preview", GH_ParamAccess.item, false);

            Param_Integer param = pManager[6] as Param_Integer;
            param.AddNamedValue("None", -1);
            param.AddNamedValue("Force", 0);
            param.AddNamedValue("Q", 1);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
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
            c0 = pink; // min colour (light gray)
            cmed = lightgray;
            c1 = blue; // max colour (blue)
            cload = pink; // load colour (pink)
            load = true;
            creact = green; // reaction colour green)
            react = false;

            //Assign
            if(!DA.GetData(0, ref network)) return;
            DA.GetDataList(1, loads);
            DA.GetData(2, ref scale);
            DA.GetData(3, ref c0);
            DA.GetData(4, ref cmed);
            DA.GetData(5, ref c1);
            DA.GetData(6, ref prop);
            DA.GetData(7, ref thickness);
            DA.GetData(8, ref cload);
            DA.GetData(9, ref load);
            DA.GetData(10, ref creact);
            DA.GetData(11, ref react);

            //Lines and forces
            externalforces = LoadMaker(network.Points, network.N, loads, scale);
            edges = ToLines(network.Curves);
            reactionforces = ReactionMaker(Solver.Reactions(network), network.Points, network.F, scale);
            
            //element-wise values
            if (prop == 0)
            {
                property = Solver.Forces(network.Curves, network.ForceDensities);
                
                var propabs = property.Select(x => Math.Abs(x)).ToList();

                SetGradient(propabs.Max());
            }
            else if (prop == 1)
            {
                property = network.ForceDensities;
                
                var propabs = property.Select(x => Math.Abs(x)).ToList();
                SetGradient(propabs.Max());
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox bb = new BoundingBox();
                for (int i = 0; i < externalforces.Length; i++) bb.Union(externalforces[i].BoundingBox);
                for (int i = 0; i < reactionforces.Length; i++) bb.Union(reactionforces[i].BoundingBox);

                return bb;
            }
        }
        
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (load) args.Display.DrawArrows(externalforces, cload);

            if (react) args.Display.DrawArrows(reactionforces, creact);

            if (prop == -1) args.Display.DrawLines(edges, c1, thickness);
            else
            {
                for (int i = 0; i < edges.Length; i++)
                {
                    args.Display.DrawLine(edges[i], grad.ColourAt(property[i]), thickness);
                }
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

        //public GH_Gradient GetGradient(double min, double max)
        //{
        //    GH_Gradient gradient = new GH_Gradient();
        //    gradient.AddGrip(min, c0);
        //    gradient.AddGrip(max, c1);

        //    return gradient;
        //}

        public void SetGradient(double max)
        {
            grad = new GH_Gradient();
            grad.AddGrip(-max, c0);
            grad.AddGrip(0, cmed);
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
                reactions.Add(new Line(points[index], anchorforces[i] * 3 * scale / normalizer));
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
                    Point3d p = nodes[index];
                    Vector3d l = loads[i];

                    if (l.Length < 0.1)
                    {
                        continue;
                    }
                    else
                    {
                        loadvectors.Add(new Line(p, l * scale / normalizer));
                    }
                    
                }
            }

            return loadvectors.ToArray();
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.visualize;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5B9176B3-B940-4C2C-AFFE-BF4532FB2111"); }
        }
    }
}