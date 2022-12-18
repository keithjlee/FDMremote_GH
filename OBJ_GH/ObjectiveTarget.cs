using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;

namespace FDMremote.OBJ_GH
{
    public class ObjectiveTarget : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectiveTarget class.
        /// </summary>
        public ObjectiveTarget()
          : base("Target", "OBJTarget",
              "Minimize the deviation from starting nodal positions",
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
            pManager.AddGenericParameter("OBJTarget", "OBJ", "Target Objective Function", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double weight = 1.0;
            DA.GetData(0, ref weight);

            OBJTarget obj = new OBJTarget(weight);

            DA.SetData(0, obj);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Target;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6AA55256-288F-49A6-9648-5314691BB659"); }
        }
    }
}