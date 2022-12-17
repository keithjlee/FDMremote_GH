using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using FDMremote.Analysis;
using Newtonsoft.Json;
using WebSocketSharp;
using FDMremote.Bengesht;

namespace FDMremote.GH_Optimization
{
    public class Optimize : GH_Component
    {
        bool Finished = false;
        int Iter = 0;
        double Loss = 0.0;
        List<double> Q = new List<double>();
        List<Curve> Curves = new List<Curve>();
        Network network = new Network();
        Network outNetwork = new Network();
        string optiminfo = "";


        //bengesht
        private WsObject wscObj;
        private bool isSubscribedToEvents;
        private GH_Document ghDocument;
        private WsAddress wsAddress;



        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
        public Optimize()
          : base("OptimizeRemote", "Optimize",
              "Use FDMremote.jl to optimize the network",
              "FDMremote", "Optimization")
        {
            //bengesht
            this.isSubscribedToEvents = false;
            this.wsAddress = new WsAddress("");
        }

        //bengesht
        ~Optimize()
        {
            this.disconnect();
        }

        //bengesht
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

        //bengesht
        private void documentOnObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this))
            {
                e.Document.ObjectsDeleted -= documentOnObjectsDeleted;
                this.disconnect();
            }
        }

        //bengesht
        private void documentServerOnDocumentClosed(GH_DocumentServer sender, GH_Document doc)
        {
            if (this.ghDocument != null && doc.DocumentID == this.ghDocument.DocumentID)
            {
                this.disconnect();
            }
        }

        //bengesht
        void onObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (this.Locked)
                this.disconnect();
        }
        //bengesht
        private void subscribeToEvents()
        {
            if (!this.isSubscribedToEvents)
            {
                this.ghDocument = OnPingDocument();

                if (this.ghDocument != null)
                {
                    this.ghDocument.ObjectsDeleted += documentOnObjectsDeleted;
                    GH_InstanceServer.DocumentServer.DocumentRemoved += documentServerOnDocumentClosed;
                }

                this.ObjectChanged += this.onObjectChanged;
                this.isSubscribedToEvents = true;
            }
        }

        //bengesht
        private void wsObjectOnChange(object sender, EventArgs e)
        {
            this.Message = WsObjectStatus.GetStatusName(this.wscObj.status);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Network to Optimize", GH_ParamAccess.item); // Network
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Objective functions, tolerances, etc.", GH_ParamAccess.item); // all other parameters
            pManager.AddVectorParameter("Load", "P", "Applied load", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Reset", "Rst", "Reset connection", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Host", "Host", "Host address", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Port", "Port", "Port ID", GH_ParamAccess.item, "2000");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Optimized", "Complete", "Has the optimization completed?", GH_ParamAccess.item);
            pManager.AddGenericParameter("Optimized Network", "Network", "New optimized network", GH_ParamAccess.item);
            pManager.AddNumberParameter("Current Q", "q", "Optimized force density values", GH_ParamAccess.list);
            pManager.AddNumberParameter("Current Loss", "f(q)", "Final objective function value", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number of Iterations", "n_iter", "Total number of iterations", GH_ParamAccess.item);
            pManager.AddNumberParameter("Loss History", "LossTrace", "Trace of f(q) over optimization duration", GH_ParamAccess.list);
            pManager.AddTextParameter("Data", "Data", "Optimization data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            //Network network = new Network();
            OBJParameters objparams = new OBJParameters(0.1, 100.0, 1e-3, 1e-3, new List<OBJ> { new OBJTarget(1.0)}, true, 10, 500);
            List<Vector3d> loads = new List<Vector3d>();
            bool reset = false;

            if (!DA.GetData(0, ref network)) return;
            DA.GetData(1, ref objparams);
            if (!DA.GetDataList(2, loads)) return;
            if (!DA.GetData(3, ref reset)) return;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != network.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            //MAKING CONNECTION
            string host = "";
            string port = "";
            string initMsg = "init";

            DA.GetData(4, ref host);
            DA.GetData(5, ref port);

            string address = "ws://" + host + ":" + port;

            if (!this.wsAddress.isSameAs(address) || reset)
            {
                this.disconnect();

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

            
            OptimizationProblem optimprob = new OptimizationProblem(network, objparams, loads);
            string data = JsonConvert.SerializeObject(optimprob);

            WsObject wsSender = new WsObject();
            wsSender = this.wscObj;
            wsSender.send(data);

            DA.SetData(0, Finished);
            DA.SetData(1, outNetwork);
            DA.SetDataList(2, Q);
            DA.SetData(3, Loss);
            DA.SetData(4, Iter);
            DA.SetData(5, optiminfo);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Properties.Resources.Optimize;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("99C37348-2761-466F-BD63-2B2381150116"); }
        }
    }
}