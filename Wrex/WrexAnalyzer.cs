using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wrex
{
    class WrexAnalyzer
    {
        public class Summary
        {
            public TimeSpan TotalTimeTaken { get; set; }
            public TimeSpan FastestTime { get; set; }
            public TimeSpan SlowestTime { get; set; }
            public TimeSpan AverageTime { get; set; }
            public int RequestsPerSecond { get; set; }

            public long TotalDataReceivedInBytes { get; set; }
            public string SampleResponse { get; set; }
        }


        private readonly Wrex wrex;
        public WrexAnalyzer(Wrex wrex)
        {
            this.wrex = wrex;
        }

        public Summary GetSummary()
        {
            var summary = new Summary();

            summary.TotalTimeTaken = wrex.TotalTimeTaken;
            summary.SampleResponse = wrex.SampleResponse;
            summary.FastestTime = TimeSpan.FromTicks(wrex.Results.Min(x => x.TimeTaken.Ticks));
            summary.SlowestTime = TimeSpan.FromTicks(wrex.Results.Max(x => x.TimeTaken.Ticks));
            summary.AverageTime = TimeSpan.FromTicks((long)wrex.Results.Average(x => x.TimeTaken.Ticks));
            summary.RequestsPerSecond = (int)(wrex.Options.NumberOfRequests / ((double)wrex.TotalTimeTaken.Ticks / TimeSpan.TicksPerSecond));

            return summary;
        }

        public Dictionary<int, int> GetStatusDistribution()
        {
            return null;
        }

        public Dictionary<int, int> GetReponseTimeDistribution()
        {
            return null;
        }
    }
}
