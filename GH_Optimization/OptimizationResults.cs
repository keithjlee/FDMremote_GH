using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using FDMremote.Optimization;
using Newtonsoft.Json;

namespace FDMremote.GH_Optimization
{
    public class OptimizationResults : GH_Component
    {
        List<Curve> curves;
        /// <summary>
        /// Initializes a new instance of the OptimizationResults class.
        /// </summary>
        public OptimizationResults()
          : base("OptimizationResults", "Results",
              "Result of optimization",
              "FDMremote", "Optimization")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Input Network", "Network", "Networking being optimized", GH_ParamAccess.item);
            pManager.AddTextParameter("Optimization Data", "Data", "Result sent back from optimization", GH_ParamAccess.item);
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
            pManager.AddTextParameter("Data", "Data", "Optimization data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // initialize
            Network network = new Network();
            String msg = "";
            Receiver receiver = new Receiver();

            // assign
            ClearData();
            if (!DA.GetData(0, ref network)) return;
            if (!DA.GetData(1, ref msg)) return;
            if (msg == "CONNECTION ENDED") return;

            // extract indices
            List<int[]> indices = network.Indices;
            // deserialize
            receiver = JsonConvert.DeserializeObject<Receiver>(msg);
            //JsonConvert.PopulateObject(info, receiver);

            //dynamic JSONoutput = JsonConvert.DeserializeObject(info);

            //var finished = JSONoutput["Finished"];
            //var iter = (int)JSONoutput["Iter"];
            //var loss = (double)JSONoutput["Loss"];
            //var q = (List<double>)JSONoutput["Q"];
            //var x = (List<double>)JSONoutput["X"];
            //var y = (List<double>)JSONoutput["Y"];
            //var z = (List<double>)JSONoutput["Z"];
            //var losstrace = (List<double>)JSONoutput["Losstrace"];

            List<double> x = receiver.X;
            List<double> y = receiver.Y;
            List<double> z = receiver.Z;

            // during intermittent analysis
            //if (!receiver.Finished)
            //{
            //    curves = CurveMaker(indices, x, y, z);
            //    DA.SetData(0, receiver.Finished);
            //    DA.SetDataList(2, receiver.Q);
            //    DA.SetData(3, receiver.Loss);
            //    DA.SetData(4, receiver.Iter);
            //    DA.SetData(5, msg);
            //    ExpireSolution(true);
            //}
            //else
            //{
                curves = CurveMaker(indices, x, y, z);
                DA.SetData(0, receiver.Finished);
                DA.SetDataList(2, receiver.Q);
                DA.SetData(3, receiver.Loss);
                DA.SetData(4, receiver.Iter);
                DA.SetData(5, msg);

                Network newnetwork = new Network(network.Anchors, curves, receiver.Q, network.Tolerance);
                DA.SetData(1, newnetwork);
                ExpireSolution(true);
            //}
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
            get { return new Guid("C000D409-6FE3-47A3-B808-483A961332D3"); }
        }
    }
}