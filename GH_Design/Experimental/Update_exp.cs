using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.UI;
using FDMremote.Utilities;

namespace FDMremote.GH_Design.Experimental
{
    public class Update_exp : GH_Component
    {
        private List<Guid> guids;
        private Network network;
        private RhinoDoc doc;
        private GH_Document ghd;
        private List<IGH_DocumentObject> relevantObjs;

        /// <summary>
        /// Initializes a new instance of the Update_exp class.
        /// </summary>
        public Update_exp()
          : base("Update_exp", "Update_exp",
              "Description",
              "FDMremote", "Experimental")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IGH_Param param = new Grasshopper.Kernel.Parameters.Param_Guid();
            pManager.AddBooleanParameter("Update curves?", "Update", "Update the drawn curves to new positions", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target Network", "Network", "Target network to update curves to", GH_ParamAccess.item);
            pManager.AddParameter(param, "Edge GUIDs", "GUIDs", "GUIDs of input network edges", GH_ParamAccess.list);
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
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool update = false;
            guids = new List<Guid>();

            DA.GetData(0, ref update);
            if (!DA.GetData(1, ref network)) return;
            if (!DA.GetDataList(2, guids)) return;

            if (network.Curves.Count != guids.Count)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Length of GUIDs must match length of edges in network");
            }

            doc = RhinoDoc.ActiveDoc;

            ghd = this.OnPingDocument();
            var ghdobjs = ghd.Objects;

            relevantObjs = new List<IGH_DocumentObject>();
            foreach (IGH_DocumentObject obj in ghdobjs)
            {
                if (obj.NickName == "Pipeline") relevantObjs.Add(obj);
            }

            foreach (IGH_DocumentObject Obj in relevantObjs)
            {
                GH_ActiveObject comp = (GH_ActiveObject)Obj;
                //comp.ExpireSolution(false);
            }

            if (update)
            {
                ghd.ScheduleSolution(10, updater);
            }



        }


        private void updater(GH_Document gdoc)
        {
            foreach (IGH_DocumentObject Obj in relevantObjs)
            {
                GH_ActiveObject comp = (GH_ActiveObject)Obj;
                comp.Locked = true;
                comp.ClearData();
            }

            for (int i = 0; i < guids.Count; i++)
            {
                Guid guid = guids[i];

                Curve newcurve = (Curve)network.Curves[i].Duplicate();

                doc.Objects.Replace(guid, newcurve);

            }

            foreach (IGH_DocumentObject Obj in relevantObjs)
            {
                GH_ActiveObject comp = (GH_ActiveObject)Obj;
                comp.Locked = false;
            }

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
            get { return new Guid("4A910A8B-A65E-41FA-9909-A39700EDEBCD"); }
        }
    }
}