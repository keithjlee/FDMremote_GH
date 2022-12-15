using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;

namespace FDMremote.GH_Optimization
{
    public class OptimizationParameters : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OptimizationParameters class.
        /// </summary>
        public OptimizationParameters()
          : base("OptimizationParameters", "Params",
              "Parameters for optimization",
              "FDMremote", "Optimization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Objective Functions", "Obj", "Objective function(s) to minimize; defaults to TargetShape", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lower Bound", "LB", "Lower bound of force densities", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Upper Bound", "UB", "Upper bound of force densities", GH_ParamAccess.item, 100);
            pManager.AddNumberParameter("Absolute Tolerance", "AbsTol", "Absolute stopping tolerance", GH_ParamAccess.item, 1e-3);
            pManager.AddNumberParameter("Relative Tolerance", "RelTol", "Relative stopping tolerance", GH_ParamAccess.item, 1e-3);
            pManager.AddIntegerParameter("Maximum Iterations", "MaxIter", "Maximum number of iterations", GH_ParamAccess.item, 500);
            pManager.AddIntegerParameter("Update Frequency", "Frequency", "Frequency of return reports", GH_ParamAccess.item, 10);
            pManager.AddBooleanParameter("Show Iterations", "ShowIter", "Show intermittent solutions", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Parameters for optimization", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            List<OBJ> objs = new List<OBJ> { new OBJTarget(1.0) };
            //List<OBJ> objs = new List<OBJ>();
            double lb = 0.1;
            double ub = 100.0;
            double abstol = 1e-3;
            double reltol = 1e-3;
            int maxiter = 500;
            int freq = 10;
            bool show = true;

            //assign
            if (!DA.GetDataList(0, objs)) return;
            //if(!DA.GetDataList(0, objs)) objs = new List<OBJ> { new OBJNull()};
            DA.GetData(1, ref lb);
            DA.GetData(2, ref ub);
            DA.GetData(3, ref abstol);
            DA.GetData(4, ref reltol);
            DA.GetData(5, ref maxiter);
            DA.GetData(6, ref freq);
            DA.GetData(7, ref show);


            //if (!DA.GetData(1, ref lb)) return;
            //if (!DA.GetData(2, ref ub)) return;
            //if (!DA.GetData(3, ref abstol)) return;
            //if (!DA.GetData(4, ref reltol)) return;
            //if (!DA.GetData(5, ref maxiter)) return;
            //if (!DA.GetData(6, ref freq)) return;
            //if (!DA.GetData(7, ref show)) return;

            //create parameter
            OBJParameters objparams = new OBJParameters(lb, ub, abstol, reltol, objs, show, freq, maxiter);

            DA.SetData(0, objparams);
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
            get { return new Guid("C94AAD07-60DA-4A0A-8E30-1CE1B46B685A"); }
        }
    }
}