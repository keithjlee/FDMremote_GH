using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Optimization;

namespace FDMremote.OBJ_GH
{
    public class ObjectiveMaxLength : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ObjectiveMaxLength class.
        /// </summary>
        public ObjectiveMaxLength()
          : base("MaximumLength", "OBJMaxLength",
              "Penalizes edge lengths above threshold",
              "FDMremote", "ObjectiveFunctions")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Weight", "W", "Weight of objective", GH_ParamAccess.item, 1.0);
            pManager.AddNumberParameter("Maximum Length", "Length", "Target maximum length", GH_ParamAccess.item, 1000.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("OBJMaxLength", "OBJ", "Maximum length function", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double weight = 1.0;
            double length = 1.0;

            DA.GetData(0, ref weight);
            DA.GetData(1, ref length);

            OBJMaxlength obj = new OBJMaxlength(length, weight);

            DA.SetData(0, obj);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.maxlength;
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A4EF19AE-A001-4902-B8B7-65117EBC4F45"); }
        }
    }
}