using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using FDMremote.Analysis;
using Newtonsoft.Json;
using WebSocketSharp;
using System.Threading;

namespace FDMremote.GH_Optimization
{
    public class Optimize : GH_Component
    {
        Network inputnetwork = new Network();
        private string _optiminfo = "";
        private string _status = "";
        private readonly object _lock = new object();
        private WebSocket ws;
        public event EventHandler changed;
        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
        /// 

        public Optimize()
          : base("OptimizeRemote", "Optimize",
              "Use FDMremote.jl to optimize the network",
              "FDMremote", "Optimization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("FDM Network", "Network", "Network to Optimize", GH_ParamAccess.item); // Network
            pManager.AddGenericParameter("Optimization Parameters", "Params", "Objective functions, tolerances, etc.", GH_ParamAccess.item); // all other parameters
            pManager.AddVectorParameter("Load", "P", "Applied load", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Connect", "Connect", "Open/Close connection", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Host", "Host", "Host address", GH_ParamAccess.item, "127.0.0.1");
            pManager.AddTextParameter("Port", "Port", "Port ID", GH_ParamAccess.item, "2000");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Data", "Data", "Optimization data", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "Status", "Status", GH_ParamAccess.item);
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
            bool live = true;

            
            if (!DA.GetData(0, ref inputnetwork)) return;
            DA.GetData(1, ref objparams);
            if (!DA.GetDataList(2, loads)) return;
            if (!DA.GetData(3, ref live)) return;

            //connection info
            string host = "";
            string port = "";

            DA.GetData(4, ref host);
            DA.GetData(5, ref port);

            string address = "ws://" + host + ":" + port;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != inputnetwork.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            //make problem

            OptimizationProblem optimprob = new OptimizationProblem(inputnetwork, objparams, loads);

            //just testing
            string data = JsonConvert.SerializeObject(optimprob);

            //update output data
            //lock (_lock)
            //{
                DA.SetData(0, _optiminfo);
                DA.SetData(1, _status);
            //}

            //initialize
            ws = new WebSocket(address);
            ws.WaitTime = new TimeSpan(0, 0, 2);
            ws.OnMessage += Ws_OnMessage;
            ws.OnOpen += Ws_OnOpen;
            ws.OnClose += Ws_OnClose;
            ws.Connect();

            //Send data to server
            if (!live)
            {
                ws.Send(data);
            }
        }
        
        private void Ws_OnClose(object sender, CloseEventArgs e)
        {
            onChanged();
        }

        private void Ws_OnOpen(object sender, EventArgs e)
        {
            onChanged();
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            lock (_lock)
            {
                //ClearData();
                 _optiminfo = e.Data;

                var receiver = JsonConvert.DeserializeObject<Receiver>(e.Data);
                if (receiver.Finished)
                {
                    _status = "FINISHED";
                    ws.Close();
                    ws.Connect();
                } 
                else _status = "ONGOING";

            }
        }

        protected virtual void onChanged()
        {
            EventHandler handler = changed;
            if (handler != null) handler(this, EventArgs.Empty);
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