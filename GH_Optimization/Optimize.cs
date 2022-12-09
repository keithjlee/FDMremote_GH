using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using Newtonsoft.Json;

namespace FDMremote.GH_Optimization
{
    public class Optimize : GH_Component
    {
        public List<double> q = new List<double>(); //persistent q values

        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
        public Optimize()
          : base("OptimizeRemote", "Optimize",
              "Use FDMremote.jl to optimize the network",
              "FDMremote", "Optimization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Network to Optimize", GH_ParamAccess.item); // Network
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Objective functions, tolerances, etc.", GH_ParamAccess.item); // all other parameters
            pManager.AddVectorParameter("Load", "P", "Applied load", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("FDM Network", "Network", "Optimized network", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Objective function value", "Loss", "Result of optimization", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Optimized variables", "X", "Design variables of optimized solution", GH_ParamAccess.list);
            pManager.AddTextParameter("Output", "Out", "TestOutput", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            Network network = new Network();
            OBJParameters objparams = new OBJParameters();
            List<Vector3d> loads = new List<Vector3d>();

            if (!DA.GetData(0, ref network)) return;
            if (!DA.GetData(1, ref objparams)) return;
            if (!DA.GetDataList(2, loads)) return;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != network.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            OptimizationProblem optimprob = new OptimizationProblem(network, objparams, loads);

            //just testing
            string data = JsonConvert.SerializeObject(optimprob);
            DA.SetData(0, data);
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
            get { return new Guid("99C37348-2761-466F-BD63-2B2381150116"); }
        }
    }
}