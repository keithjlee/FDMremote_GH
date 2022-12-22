using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Rhino;

namespace FDMremote.GH_Utilities
{
    public class UpdateCurves : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the UpdateCurves class.
        /// </summary>
        public UpdateCurves()
          : base("Update Geometry", "Update",
              "Match drawn network to target solved network",
              "FDMremote", "Utilities")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param param = new Grasshopper.Kernel.Parameters.Param_Guid();
            pManager.AddBooleanParameter("Update curves?", "Update", "Update the drawn curves to new positions", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target Network", "Network", "Target network to update curves to", GH_ParamAccess.item);
            //pManager.AddParameter(param, "Curve GUIDs", "GUIDs", "GUID of curves", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// 


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool update = false;
            Network network = new Network();
            //List<Guid> guids = new List<Guid>();

            DA.GetData(0, ref update);
            if (!DA.GetData(1, ref network)) return;
            //if (!DA.GetDataList(2, guids)) return;

            List <Guid> guids = network.Guids;
            var doc = RhinoDoc.ActiveDoc;

            if (update)
            {
                for (int i = 0; i < guids.Count; i++)
                {
                    Guid guid = guids[i];
                    Curve newcurve = network.Curves[i];

                    doc.Objects.Replace(guid, newcurve);
                }
            }
        }



        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.update;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("11D6D7E9-EE9F-4B5E-A35F-087F852403CC"); }
        }
    }
}