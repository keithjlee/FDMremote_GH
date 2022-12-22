﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Rhino;
using FDMremote.Optimization;
using Eto.Forms;
using Rhino.UI;

namespace FDMremote.GH_Utilities
{
    
    public class UpdateCurves : GH_Component
    {
        private List<Guid> guids;
        private Network network;
        private RhinoDoc doc;
        private List<IGH_DocumentObject> relevantObjs;
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
            pManager.AddBooleanParameter("Update curves?", "Update", "Update the drawn curves to new positions", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Target Network", "Network", "Target network to update curves to", GH_ParamAccess.item);
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

            DA.GetData(0, ref update);
            if (!DA.GetData(1, ref network)) return;

            guids = network.Guids;
            doc = RhinoDoc.ActiveDoc;

            GH_Document ghd = this.OnPingDocument();
            var ghdobjs = ghd.Objects;

            relevantObjs = new List<IGH_DocumentObject>();
            foreach (IGH_DocumentObject obj in ghdobjs)
            {
                if (obj.NickName == "Pipeline") relevantObjs.Add(obj);
            }

            foreach (IGH_DocumentObject Obj in relevantObjs)
            {
                GH_ActiveObject comp = (GH_ActiveObject)Obj;
                comp.ExpireSolution(false);
            }

            if (update)
            {
                ghd.ScheduleSolution(10, updater);
            }

        }


        private void updater(GH_Document gdoc)
        {
            var ghdobjs = gdoc.Objects;
            foreach (IGH_DocumentObject Obj in relevantObjs)
            {
                GH_ActiveObject comp = (GH_ActiveObject)Obj;
                comp.Locked = true;
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