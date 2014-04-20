// Author: Prasanna V. Loganathar
// Project: Wrex
// 
// Copyright 2014 Launchark. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//  
// Created: 4:10 AM 19-04-2014

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