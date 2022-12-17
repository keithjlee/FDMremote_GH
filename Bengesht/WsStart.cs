using System;
using System.Collections.Generic;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;
using System.Windows.Forms;
using Grasshopper;

namespace FDMremote.Bengesht
{
    public class WsStart : GH_Component
    {
        private WsObject wscObj;
        private bool isSubscribedToEvents;
        private GH_Document ghDocument;
        private WsAddress wsAddress;

        /// <summary>
        /// Initializes a new instance of the WsStart class.
        /// </summary>
        public WsStart()
          : base("FDMstart", "Start",
              "Start client-server connection",
              "FDMremote", "Optimization")
        {
            this.isSubscribedToEvents = false;
            this.wsAddress = new WsAddress("");
        }

        ~WsStart()
        {
            this.disconnect();
        }

        private void disconnect()
        {
            if (this.wscObj != null)
            {
                try { this.wscObj.disconnect(); }
                catch { }
                this.wscObj.changed -= this.wsObjectOnChange;
                this.wscObj = null;
                this.wsAddress.setAddress(null);
            }
        }

        private void documentOnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this))
            {
                e.Document.ObjectsDeleted -= documentOnObjectsDeleted;
                this.disconnect();
            }
        }

        private void documentServerOnDocumentClosed(GH_DocumentServer sender, GH_Document doc)
        {
            if (this.ghDocument != null && doc.DocumentID == this.ghDocument.DocumentID)
            {
                this.disconnect();
            }
        }

        void onObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (this.Locked)
                this.disconnect();
        }

        private void subscribeToEvents()
        {
            if (!this.isSubscribedToEvents)
            {
                this.ghDocument = OnPingDocument();

                if (this.ghDocument != null)
                {
                    this.ghDocument.ObjectsDeleted += documentOnObjectsDeleted;
                    Instances.DocumentServer.DocumentRemoved += documentServerOnDocumentClosed;
                }

                this.ObjectChanged += this.onObjectChanged;
                this.isSubscribedToEvents = true;
            }
        }

        private void wsObjectOnChange(object sender, EventArgs e)
        {
            this.Message = WsObjectStatus.GetStatusName(this.wscObj.status);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Rst", "Reset connection", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Host", "Host", "Host address", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Port", "Port", "Port ID", GH_ParamAccess.item, "2000");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Websocket Objects", "WSC", "This object provides access to the connection. Connect this output to WS input websocket Send/Recv components.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            this.subscribeToEvents();

            string initMsg = "init";
            string host = "127.0.0.1";
            string port = "2000";
            bool reset = false;

            DA.GetData(0, ref reset);
            DA.GetData(1, ref host);
            DA.GetData(2, ref port);

            string address = "ws://" + host + ":" + port;

            if (!this.wsAddress.isSameAs(address) || reset)
            {
                ////this.disconnect();

                this.wsAddress.setAddress(address);

                if (this.wsAddress.isValid())
                {
                    this.wscObj = new WsObject().init(address, initMsg);
                    this.Message = "Connecting";
                    this.wscObj.changed += this.wsObjectOnChange;
                }
                else
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid address");
                }
            }

            DA.SetData(0, this.wscObj);
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
            get { return new Guid("23D4555A-96EF-454B-B33D-691396240F4C"); }
        }
    }
}