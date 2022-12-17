using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Bengesht;

namespace FDMremote.GH_Optimization
{
    public class Optimizereceive : GH_Component
    {
        private WsObject wscObj;
        private bool onMessageTriggered;
        private GH_Document ghDocument;
        private bool isAutoUpdate;
        private bool isAskingNewSolution;
        private List<string> buffer = new List<string>();

        /// <summary>
        /// Initializes a new instance of the Optimizereceive class.
        /// </summary>
        public Optimizereceive()
          : base("Optimizereceive", "Nickname",
              "Description",
              "FDMremote", "Optimization")
        {
            this.onMessageTriggered = false;
            this.isAutoUpdate = true;
            this.isAskingNewSolution = false;
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Websocket Objects", "WSC", "websocket objects", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Auto Update", "Upd", "update solution on new message, not recommended for high frequency inputs", GH_ParamAccess.item, true);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Message", "Msg", "message", GH_ParamAccess.item);
            pManager.AddGenericParameter("Status", "Sts", "status", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(1, ref this.isAutoUpdate);

            if (this.ghDocument == null)
            {
                this.ghDocument = OnPingDocument();
                if (this.ghDocument == null) return;

                GH_Document.SolutionEndEventHandler handle = delegate (Object sender, GH_SolutionEventArgs e)
                {

                };

                ghDocument.SolutionEnd += handle;
            }

            if (!this.onMessageTriggered)
            {
                WsObject wscObj = new WsObject();

                if (DA.GetData(0, ref wscObj))
                {
                    if (this.wscObj != wscObj)
                    {
                        this.unsubscribeEventHandlers();
                        this.wscObj = wscObj;
                        this.subscribeEventHandlers();
                    }
                }
                else
                {
                    this.unsubscribeEventHandlers();
                    this.wscObj = null;
                    this.onMessageTriggered = false;
                    return;
                }
            }

            DA.SetData(0, this.wscObj.message);
            DA.SetData(1, WsObjectStatus.GetStatusName(this.wscObj.status));
            this.onMessageTriggered = false;
        }

        private void unsubscribeEventHandlers()
        {
            try { this.wscObj.changed -= this.wscObjOnChanged; }
            catch { }
        }//eof




        private void subscribeEventHandlers()
        {
            this.wscObj.changed += this.wscObjOnChanged;
        }




        private void wscObjOnChanged(object sender, EventArgs e)
        {
            /*
            ghDocument.ScheduleSolution(0, doc =>
            {
                this.onMessageTriggered = true;
                this.ExpireSolution(true);
            });
            */

            if (this.isAutoUpdate && ghDocument.SolutionState != GH_ProcessStep.Process && wscObj != null && !isAskingNewSolution)
            {

                Instances.DocumentEditor.BeginInvoke((Action)delegate ()
                {
                    if (ghDocument.SolutionState != GH_ProcessStep.Process)
                    {
                        isAskingNewSolution = true;
                        this.onMessageTriggered = true;
                        this.ExpireSolution(true);
                        isAskingNewSolution = false;
                    }
                });
            }
        }//eof

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
            get { return new Guid("BD33E8B4-5485-456F-B636-D60B5CC24A24"); }
        }
    }
}