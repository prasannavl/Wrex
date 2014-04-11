// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 3:47 PM 08-04-2014

namespace Wrex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;

    public class WrexAnalyzer
    {
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
            summary.RequestsPerSecond =
                (int)(wrex.Options.NumberOfRequests / ((double)wrex.TotalTimeTaken.Ticks / TimeSpan.TicksPerSecond));

            summary.TotalDataReceivedInBytes = wrex.TotalTransferedBytes;

            return summary;
        }

        public IEnumerable<StatusCodeDistribution> GetStatusDistribution()
        {
            var distribution =
                wrex.Results.GroupBy(x => x.StatusCode)
                    .Select(x => new StatusCodeDistribution { Status = x.Key, ResponseCount = x.Count() })
                    .OrderByDescending(x => x.ResponseCount);

            return distribution;
        }

        public IEnumerable<ResponseTimeDistribution> GetReponseTimeDistribution(Summary summary = null)
        {
            TimeSpan fastest;
            TimeSpan slowest;

            if (summary != null)
            {
                fastest = summary.FastestTime;
                slowest = summary.SlowestTime;
            }
            else
            {
                fastest = wrex.Results.Min(x => x.TimeTaken);
                slowest = wrex.Results.Max(x => x.TimeTaken);
            }

            const int NoOfDivisions = 10;

            var variance = (slowest.Ticks - fastest.Ticks) / (float)NoOfDivisions;

            var millisCeiling = new TimeSpan[NoOfDivisions + 1];

            for (int i = 0; i < NoOfDivisions; i++)
            {
                millisCeiling[i] = TimeSpan.FromTicks((long)(fastest.Ticks + (variance * i)));
            }

            millisCeiling[NoOfDivisions] = slowest;

            var distribution =
                wrex.Results.GroupBy(x => millisCeiling.FirstOrDefault(y => y >= x.TimeTaken))
                    .Select(x => new ResponseTimeDistribution { TimeSpan = x.Key, ResponseCount = x.Count() })
                    .OrderBy(x => x.TimeSpan);

            return distribution;
        }

        public class ResponseTimeDistribution
        {
            public TimeSpan TimeSpan { get; set; }
            public int ResponseCount { get; set; }
        }

        public class StatusCodeDistribution
        {
            public HttpStatusCode Status { get; set; }
            public int ResponseCount { get; set; }
        }

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
    }
}