using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using FDMremote.Utilities;

namespace FDMremote
{
    public class CreateNetwork : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CreateNetwork()
          : base("Create Network", "Create",
            "Create FDM network",
            "FDMremote", "Design")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param param = new Grasshopper.Kernel.Parameters.Param_Guid();
            pManager.AddCurveParameter("Edges", "E", "Edges between nodes", GH_ParamAccess.list);
            pManager.AddParameter(param, "Edge GUIDs", "GUIDs", "GUID of edges", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchors", "A", "Anchor points", GH_ParamAccess.list);
            pManager.AddNumberParameter("ForceDensities", "q", "Force density of edges", GH_ParamAccess.list, 1.0);
            pManager.AddNumberParameter("IntersectionTolerance", "tol", "Geometric tolerance for connecting geometry", GH_ParamAccess.item, 0.1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Network", "Network", "FDM network", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            List<Curve> edges = new List<Curve>();
            List<Guid> guids = new List<Guid>();
            List<Point3d> anchors = new List<Point3d>();
            List<double> q = new List<double>();
            double tol = 1.0;

            //assign
            if (!DA.GetDataList(0, edges)) return;
            if (!DA.GetDataList(1, guids)) return;
            if (!DA.GetDataList(2, anchors)) return;
            if (!DA.GetDataList(3, q)) return;
            DA.GetData(4, ref tol);

            if (edges.Count != guids.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of GUIDs must match length of edges");
            }

            //create network
            Network fdmNetwork = new Network(anchors, edges, guids, q, tol);

            //Check sufficent anchor definitions
            if (!fdmNetwork.AnchorCheck())
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For stability, define at least 3 anchor points.");
                return ;
            }

            //check that fixed/free nodes were properly captured
            if (!fdmNetwork.NFCheck())
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Populating fixed/free nodes did not succeed; modify 'tol'");
                return ;
            }

            //check that all elements have valid indices
            if (!fdmNetwork.IndexCheck())
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Start/end indices for all edges did not succeed; modify 'tol'");
                return ;
            }

            //assign
            DA.SetData(0, fdmNetwork);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Create;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("5DBF67D7-9DCE-4B0B-9DE9-15422CD2B4FA");
    }
}