using System;
using System.Collections.Generic;
using System.Net;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;
using System.Windows.Forms;
using Grasshopper;
using FDMremote.Bengesht;

namespace FDMremote.GH_Optimization
{
    public class FDMstart : GH_Component
    {
        private WsObject wscObj;
        private bool isSubscribedToEvents;
        private GH_Document ghDocument;
        private WsAddress wsAddress;

        /// <summary>
        /// Initializes a new instance of the WsStart class.
        /// </summary>
        public FDMstart()
          : base("Remote Start", "FDMstart",
              "Start client-server connection; Bengesht design, Icon by ookijasa (Noun Project)",
              "FDMremote", "Optimization")
        {
            isSubscribedToEvents = false;
            wsAddress = new WsAddress("");
        }

        ~FDMstart()
        {
            disconnect();
        }

        private void disconnect()
        {
            if (wscObj != null)
            {
                try { wscObj.disconnect(); }
                catch { }
                wscObj.changed -= wsObjectOnChange;
                wscObj = null;
                wsAddress.setAddress(null);
            }
        }

        private void documentOnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this))
            {
                e.Document.ObjectsDeleted -= documentOnObjectsDeleted;
                disconnect();
            }
        }

        private void documentServerOnDocumentClosed(GH_DocumentServer sender, GH_Document doc)
        {
            if (ghDocument != null && doc.DocumentID == ghDocument.DocumentID)
            {
                disconnect();
            }
        }

        void onObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (Locked)
                disconnect();
        }

        private void subscribeToEvents()
        {
            if (!isSubscribedToEvents)
            {
                ghDocument = OnPingDocument();

                if (ghDocument != null)
                {
                    ghDocument.ObjectsDeleted += documentOnObjectsDeleted;
                    Instances.DocumentServer.DocumentRemoved += documentServerOnDocumentClosed;
                }

                ObjectChanged += onObjectChanged;
                isSubscribedToEvents = true;
            }
        }

        private void wsObjectOnChange(object sender, EventArgs e)
        {
            Message = WsObjectStatus.GetStatusName(wscObj.status);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Rst", "Reset connection", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Host", "Host", "Host address", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Port", "Port", "Port ID", GH_ParamAccess.item, "2000");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Websocket Objects", "WSC", "This object provides access to the connection. Connect this output to WS input websocket Send/Recv components.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            subscribeToEvents();

            string initMsg = "init";
            string host = "127.0.0.1";
            string port = "2000";
            bool reset = false;

            DA.GetData(0, ref reset);
            DA.GetData(1, ref host);
            DA.GetData(2, ref port);

            string address = "ws://" + host + ":" + port;

            if (!wsAddress.isSameAs(address) || reset)
            {
                ////this.disconnect();

                wsAddress.setAddress(address);

                if (wsAddress.isValid())
                {
                    wscObj = new WsObject().init(address, initMsg);
                    Message = "Connecting";
                    wscObj.changed += wsObjectOnChange;
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid address");
                }
            }

            DA.SetData(0, wscObj);
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