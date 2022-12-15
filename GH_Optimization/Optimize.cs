using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using FDMremote.Analysis;
using Newtonsoft.Json;
using WebSocketSharp;

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
        /// <summary>
        /// Initializes a new instance of the Optimize class.
        /// </summary>
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
            //pManager.AddGenericParameter("FDM Network", "Network", "Optimized network", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Objective function value", "Loss", "Result of optimization", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Optimized variables", "X", "Design variables of optimized solution", GH_ParamAccess.list);
            //pManager.AddTextParameter("Output", "Out", "TestOutput", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Optimized", "Complete", "Has the optimization completed?", GH_ParamAccess.item);
            pManager.AddGenericParameter("Optimized Network", "Network", "New optimized network", GH_ParamAccess.item);
            pManager.AddNumberParameter("Current Q", "q", "Optimized force density values", GH_ParamAccess.list);
            pManager.AddNumberParameter("Current Loss", "f(q)", "Final objective function value", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Number of Iterations", "n_iter", "Total number of iterations", GH_ParamAccess.item);
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
            bool status = true;

            if (!DA.GetData(0, ref network)) return;
            DA.GetData(1, ref objparams);
            if (!DA.GetDataList(2, loads)) return;
            if (!DA.GetData(3, ref status)) return;

            //connection info
            string host = "";
            string port = "";

            DA.GetData(4, ref host);
            DA.GetData(5, ref port);

            string address = "ws://" + host + ":" + port;

            //check that load count is correct
            if (loads.Count != 1)
            {
                if (loads.Count != network.N.Count)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of loads must be 1 or equal to number of free nodes");
                }
            }

            //make problem
            if (status)
            {
                OptimizationProblem optimprob = new OptimizationProblem(network, objparams, loads);

                //just testing
                string data = JsonConvert.SerializeObject(optimprob);
                //DA.SetData(0, data);

                //loop
                using (WebSocket ws = new WebSocket(address))
                {
                    ws.OnMessage += Ws_OnMessage;
                    ws.Connect();
                    ws.Send(data);
                }
            }
            

            DA.SetData(0, Finished);
            DA.SetData(1, outNetwork);
            DA.SetDataList(2, Q);
            DA.SetData(3, Loss);
            DA.SetData(4, Iter);
            DA.SetData(5, optiminfo);

        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            Receiver receiver = JsonConvert.DeserializeObject<Receiver>(e.Data);
            if (receiver.Finished)
            {
                optiminfo = e.Data;
            }
            MessageAct(receiver);
            this.ExpireSolution(true);
        }

        private void MessageAct(Receiver receiver)
        {
            Finished = receiver.Finished;
            Iter = receiver.Iter;
            Loss = receiver.Loss;
            Q = (List<double>)receiver.Q;

            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < receiver.X.Count; i++)
            {
                points.Add(new Point3d(receiver.X[i], receiver.Y[i], receiver.Z[i]));
            }

            Curves = Solver.NewCurves(network, points);

            if (Finished)
            {
                outNetwork = new Network(network.Anchors, Curves, (List<double>)receiver.Q, network.Tolerance);
            }

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