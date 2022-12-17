using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Analysis;

namespace FDMremote.GH_Analysis
{
    public class NetworkInfo : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the VisualizeNetwork class.
        /// </summary>
        public NetworkInfo()
          : base("Network Information", "Info",
              "Information about a FDM network",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "FDM network class", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "N", "Nodes", GH_ParamAccess.list);
            pManager.AddCurveParameter("Edges", "E", "Edges", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchors", "A", "Anchor nodes", GH_ParamAccess.list);
            pManager.AddNumberParameter("ForceDensities", "q", "Force densities of edges", GH_ParamAccess.list);
            pManager.AddIntegerParameter("NumberEdges", "Ne", "Number of edges", GH_ParamAccess.item);
            pManager.AddIntegerParameter("NumberNodes", "Nn", "Number of nodes", GH_ParamAccess.item);
            pManager.AddIntegerParameter("FreeIndices", "iN", "Indices of free points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("FixedIndices", "iF", "Indices of fixed points", GH_ParamAccess.list);
            pManager.AddNumberParameter("MemberForces", "Force", "Internal forces assuming L = stressed length", GH_ParamAccess.list);
            pManager.AddVectorParameter("Reaction Forces", "Reactions", "Force vectors acting at anchor points", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Network fdmNetwork = new Network();
            if (!DA.GetData(0, ref fdmNetwork)) return;

            List<double> forces = Solver.Forces(fdmNetwork);
            List<Vector3d> reactions = Solver.Reactions(fdmNetwork);

            DA.SetDataList(0, fdmNetwork.Points);
            DA.SetDataList(1, fdmNetwork.Curves);
            DA.SetDataList(2, fdmNetwork.Anchors);
            DA.SetDataList(3, fdmNetwork.ForceDensities);
            DA.SetData(4, fdmNetwork.Ne);
            DA.SetData(5, fdmNetwork.Nn);
            DA.SetDataList(6, fdmNetwork.N);
            DA.SetDataList(7, fdmNetwork.F);
            DA.SetDataList(8, forces);
            DA.SetDataList(9, reactions);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Information;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4E02AFBA-6E24-4855-9A0F-91E2042ED22B"); }
        }
    }
}