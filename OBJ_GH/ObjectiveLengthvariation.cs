using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;

namespace FDMremote.OBJ_GH
{
    public class ObjectiveLengthvariation : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectiveLengthvariation class.
        /// </summary>
        public ObjectiveLengthvariation()
          : base("OBJLengthVariation", "OBJLength",
              "Minimize the difference between the longest and shortest elements",
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
            pManager.AddGenericParameter("OBJTarget", "OBJ", "Length Objective Function", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double weight = 1.0;
            DA.GetData(0, ref weight);

            OBJlengthvariation obj = new OBJlengthvariation(weight);

            DA.SetData(0, obj);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.OBJlength;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("2F4C6C0C-8D39-4764-979C-FEF17F1DB38D"); }
        }
    }
}