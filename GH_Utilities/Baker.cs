using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;

namespace FDMremote.GH_Utilities
{
    public class Baker : GH_Component
    {
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
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Curves", "Network edges", GH_ParamAccess.list);
            pManager.AddPointParameter("Anchors", "Anchors", "Network anchors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Network network = new Network();

            if (!DA.GetData(0, ref network)) return;

            DA.SetDataList(0, network.Curves);
            DA.SetDataList(1, network.Anchors);
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