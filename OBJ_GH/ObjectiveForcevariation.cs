using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;

namespace FDMremote.OBJ_GH
{
    public class ObjectiveForcevariation : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectiveForcevariation class.
        /// </summary>
        public ObjectiveForcevariation()
          : base("OBJForceVariation", "OBJForce",
              "Minimize the difference between the largest and smallest member forces.",
              "FDMremote", "Objective Functions")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Weight", "W", "Weight of objective", GH_ParamAccess.item, 1.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("OBJForce", "OBJ", "Force Variation Function", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double weight = 1.0;
            DA.GetData(0, ref weight);

            OBJforcevariation obj = new OBJforcevariation(weight);

            DA.SetData(0, obj);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.OBJforcedev;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DDF4BBFC-685F-4D1F-A4BB-0E91B539D149"); }
        }
    }
}