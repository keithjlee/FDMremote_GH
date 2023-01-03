using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using FDMremote.Utilities;
using Rhino.Collections;
using Rhino.Render.ChangeQueue;
using System.Linq;
using Grasshopper.GUI.Gradient;
using System.Runtime.InteropServices;
using FDMremote.Properties;

namespace FDMremote.GH_Materialization
{
    public class MakeHelper : GH_Component
    {
        //Inputs
        double E;
        double A;
        Network network;
        int radius; //node radius
        bool nodes; //show nodes
        int thickness; //line thickness
        bool show; //show all drawing
        bool showStraight; //show straight segments
        double straightSpacing; //spacing of straight segments
        bool showProjection; //show projection drawing
        bool showPairs; //show curve-curve pairs
        int textSize; //text size
        bool showTags; //show text size
        Vector3d straightOffset;
        Vector3d projectionOffset;
        int indexer;

        //Visualization
        System.Drawing.Color F0;
        System.Drawing.Color F1;
        System.Drawing.Color N0;
        System.Drawing.Color N1;
        System.Drawing.Color pairColour;
        System.Drawing.Color textColour;
        GH_Gradient Fgradient;
        GH_Gradient Ngradient;

        //Derived values
        Point3d pbl;
        BoundingBox bb;
        Vector3d straightStart;
        Vector3d projectionStart;
        List<Point3d> points;
        List<Point3d> straightPoints;
        List<System.Drawing.Color> straightPointColors;
        List<string> straightPointIDs;
        List<Point3d> projectionPoints;
        List<System.Drawing.Color> colors;
        List<string> pointIDs;
        Line[] edgeLines;
        Line[] flatLines;
        Line[] straightLines;
        Line[] pairsFlat;
        Line[] pairsStraight;
        List<double> unstressedLengths;
        List<string> unstressedValues;
        List<(string, string)> edgeIDs;
        List<(System.Drawing.Color, System.Drawing.Color)> edgecolors;

        //default colours
        readonly System.Drawing.Color darkblue = System.Drawing.Color.FromArgb(3, 0, 198);
        readonly System.Drawing.Color lightgray = System.Drawing.Color.FromArgb(128, 128, 128);
        readonly System.Drawing.Color magenta = System.Drawing.Color.FromArgb(237, 30, 121);

        readonly System.Drawing.Color b1 = System.Drawing.Color.FromArgb(0, 79, 235);
        readonly System.Drawing.Color b2 = System.Drawing.Color.FromArgb(140, 237, 235);
        readonly System.Drawing.Color o1 = System.Drawing.Color.FromArgb(255, 101, 59);
        readonly System.Drawing.Color o2 = System.Drawing.Color.FromArgb(255, 255, 59);

        Vector3d textoffset;

        /// <summary>
        /// Initializes a new instance of the MakeHelper class.
        /// </summary>
        public MakeHelper()
          : base("MakeHelper", "Make",
              "Utility for materializing a FDM network",
              "FDMremote", "Experimental")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        /// 
        //bool offsetProjection; //offset of projection drawing
        //bool showProjection; //show projection drawing
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Show", "Show", "Show make information", GH_ParamAccess.item, true);
            pManager.AddGenericParameter("Network", "Network", "Network to materialize", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaterialStiffness", "E", "Young's Modulus of edge (VERIFY YOUR UNITS)", GH_ParamAccess.item, 100);
            pManager.AddNumberParameter("Area", "A", "Cross-sectional area of edge VERIFY YOUR UNITS)", GH_ParamAccess.item, 10);
            pManager.AddBooleanParameter("ShowNodes", "Nodes", "Show node spheres", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("NodeRadius", "Radius", "Radius of node spheres", GH_ParamAccess.item, 5);
            pManager.AddColourParameter("FixedColour1", "F0", "Gradient extrema 1 for fixed nodes", GH_ParamAccess.item, b1);
            pManager.AddColourParameter("FixedColour2", "F1", "Gradient extrema 2 for fixed nodes", GH_ParamAccess.item, b2);
            pManager.AddColourParameter("FreeColour1", "N0", "Gradient extrema 1 for free nodes", GH_ParamAccess.item, o1);
            pManager.AddColourParameter("FreeColour2", "N1", "Gradient extrema 2 for free nodes", GH_ParamAccess.item, o2);
            pManager.AddIntegerParameter("EdgeThickness", "Thickness", "Thickness of edge lines", GH_ParamAccess.item, 5);
            //pManager.AddVectorParameter("StraightOffset", "StraightOffset", "Offset of drawn straight edges", GH_ParamAccess.item) ;
            pManager.AddNumberParameter("StraightSpacing", "Spacing", "Spacing of straight edges", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("ShowStraight", "Straight", "Show straight unstressed edges", GH_ParamAccess.item, true);
            //pManager.AddVectorParameter("ProjectionOffset", "ProjOffset", "Offset of projection drawing", GH_ParamAccess.item);
            pManager.AddBooleanParameter("ShowProjection", "Projection", "Show projection drawing", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("ShowPairs", "Pairs", "Show curve-curve pairs", GH_ParamAccess.item, false);
            pManager.AddColourParameter("PairColour", "Cpair", "Colour of pair lines", GH_ParamAccess.item, System.Drawing.Color.Gray);
            pManager.AddIntegerParameter("TextSize", "TextSize", "Size of text tags", GH_ParamAccess.item, 3);
            pManager.AddBooleanParameter("ShowText", "Text", "Show text tags", GH_ParamAccess.item, true);
            pManager.AddColourParameter("TextColour", "Ctext", "Colour of tags", GH_ParamAccess.item, System.Drawing.Color.Black);
            pManager.AddVectorParameter("StraightOffset", "StraightOffset", "Offset of straight edges", GH_ParamAccess.item, new Vector3d(0, 0, 0));
            pManager.AddVectorParameter("ProjectionOffset", "ProjOffset", "Offset of projection edges", GH_ParamAccess.item, new Vector3d(0, 0, 0));
            pManager.AddNumberParameter("TextOffset", "TextOffset", "Offset of text tags in Z-direction",
                GH_ParamAccess.item, 0.5);
            pManager.AddIntegerParameter("PairIndexer", "Indexer", "Isolate element pair information", GH_ParamAccess.item, -1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("ProjectedCurves", "Projection", "Projected edges", GH_ParamAccess.list);
            pManager.AddLineParameter("ProjectedPairs", "ProjPairs", "Pair lines of projected edges", GH_ParamAccess.list);
            pManager.AddLineParameter("StraightCurves", "Straight", "Straight unstressed edges", GH_ParamAccess.list);
            pManager.AddLineParameter("StraightPairs", "StraightPairs", "Pair lines of straight unstressed edges", GH_ParamAccess.list);

            pManager.HideParameter(0);
            pManager.HideParameter(1);
            pManager.HideParameter(2);
            pManager.HideParameter(3);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //initialize
            //network = new Network();
            double offsetval = 0;
            //assign
            DA.GetData(0, ref show);
            if (!DA.GetData(1, ref network)) return;
            DA.GetData(2, ref E);
            DA.GetData(3, ref A);
            DA.GetData(4, ref nodes);
            DA.GetData(5, ref radius);
            DA.GetData(6, ref F0);
            DA.GetData(7, ref F1);
            DA.GetData(8, ref N0);
            DA.GetData(9, ref N1);
            DA.GetData(10, ref thickness);
            DA.GetData(11, ref straightSpacing);
            DA.GetData(12, ref showStraight);
            DA.GetData(13, ref showProjection);
            DA.GetData(14, ref showPairs);
            DA.GetData(15, ref pairColour);
            DA.GetData(16, ref textSize);
            DA.GetData(17, ref showTags);
            DA.GetData(18, ref textColour);
            DA.GetData(19, ref straightOffset);
            DA.GetData(20, ref projectionOffset);
            DA.GetData(21, ref offsetval);

            DA.GetData(22, ref indexer);

            //make sure indices are out of bounds
            if (indexer > network.Curves.Count - 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Bounds of indexer exceeds number of elements in network");
            }

            

            textoffset = Vector3d.ZAxis * offsetval;

            MakeGradients(); //make colour gradients
            GetBB(); //get bounds information
            GetIDs(); //get tag information
            GetLengths(); //get length information
            GetPairs(); //get curve curve pairs

            DA.SetDataList(0, flatLines);
            DA.SetDataList(1, pairsFlat);
            DA.SetDataList(2, straightLines);
            DA.SetDataList(3, pairsStraight);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (show)
            {
                //args.Display.DrawPoint(pbl, Rhino.Display.PointStyle.RoundSimple, radius, System.Drawing.Color.GreenYellow);
                //args.Display.DrawPoint(pbl + straightStart, Rhino.Display.PointStyle.RoundSimple, radius, System.Drawing.Color.GreenYellow);

                //args.Display.DrawBrepWires(bb.ToBrep(), magenta);

                //draw edges
                for (int i = 0; i < edgeLines.Length; i++)
                {
                    var stops = new Rhino.Display.ColorStop[2];
                    stops[0] = new Rhino.Display.ColorStop(edgecolors[i].Item1, 0);
                    stops[1] = new Rhino.Display.ColorStop(edgecolors[i].Item2, 1);

                    args.Display.DrawGradientLines(new Line[] { edgeLines[i] }, thickness, stops, edgeLines[i].From, edgeLines[i].To, true, 1);

                }

                if (showTags)
                {
                    for (int i = 0; i < edgeLines.Length; i++)
                    {
                        //string text = unstressedValues[i];
                        string text = "E" + i.ToString();
                        Plane pl;
                        args.Viewport.GetFrustumFarPlane(out pl);

                        pl.Origin = edgeLines[i].PointAt(0.5);

                        args.Display.Draw3dText(text,
                            textColour,
                            pl,
                            textSize,
                            "Arial",
                            false,
                            false,
                            Rhino.DocObjects.TextHorizontalAlignment.Center,
                            Rhino.DocObjects.TextVerticalAlignment.Middle);
                    }

                    for (int i = 0; i < points.Count; i++)
                    {
                        string text = pointIDs[i];
                        Plane pl;
                        args.Viewport.GetFrustumFarPlane(out pl);
                        pl.Origin = points[i];

                        args.Display.Draw3dText(text,
                            textColour,
                            pl,
                            textSize,
                            "Arial",
                            false,
                            false,
                            Rhino.DocObjects.TextHorizontalAlignment.Center,
                            Rhino.DocObjects.TextVerticalAlignment.Middle);
                    }

                }

                if (nodes)
                {
                    //draw nodes
                    for (int i = 0; i < points.Count; i++)
                    {
                        args.Display.DrawPoint(points[i], Rhino.Display.PointStyle.RoundSimple, radius, colors[i]);
                    }

                }


                //draw projection
                if (showProjection)
                {
                    for (int i = 0; i < flatLines.Length; i++)
                    {
                        var stops = new Rhino.Display.ColorStop[2];
                        stops[0] = new Rhino.Display.ColorStop(edgecolors[i].Item1, 0);
                        stops[1] = new Rhino.Display.ColorStop(edgecolors[i].Item2, 1);

                        args.Display.DrawGradientLines(new Line[] { flatLines[i] }, thickness, stops, flatLines[i].From, flatLines[i].To, true, 1);

                    }

                    if (showPairs)
                    {
                        if (indexer == -1)
                        {
                            args.Display.DrawLines(pairsFlat, pairColour, thickness / 2);
                        }
                        else
                        {
                            var pair = pairsFlat[indexer];
                            args.Display.DrawLine(pair, pairColour, thickness / 2);

                            if (!showStraight)
                            {
                                string eid = indexer.ToString();
                                string startnode = edgeIDs[indexer].Item1;
                                string endnode = edgeIDs[indexer].Item2;
                                string initlength = unstressedValues[indexer];

                                string info = $@"Edge {eid}:
Cut to L = {initlength}
Connect from Node {startnode}
to Node {endnode}";
                                Point2d tag = new Point2d(10, args.Viewport.Bounds.Height / 2);

                                args.Display.Draw2dText(info,
                                    pairColour,
                                    tag,
                                    false,
                                    25);
                            }
                            
                        }
                    }

                    if (showTags)
                    {
                        for (int i = 0; i < flatLines.Length; i++)
                        {
                            //string text = unstressedValues[i];
                            string text = "E" + i.ToString();
                            Plane pl = Plane.WorldXY;
                            pl.Origin = flatLines[i].PointAt(0.5) + textoffset;

                            args.Display.Draw3dText(text,
                                textColour,
                                pl,
                                textSize,
                                "Arial",
                                false,
                                false,
                                Rhino.DocObjects.TextHorizontalAlignment.Center,
                                Rhino.DocObjects.TextVerticalAlignment.Middle);
                        }

                        for (int i = 0; i < projectionPoints.Count; i++)
                        {
                            string text = pointIDs[i];
                            Plane pl = Plane.WorldXY;

                            pl.Origin = projectionPoints[i] + textoffset;

                            args.Display.Draw3dText(text,
                                textColour,
                                pl,
                                textSize,
                                "Arial",
                                false,
                                false,
                                Rhino.DocObjects.TextHorizontalAlignment.Center,
                                Rhino.DocObjects.TextVerticalAlignment.Middle);
                        }
                    }

                    if (nodes)
                    {
                        //draw nodes
                        for (int i = 0; i < projectionPoints.Count; i++)
                        {
                           args.Display.DrawPoint(projectionPoints[i], Rhino.Display.PointStyle.RoundSimple, radius, colors[i]);
                        }

                    }

                }

                //draw straight
                if (showStraight)
                {
                    for (int i = 0; i < straightLines.Length; i++)
                    {
                        var stops = new Rhino.Display.ColorStop[2];
                        stops[0] = new Rhino.Display.ColorStop(edgecolors[i].Item1, 0);
                        stops[1] = new Rhino.Display.ColorStop(edgecolors[i].Item2, 1);

                        args.Display.DrawGradientLines(new Line[] { straightLines[i] }, thickness, stops, straightLines[i].From, straightLines[i].To, true, 1);
                    }

                    if (showPairs)
                    {
                        //args.Display.DrawLines(pairsStraight, pairColour, thickness / 2);
                        if (indexer == -1)
                        {
                            args.Display.DrawLines(pairsStraight, pairColour, thickness / 2);
                        }
                        else
                        {
                            //int index = indexer[i];
                            var pair = pairsStraight[indexer];
                            args.Display.DrawLine(pair, pairColour, thickness / 2);

                            string eid = indexer.ToString();
                            string startnode = edgeIDs[indexer].Item1;
                            string endnode = edgeIDs[indexer].Item2;
                            string initlength = unstressedValues[indexer];

                            string info = $@"Edge {eid}:
Cut to L = {initlength}
Connect from Node {startnode}
to Node {endnode}";
                            Point2d tag = new Point2d(10, args.Viewport.Bounds.Height/2);

                            args.Display.Draw2dText(info,
                                pairColour,
                                tag,
                                false,
                                25);
                        }
                    }

                    if (showTags)
                    {

                        for (int i = 0; i < straightLines.Length; i++)
                        {
                            string text = unstressedValues[i];
                            Plane pl = Plane.WorldXY;
                            pl.Origin = straightLines[i].PointAt(0.5) + textoffset;



                            args.Display.Draw3dText(text,
                                textColour,
                                pl,
                                textSize,
                                "Arial",
                                false,
                                false,
                                Rhino.DocObjects.TextHorizontalAlignment.Center,
                                Rhino.DocObjects.TextVerticalAlignment.Middle);
                        }

                        for (int i = 0; i < straightPoints.Count; i++)
                        {
                            string text = straightPointIDs[i];
                            Plane pl = Plane.WorldXY;

                            pl.Origin = straightPoints[i] + textoffset;

                            if (i % 2 == 0)
                            {
                                pl.Origin += new Vector3d(0, -textSize, 0);
                            }
                            else
                            {
                                pl.Origin += new Vector3d(0, textSize, 0);
                            }

                            args.Display.Draw3dText(text,
                                textColour,
                                pl,
                                textSize,
                                "Arial",
                                false,
                                false,
                                Rhino.DocObjects.TextHorizontalAlignment.Center,
                                Rhino.DocObjects.TextVerticalAlignment.Middle);
                        }
                    }

                    if (nodes)
                    {
                        //draw nodes
                        for (int i = 0; i < straightPoints.Count; i++)
                        {
                            args.Display.DrawPoint(straightPoints[i], Rhino.Display.PointStyle.RoundSimple, radius, straightPointColors[i]);
                        }

                    }
                }
                
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                BoundingBox b = new BoundingBox();

                foreach (Line line in edgeLines) b.Union(line.BoundingBox);
                foreach (Line line in flatLines) b.Union(line.BoundingBox);
                foreach (Line line in straightLines) b.Union(line.BoundingBox);

                return b;
            }
        }

        /// <summary>
        /// Make colour gradients
        /// </summary>
        private void MakeGradients()
        {
            Fgradient = new GH_Gradient();
            Fgradient.AddGrip(0, F0);
            Fgradient.AddGrip(network.F.Count, F1);

            Ngradient = new GH_Gradient();
            Ngradient.AddGrip(0, N0);
            Ngradient.AddGrip(network.N.Count, N1);

        }

        /// <summary>
        /// get bounding box
        /// </summary>
        private void GetBB()
        {
            bb = new BoundingBox(network.Points);

            //foreach (Curve curve in network.Curves)
            //{
            //    bb.Union(curve.GetBoundingBox(true));
            //}


            //get total height of geometry

            pbl = bb.Corner(true, true, true);
            var pbr = bb.Corner(false, true, true);
            var ptl = bb.Corner(true, false, true);
            var pzl = bb.Corner(true, true, false);

            Vector3d width = pbr - pbl;

            straightStart = (ptl - pbl) * 1.1 + width/2;

            Vector3d xoffset = new Vector3d(-network.Ne * straightSpacing / 2, 0, 0);
            straightStart += xoffset + straightOffset;


            //projectionStart = (pbl - ptl) * 1.1;
            projectionStart = (pbl - pzl) / 10 + projectionOffset;
        }

        /// <summary>
        /// Get tags for nodes and elements
        /// </summary>
        private void GetIDs()
        {
            //ids
            points = new List<Point3d>();
            projectionPoints= new List<Point3d>();
            //projectionPointColors = new List<System.Drawing.Color>();
            colors = new List<System.Drawing.Color>();
            pointIDs = new List<string>();
            edgeIDs = new List<(string, string)>();
            edgecolors = new List<(System.Drawing.Color, System.Drawing.Color)>();
            edgeLines = new Line[network.Ne];
            flatLines = new Line[network.Ne];

            //generate blank list of point IDs
            for (int i = 0; i < network.Points.Count; i++)
            {
                points.Add(network.Points[i]);
                pointIDs.Add("");
                colors.Add(System.Drawing.Color.Black);

                Point3d projpoint = new Point3d(network.Points[i]);
                projpoint.Z = pbl.Z;
                projpoint += projectionStart;
                projectionPoints.Add(projpoint);
            }

            //updated fixed points
            for (int i = 0; i < network.F.Count; i++)
            {
                int index = network.F[i];

                pointIDs[index] += "F" + i.ToString();
                colors[index] = Fgradient.ColourAt(i);
            }

            //update free points
            for (int i = 0; i < network.N.Count; i++)
            {
                int index = network.N[i];

                pointIDs[index] += "N" + i.ToString();
                colors[index] = Ngradient.ColourAt(i);
            }

            //create edge information
            for (int i = 0; i < network.Ne; i++)
            {
                Curve edge = network.Curves[i];

                int[] indices = network.Indices[i];
                int istart = indices[0];
                int iend = indices[1];

                edgeIDs.Add((pointIDs[istart], pointIDs[iend]));
                edgecolors.Add((colors[istart], colors[iend]));


                Point3d startpoint = points[istart];
                //var startcol = colors[istart];
                Point3d startpointflat = new Point3d(startpoint.X, startpoint.Y, pbl.Z) + projectionStart;
                Point3d endpoint = points[iend];
                //var endcol = colors[iend];
                Point3d endpointflat = new Point3d(endpoint.X, endpoint.Y, pbl.Z) + projectionStart;

                edgeLines[i] = new Line(startpoint, endpoint);
                flatLines[i] = new Line(startpointflat, endpointflat);
                //projectionPoints.Add(startpointflat);
                //projectionPointColors.Add(startcol);
                //projectionPoints.Add(endpointflat);
                //projectionPointColors.Add(endcol);
            }
        }

        private void GetPairs()
        {
            pairsFlat = new Line[network.Ne];
            pairsStraight = new Line[network.Ne];

            for (int i = 0; i < network.Ne; i++)
            {
                pairsFlat[i] = new Line(edgeLines[i].PointAt(0.5), flatLines[i].PointAt(0.5));
                pairsStraight[i] = new Line(edgeLines[i].PointAt(0.5), straightLines[i].PointAt(0.5));
            }
        }

        /// <summary>
        /// Get the unstressed lengths of each edge
        /// </summary>
        private void GetLengths()
        {
            straightPoints = new List<Point3d>();
            straightPointColors = new List<System.Drawing.Color>();
            straightPointIDs = new List<string>();
            unstressedLengths = new List<double>();
            unstressedValues = new List<string>();
            straightLines = new Line[network.Ne];

            for (int i = 0; i < network.Ne; i++)
            {
                double q = network.ForceDensities[i];
                Curve edge = network.Curves[i];

                double Lf = edge.GetLength();

                double Lo = (Lf * E * A) / (q * Lf + E * A);
                unstressedLengths.Add(Lo);

                var indices = network.Indices[i];

                int i1 = indices[0];
                int i2 = indices[1];

                var c1 = colors[i1];
                var c2 = colors[i2];

                straightPointColors.Add(c1);
                straightPointColors.Add(c2);

                string id1 = pointIDs[i1];
                string id2 = pointIDs[i2];

                straightPointIDs.Add(id1);
                straightPointIDs.Add(id2);

                Vector3d rightshift = new Vector3d(straightSpacing * i, 0, 0);

                Point3d startpoint = pbl + straightStart + rightshift;
                Point3d endpoint = startpoint + Vector3d.YAxis * Lo;

                straightPoints.Add(startpoint);
                straightPoints.Add(endpoint);

                straightLines[i] = new Line(startpoint, endpoint);
                string lengthval = Lo.ToString("N1");
                unstressedValues.Add(lengthval);
            }

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.Maker;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("B6DA33F2-A908-45B9-8A03-8E31216DB08A"); }
        }
    }
}