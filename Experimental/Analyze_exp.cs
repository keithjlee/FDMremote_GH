using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;
using MathNet.Numerics.LinearAlgebra;

namespace FDMremote.Experimental
{
    public class Analyze_exp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Analyze_exp class.
        /// </summary>
        public Analyze_exp()
          : base("Analyze_exp", "Analyze",
              "Analyze a FDM network",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Input FDM network", GH_ParamAccess.item);
            pManager.AddVectorParameter("Load Vector", "P", "Load Vector", GH_ParamAccess.list);
            pManager.AddNumberParameter("IntersectionTolerance", "tol", "Geometric tolerance for connecting geometry", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Cf", "Cf", "Connectivity Matrix", GH_ParamAccess.item);
            pManager.AddTextParameter("Cn", "Cn", "Connectivity Matrix", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //read
            Network fdmNetwork = new Network();
            List<Vector3d> loads = new List<Vector3d>();
            double tol = 1.0;

            if (!DA.GetData(0, ref fdmNetwork)) return;
            if (!DA.GetDataList(1, loads)) return;
            if (!DA.GetData(2, ref tol)) return;

            //analysis
            FDMproblem prob = new FDMproblem(fdmNetwork, loads, tol);

            var A = prob.Cn.TransposeThisAndMultiply(prob.Q) * prob.Cn;
            var B = prob.Pn - (prob.Cn.TransposeThisAndMultiply(prob.Q) * prob.Cf * prob.XYZf);

            var xyzNew = A.Cholesky().Solve(B);

            DA.SetData(0, xyzNew.ToString());
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
            get { return new Guid("63B48EA8-1D76-43AD-BCA1-BBC7C3CA598A"); }
        }
    }
}