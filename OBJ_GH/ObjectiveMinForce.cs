using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;

namespace FDMremote.OBJ_GH
{
    public class ObjectiveMinForce : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectiveMinForce class.
        /// </summary>
        public ObjectiveMinForce()
          : base("MinimumForce", "OBJMinForce",
              "Penalizes force values below threshold",
              "FDMremote", "ObjectiveFunctions")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Weight", "W", "Weight of objective", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Minimum Force", "Force", "Target minimum force", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("OBJMinForce", "OBJ", "Minimum force function", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double weight = 1.0;
            double force = 1.0;

            DA.GetData(0, ref weight);
            DA.GetData(1, ref force);

            OBJMinforce obj = new OBJMinforce(force, weight);

            DA.SetData(0, obj);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.minforce;
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("C0F8AE23-2A8B-4800-8675-1C652502BCF9"); }
        }
    }
}