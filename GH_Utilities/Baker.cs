using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Rhino.Render;
using Grasshopper.Kernel.Parameters;
using Rhino;
using FDMremote.Analysis;
using System.Linq;
using Eto.Forms;
using Grasshopper.GUI.Gradient;
using Rhino.DocObjects;

namespace FDMremote.GH_Utilities
{
    public class Baker : GH_Component
    {

        //persistent data
        System.Drawing.Color c0;
        System.Drawing.Color cmed;
        System.Drawing.Color c1;
        GH_Gradient grad;
        int prop;
        Network network;
        string layer;
        int layeridx;

        RhinoDoc doc;
        BoundingBox bb;
        Vector3d offset;
        Vector3d fulloffset;

        List<ObjectAttributes> attribs;

        //default colours
        readonly System.Drawing.Color lightgray = System.Drawing.Color.FromArgb(230, 231, 232);
        readonly System.Drawing.Color blue = System.Drawing.Color.FromArgb(62, 168, 222);
        readonly System.Drawing.Color pink = System.Drawing.Color.FromArgb(255, 123, 172);


        /// <summary>
        /// Initializes a new instance of the Baker class.
        /// </summary>
        public Baker()
          : base("Baker", "Baker",
              "Component to bake all relevant network geometry",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "Network to bake", GH_ParamAccess.item);
            pManager.AddTextParameter("BakeLayer", "Layer", "Bake target layer", GH_ParamAccess.item, "Layer 05");
            pManager.AddColourParameter("ColourMin", "Cmin", "Colour for minimum value", GH_ParamAccess.item, pink);
            pManager.AddColourParameter("ColourMed", "Cmed", "Colour for neutral value", GH_ParamAccess.item, lightgray);
            pManager.AddColourParameter("ColourMax", "Cmax", "Colour for maximum value",
                GH_ParamAccess.item, blue);
            pManager.AddIntegerParameter("Color Property", "Property", "Property displayed by colour gradient", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Bake", "Bake", "Bake geometry", GH_ParamAccess.item, false);
            pManager.AddVectorParameter("BakeOffset", "Offset", "Additional offset of baked geometry (defaults to 1.5x bounding box to left", GH_ParamAccess.item, new Vector3d(0, 0, 0));

            Param_Integer param = pManager[5] as Param_Integer;
            param.AddNamedValue("None", -1);
            param.AddNamedValue("Force", 0);
            param.AddNamedValue("Q", 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Curves", "Network edges", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchors", "Anchors", "Network anchors", GH_ParamAccess.list);

            pManager.HideParameter(0);
            pManager.HideParameter(1);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            network = new Network();
            layer = "Layer 05";
            prop = 0;
            bool bake = false;
            offset = new Vector3d();

            c0 = pink;
            cmed = lightgray;
            c1 = blue;

            if (!DA.GetData(0, ref network)) return;
            DA.GetData(1, ref layer);
            DA.GetData(2, ref c0);
            DA.GetData(3, ref cmed);
            DA.GetData(4, ref c1);
            DA.GetData(5, ref prop);
            DA.GetData(6, ref bake);
            DA.GetData(7, ref offset);

            doc = RhinoDoc.ActiveDoc;

            if (bake)
            {
                GetLayerInfo();
                GetBB();
                ColourMaker();
                Bake();
            }
            
            DA.SetDataList(0, network.Curves);
            DA.SetDataList(1, network.Anchors);
        }

        public void Bake()
        {
            for (int i = 0; i < network.Curves.Count; i++)
            {
                var curve = network.Curves[i].Duplicate();

                curve.Translate(fulloffset);

                doc.Objects.AddCurve((Curve)curve, attribs[i]);
                
            }

            var attribute = new ObjectAttributes();
            attribute.LayerIndex = layeridx;

            foreach (Point3d anchor in network.Anchors)
            {
                var newanchor = new Point3d(anchor) + fulloffset;

                doc.Objects.AddPoint(newanchor, attribute);
            }
        }

        public void GetLayerInfo()
        {
            layeridx = 2;
            var layerid = doc.Layers.FindName(layer, RhinoMath.UnsetIntIndex);
            if (layerid == null)
            {
                doc.Layers.Add(layer, c1);
            }

            layeridx = doc.Layers.FindName(layer, RhinoMath.UnsetIntIndex).Index;

        }

        public void ColourMaker()
        {
            attribs = new List<ObjectAttributes>();

            List<double> property = new List<double>();
            //element-wise values
            if (prop == 0)
            {
                property = Solver.Forces(network.Curves, network.ForceDensities);
                GradientMaker(property);
            }
            else if (prop == 1)
            {
                property = network.ForceDensities;
                GradientMaker(property);
            }

            foreach (double p in property)
            {
                var col = grad.ColourAt(p);

                var attribute = new ObjectAttributes();
                attribute.ColorSource = ObjectColorSource.ColorFromObject;
                attribute.ObjectColor = col;
                attribute.LayerIndex = layeridx;

                attribs.Add(attribute);
            }
        }


        public void GradientMaker(List<double> property)
        {
            double minprop = property.Min();
            double maxprop = property.Max();

            int signmin = Math.Sign(minprop);
            int signmax = Math.Sign(maxprop);

            //all data is negative
            if (signmin <= 0 && signmax <= 0)
            {
                grad = new GH_Gradient();
                grad.AddGrip(minprop, c0);
                grad.AddGrip(0, cmed);
            }
            //negative and positive values
            else if (signmin <= 0 && signmax >= 0)
            {
                grad = new GH_Gradient();
                grad.AddGrip(minprop, c0);
                grad.AddGrip(0, cmed);
                grad.AddGrip(maxprop, c1);
            }
            //all positive
            else
            {
                grad = new GH_Gradient();
                grad.AddGrip(0, cmed);
                grad.AddGrip(maxprop, c1);
            }


        }

        private void GetBB()
        {
            fulloffset = new Vector3d();
            bb = new BoundingBox(network.Points);


            var pbl = bb.Corner(true, true, true);
            var pbr = bb.Corner(false, true, true);
            fulloffset = (pbl - pbr) * 1.5 + offset;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.baker;
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7597DF61-4AF4-4692-83BD-EA197D02F183"); }
        }
    }
}