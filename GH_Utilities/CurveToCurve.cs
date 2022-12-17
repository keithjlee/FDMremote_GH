using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;

namespace FDMremote.GH_Analysis
{
    public class CurveToCurve : GH_Component
    {
        Line[] lines;
        System.Drawing.Color col;
        int thickness;
        bool show;

        /// <summary>
        /// Initializes a new instance of the CurveToCurve class.
        /// </summary>
        public CurveToCurve()
          : base("CurveToCurve", "CurvePairs",
              "Displays the start and end orientation of curves pre/post analysis",
              "FDMremote", "Utilities")
        {
        }
         
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Initial Network", "Network1", "Network before analysis", GH_ParamAccess.item);
            pManager.AddGenericParameter("Solved Network", "Network2", "Network after analysis", GH_ParamAccess.item);
            pManager.AddColourParameter("Line Colour", "Colour", "Colour of line", GH_ParamAccess.item, System.Drawing.Color.SlateGray);
            pManager.AddIntegerParameter("Line Thickness", "Weight", "Weight of lines", GH_ParamAccess.item, 2);
            pManager.AddBooleanParameter("Show", "Show", "Show assignments", GH_ParamAccess.item, true);
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
            Network fdm1 = new Network();
            Network fdm2 = new Network();
            col = System.Drawing.Color.SlateGray;
            thickness = 2;
            show = true;

            if (!DA.GetData(0, ref fdm1)) return;
            if (!DA.GetData(1, ref fdm2)) return;

            if (fdm1.Ne != fdm2.Ne) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Input and output networks must have same number of elements");

            lines = GetLines(fdm1, fdm2);

            DA.GetData(2, ref col);
            DA.GetData(3, ref thickness);
            DA.GetData(4, ref show);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (show) args.Display.DrawLines(lines, col, thickness);

        }

        private Line[] GetLines(Network n1, Network n2)
        {
            Line[] ls = new Line[n1.Curves.Count];

            for (int i = 0; i < n1.Curves.Count; i++)
            {
                Curve c1 = n1.Curves[i];
                Curve c2 = n2.Curves[i];

                ls[i] = new Line(c1.PointAtNormalizedLength(0.5), c2.PointAtNormalizedLength(0.5));
            }

            return ls;
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
            get { return new Guid("4E9B60C2-E43A-4680-800A-B66871229F3B"); }
        }
    }
}