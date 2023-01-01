﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Collections;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace FDMremote.GH_Design.Experimental
{
    public class CtrlSurfPoint : GH_Component
    {
        private List<Point3d> inputPoints;
        private NurbsSurface surf;
        private NurbsSurface offsetSurf;
        private List<Point3d> points;
        private List<Point3d> offsetPoints;
        private List<string> names;
        private List<double> zvalues;
        private List<double> values;
        private BoundingBox bb;
        private double height;
        private double baseline;
        private GH_Document ghd;
        private int ctrlidx = 7;
        private int u;
        private int v;
        private double vmax;
        private double vmin;

        private bool show;
        private Point3d pbl;
        private Point3d pbr;
        private Point3d ptl;

        /// <summary>
        /// Initializes a new instance of the CtrlSurf class.
        /// </summary>
        public CtrlSurfPoint()
          : base("Load Control Surface", "CtrlSurfP",
              "NURBs control surface for dimensionality reduction",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGeometryParameter("Geometry", "Geo", "Geometry to reference", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Generate", "Generate", "Generate the control surface", GH_ParamAccess.item, false);
            pManager.AddPointParameter("Points", "Points", "Reference Points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Ucount", "nU", "Number of points in u direction", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Vcount", "nV", "Number of points in v direction", GH_ParamAccess.item, 4);
            pManager.AddVectorParameter("SurfaceOffset", "Offset", "Offset of displayed control surface (independent of actual value calculation)", GH_ParamAccess.item, new Vector3d(0, 0, -100));
            pManager.AddNumberParameter("MaximumValue", "Max", "Maximum value represented by surface", GH_ParamAccess.item, 1e3);
            pManager.AddNumberParameter("MinimumValue", "Min", "Minimum value represented by surface",
                GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("CtrlValue", "Value", "Surface control point values", GH_ParamAccess.list, 0);
            pManager.AddBooleanParameter("ShowSurface", "Show", "Show the control surface", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "Control Surface", "Output control surface", GH_ParamAccess.item);
            pManager.AddNumberParameter("Values", "Vals", "Output values", GH_ParamAccess.list);

            pManager.HideParameter(0);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //Initialize
            inputPoints = new List<Point3d>();
            u = 3;
            v = 3;
            Vector3d offset = new Vector3d(0, 0, -100);
            vmax = 1e3;
            vmin = -1e3;
            bool reset = false;
            show = true;

            //assign
            DA.GetData(0, ref reset);
            if (!DA.GetDataList(1, inputPoints)) return;
            DA.GetData(2, ref u);
            DA.GetData(3, ref v);
            DA.GetData(8, ref show);

            //upper limit for density of control points
            if (u > 5 && v > 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Number of control points is exceeding human-usable limits");
            }

            DA.GetData(4, ref offset);
            DA.GetData(5, ref vmax);
            DA.GetData(6, ref vmin);

            //active doc
            ghd = this.OnPingDocument();

            //get global bounding box
            GetBB(inputPoints);

            //create sliders
            if (reset)
            {
                ghd.ScheduleSolution(5, SolutionCallback);
            }

            zvalues = new List<double>();
            DA.GetDataList(7, zvalues);

            //create control points
            GetPoints();

            //generate surface
            surf = NurbsSurface.CreateFromPoints(points, u, v, 3, 3);

            //interpolate values
            GetValues();

            //visualized nurbs surface
            GetOffsets(offset);

            DA.SetData(0, offsetSurf);
            DA.SetDataList(1, values);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (show)
            {
                args.Display.DrawPoints(offsetPoints, Rhino.Display.PointStyle.Circle, 3, System.Drawing.Color.MediumAquamarine);
                args.Display.DrawSurface(offsetSurf, System.Drawing.Color.MediumAquamarine, 6);
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox b = new BoundingBox();
                b.Union(bb);

                var points1 = new Point3dList(offsetPoints);
                b.Union(points1.BoundingBox);

                b.Union(offsetSurf.GetBoundingBox(true));

                return b;
            }
        }

        private void GetOffsets(Vector3d offset)
        {
            offsetSurf = (NurbsSurface)surf.Duplicate();
            offsetSurf.Translate(offset);

            offsetPoints = new List<Point3d>();

            foreach (Point3d point in points)
            {
                var offsetpoint = new Point3d(point);

                offsetPoints.Add(offsetpoint + offset);
            }
        }


        private void SolutionCallback(GH_Document gdoc)
        {
            MakeSliders();
        }

        /// <summary>
        /// create sliders
        /// </summary>
        private void MakeSliders()
        {
            if (this.Params.Input[ctrlidx].SourceCount > 0)
            {
                List<IGH_Param> sources = new List<IGH_Param>(this.Params.Input[ctrlidx].Sources);
                ghd.RemoveObjects(sources, false);
            }

            int nsliders = u * v;

            GetNames();

            //instantiate new slider
            for (int i = 0; i < nsliders; i++)
            {
                Grasshopper.Kernel.Special.GH_NumberSlider slider = new Grasshopper.Kernel.Special.GH_NumberSlider();

                slider.CreateAttributes();

                int inputcount = this.Params.Input[ctrlidx].SourceCount;
                var xpos = (float)this.Attributes.DocObject.Attributes.Bounds.Left - slider.Attributes.Bounds.Width - 30;
                var ypos = (float)this.Params.Input[ctrlidx].Attributes.Bounds.Y + inputcount * 30;

                slider.Attributes.Pivot = new System.Drawing.PointF(xpos, ypos);
                slider.Slider.Maximum = 1;
                slider.Slider.Minimum = 0;
                slider.Slider.DecimalPlaces = 2;
                slider.Slider.Value = (decimal)0.5;
                slider.NickName = names[i];

                ghd.AddObject(slider, false);

                this.Params.Input[ctrlidx].AddSource(slider);

            }
        }

        /// <summary>
        /// get bounding box
        /// </summary>
        /// <param name="points"></param>
        private void GetBB(List<Point3d> points)
        {
            bb = new BoundingBox();

            var pointlist = new Point3dList(points);

            bb.Union(pointlist.BoundingBox);

            //get total height of geometry

            pbl = bb.Corner(true, true, true);
            pbr = bb.Corner(false, true, true);
            ptl = bb.Corner(true, false, true);
            var pzl = bb.Corner(true, true, false);

            height = pbl.DistanceTo(pzl);
            baseline = pbl.Z - height;
        }

        /// <summary>
        /// Extract nicknames for sliders
        /// </summary>
        private void GetNames()
        {
            names = new List<string>();
            for (int i = 0; i < u; i++)
            {
                for (int j = 0; j < v; j++)
                {
                    string name = "[" + i.ToString() + ", " + j.ToString() + "]";
                    names.Add(name);
                }
            }
        }

        /// <summary>
        /// Generate point grid
        /// </summary>
        private void GetPoints()
        {
            points = new List<Point3d>();
            //extract spans
            double x = pbl.DistanceTo(pbr);
            double y = pbl.DistanceTo(ptl);

            //spacings
            double xspacing = x / (u - 1);
            double yspacing = y / (v - 1);

            int k = 0;

            for (int i = 0; i < u; i++)
            {
                for (int j = 0; j < v; j++)
                {
                    double px = pbl.X + xspacing * i;
                    double py = pbl.Y + yspacing * j;
                    double pz;

                    if (zvalues.Count == 1)
                    {
                        pz = height * zvalues[0] + baseline;
                    }
                    else
                    {
                        pz = height * zvalues[k] + baseline;
                    }


                    points.Add(new Point3d(px, py, pz));
                    k++;
                }
            }

        }

        private void GetValues()
        {
            values = new List<double>();
            double range = vmax - vmin;
            foreach (Point3d point in inputPoints)
            {
                Vector3d ray = -2 * height * Vector3d.ZAxis;
                Line line = new Line(point, ray);

                var inter = Intersection.CurveSurface(line.ToNurbsCurve(), surf, 1e-2, 1e-2)[0];

                Point3d interpoint = inter.PointB;

                double val = vmin + (interpoint.Z - baseline) / height * range;

                values.Add(val);
            }
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
            get { return new Guid("5C6A252F-A943-46A1-B609-7E80B9DDAD73"); }
        }
    }
}