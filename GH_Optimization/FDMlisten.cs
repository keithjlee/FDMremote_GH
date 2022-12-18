using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Bengesht;
using FDMremote.Utilities;
using FDMremote.Analysis;
using FDMremote.Optimization;
using Newtonsoft.Json;

namespace FDMremote.GH_Optimization
{
    /// <summary>
    /// The main functionality is from Bengesht by Behrooz Tahanzadeh
    /// Modified to directly parse message into FDMremote objects
    /// </summary>
    public class FDMlisten : GH_Component
    {
        //network
        private WsObject wscObj;
        private bool onMessageTriggered;
        private GH_Document ghDocument;
        private bool isAutoUpdate;
        private bool isAskingNewSolution;
        private List<string> buffer = new List<string>();

        //network info
        List<int[]> indices;
        List<Point3d> anchors;
        double tolerance;

        //data
        Network network;
        Network solvednetwork;
        bool finished;
        List<double> x;
        List<double> y;
        List<double> z;
        List<Curve> curves;
        List<double> q;
        double loss;
        int iter;
        List<double> trace;

        private void Initialize()
        {
            network = new Network();
            solvednetwork = new Network();
            finished = false;
            x = new List<double>();
            y = new List<double>();
            z = new List<double>();
            curves = new List<Curve>();
            q = new List<double>();
            iter = 0;
            trace = new List<double>();

            indices = new List<int[]>();
            anchors = new List<Point3d>();
            tolerance = 0.1;
        }

        /// <summary>
        /// Initializes a new instance of the Optimizereceive class.
        /// </summary>
        public FDMlisten()
          : base("Remote Listen", "FDMlisten",
              "Receive data from server; Bengesht design",
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
            pManager.AddGenericParameter("Anaylzed Network", "Network", "Used to topologize and generate solved network", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "Data", "Network information in JSON format", GH_ParamAccess.item);
            pManager.AddGenericParameter("Network Status", "Sts", "status", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Optimization Finished", "Finished", "State of optimization", GH_ParamAccess.item);
            pManager.AddNumberParameter("Force Densities", "q", "Current solved force densities", GH_ParamAccess.list);
            pManager.AddNumberParameter("Loss", "f(q)", "Current objective function value", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Iteration", "Iter", "Current number of optimization iterations", GH_ParamAccess.item) ;
            pManager.AddNumberParameter("Loss Trace", "f(q(t))", "History of objective function value", GH_ParamAccess.list) ;
            pManager.AddGenericParameter("Solved network", "Network", "Current solution network", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(1, ref this.isAutoUpdate);

            Initialize(); //initialize all persistent fields

            if (!DA.GetData(2, ref network)) return; //create network
            indices = network.Indices;
            anchors = network.Anchors;
            tolerance = network.Tolerance;

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

            //assign data
            DA.SetData(0, this.wscObj.message);

            DA.SetData(1, WsObjectStatus.GetStatusName(this.wscObj.status));
            this.onMessageTriggered = false;

            ParseMsg(this.wscObj.message);

            DA.SetData(2, finished);
            DA.SetDataList(3, q);
            DA.SetData(4, loss);
            DA.SetData(5, iter);
            DA.SetDataList(6, trace);
            DA.SetData(7, solvednetwork);
        }

        private void ParseMsg(string msg)
        {
            var receiver = JsonConvert.DeserializeObject<Receiver>(msg);
            if (receiver == null) return;

            //update
            finished = receiver.Finished;
            q = receiver.Q;
            loss = receiver.Loss;
            iter = receiver.Iter;
            trace = receiver.Losstrace;
            x = receiver.X;
            y = receiver.Y;
            z = receiver.Z;
            curves = CurveMaker(indices, x, y, z);

            solvednetwork = new Network(anchors, curves, q, tolerance);
        }

        private List<Curve> CurveMaker(List<int[]> indices, List<double> x, List<double> y, List<double> z)
        {
            List<Curve> curves = new List<Curve>();

            List<Point3d> points = new List<Point3d>();
            //make points
            for (int i = 0; i < x.Count; i++)
            {
                points.Add(new Point3d(x[i], y[i], z[i]));
            }

            foreach (int[] index in indices)
            {
                curves.Add(new LineCurve(points[index[0]], points[index[1]]));
            }

            return curves;
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
        protected override System.Drawing.Bitmap Icon => Properties.Resources.listen;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("BD33E8B4-5485-456F-B636-D60B5CC24A24"); }
        }
    }
}