using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FDMremote.Optimization
{

    /// <summary>
    /// Contains all parameters necessary for an optimization run
    /// </summary>
    internal class OBJParameters
    {
        public double LowerBound;
        public double UpperBound;
        public double AbsTolerance;
        public double RelTolerance;
        public List<OBJ> Objectives;
        public bool ShowIterations;
        public int UpdateFrequency;
        public int MaxIterations;

        public OBJParameters()
        {

        }

        public OBJParameters(double lb, double ub, double abstol, double reltol, List<OBJ> objs, bool showiter, int updatefreq, int maxiters)
        {
            LowerBound = lb;
            UpperBound = ub;
            AbsTolerance = abstol;
            RelTolerance = reltol;
            Objectives = objs;
            ShowIterations = showiter;
            UpdateFrequency = updatefreq;
            MaxIterations = maxiters;
        }

        public OBJParameters(double lb, double ub, double abstol, double reltol, OBJ objs, bool showiter, int updatefreq, int maxiters)
        {
            LowerBound = lb;
            UpperBound = ub;
            AbsTolerance = abstol;
            RelTolerance = reltol;
            ShowIterations = showiter;
            UpdateFrequency = updatefreq;

            List<OBJ> objectiveList = new List<OBJ> { objs };
            Objectives = objectiveList;

            MaxIterations = maxiters;
        }


    }
}
