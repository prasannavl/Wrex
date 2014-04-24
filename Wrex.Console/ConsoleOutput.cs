// Author: Prasanna V. Loganathar
// Project: Wrex.Console
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
// Created: 8:50 PM 10-04-2014

namespace Wrex.Console
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using ConsoleUtils;

    internal class ConsoleOutput
    {
        private FixedWidthInfomativeProgressBar progressBar;

        public async Task StartProgressWriter(Wrex wrexInstance, CancellationToken token)
        {
            const int DelayConstant = 500;
            var firstRun = true;
            while (!token.IsCancellationRequested)
            {
                if (firstRun)
                {
                    firstRun = false;
                }
                else
                {
                    await Task.Delay(DelayConstant);
                }
                WriteProgress(wrexInstance.ExecutedRequests, wrexInstance.Options.NumberOfRequests);
            }

            WriteProgress(wrexInstance.ExecutedRequests, wrexInstance.Options.NumberOfRequests);
            Console.WriteLine();
            Console.WriteLine();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteProgress(int count, int total)
        {
            lock (Console.Out)
            {
                if (progressBar == null)
                {
                    progressBar = new FixedWidthInfomativeProgressBar(total);
                }
                progressBar.UpdateProgress(count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HandleError(Exception ex)
        {
            const string ErrorString = "Error: ";
            lock (Console.Out)
            {
                Console.WriteLine();
                Console.WriteLine();
                ExtendedConsole.WriteErrorLine(ErrorString + ex.Message);
                Console.WriteLine();
                if (ex.GetType() == typeof(AggregateException))
                {
                    var ae = (AggregateException)ex;
                    {
                        ae.Handle(
                            (e) =>
                                {
                                    ExtendedConsole.WriteErrorLine(ErrorString + e.Message);
                                    return true;
                                });
                    }
                }
                Console.WriteLine();
            }
        }

        public void PrintSummary(WrexAnalyzer.Summary summary, Wrex wrexInstance)
        {
            const string Title = "Summary:";
            Console.WriteLine(Title);
            Console.WriteLine(new string('-', Title.Length));
            Console.WriteLine();
            Console.WriteLine("Total time taken: {0}", summary.TotalTimeTaken);
            Console.WriteLine();
            Console.WriteLine("Fastest: {0}", summary.FastestTime);
            Console.WriteLine("Slowest: {0}", summary.SlowestTime);
            Console.WriteLine("Average: {0}", summary.AverageTime);
            Console.WriteLine();
            Console.WriteLine("Requests/second: {0}", summary.RequestsPerSecond);
            Console.WriteLine();
            if (wrexInstance.SampleResponse != null)
            {
                Console.WriteLine("Total number of bytes received: " + wrexInstance.TotalTransferedBytes);
                Console.WriteLine(
                    "Average bytes/request: "
                    + (double)wrexInstance.TotalTransferedBytes / wrexInstance.Options.NumberOfRequests);
                Console.WriteLine();
            }
        }

        public void PrintWrexOptions(WrexOptions wrexOptions)
        {
            foreach (var prop in wrexOptions.GetType().GetProperties())
            {
                var propType = prop.PropertyType;
                var value = prop.GetValue(wrexOptions);
                if (propType == typeof(WebHeaderCollection))
                {
                    var collection = (WebHeaderCollection)value;
                    if (collection.Count > 0)
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine();
                        sb.AppendLine();
                        foreach (string item in collection)
                        {
                            sb.AppendLine(string.Format("  {0} : {1}", item, collection[item]));
                        }
                        value = sb.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                }
                else if (propType == typeof(IWebProxy))
                {
                    var proxy = ((WebProxy)value);
                    if (proxy.Address != null)
                    {
                        value = proxy.Address.ToString();
                    }
                    else
                    {
                        value = string.Empty;
                    }
                }

                if (value == null || value.ToString().Equals(string.Empty))
                {
                    value = "Not specified";
                }

                Console.WriteLine(prop.Name + " : " + value);
            }
        }

        public void PrintStatusDistribution(
            IEnumerable<WrexAnalyzer.StatusCodeDistribution> statusDistributions,
            Wrex wrexInstance)
        {
            const string Title = "Status code distribution:";
            Console.WriteLine(Title);
            Console.WriteLine(new string('-', Title.Length));
            Console.WriteLine();

            foreach (var distribution in statusDistributions)
            {
                var statusCode = (int)distribution.Status;
                string outputString;
                if (statusCode == 0)
                {
                    outputString = string.Format("Failed connections : {0}", distribution.ResponseCount);
                }
                else
                {
                    outputString = string.Format(
                        "{0} [{1}] : {2} response{3}",
                        statusCode,
                        distribution.Status.ToString(),
                        distribution.ResponseCount,
                        distribution.ResponseCount > 1 ? "s" : string.Empty);
                }

                if (statusCode < 200 || statusCode > 299)
                {
                    ExtendedConsole.WriteErrorLine(outputString);
                }
                else
                {
                    Console.WriteLine(outputString);
                }
            }
            Console.WriteLine();
        }

        public void PrintResponseTimeHistogram(
            IEnumerable<WrexAnalyzer.ResponseTimeDistribution> responseTimeDistributions,
            Wrex wrexInstance)
        {
            const string Title = "Response-time histogram:";
            Console.WriteLine(Title);
            Console.WriteLine(new string('-', Title.Length));
            Console.WriteLine();

            var responsetimeDistributions = responseTimeDistributions.ToArray();

            var leftColumnLength = (responsetimeDistributions.Max(x => x.ResponseCount).ToString().Length
                                    + responsetimeDistributions.Max(x => x.TimeSpan).ToString().Length) + 10;

            foreach (var responseTimeDistribution in responsetimeDistributions)
            {
                var barWidth =
                    (int)
                    (((double)responseTimeDistribution.ResponseCount / wrexInstance.Options.NumberOfRequests)
                     * ((Console.WindowWidth * 2 / 3) - leftColumnLength));

                var leftColumn =
                    string.Format(
                        "{0} [{1}]",
                        responseTimeDistribution.TimeSpan,
                        responseTimeDistribution.ResponseCount).PadRight(leftColumnLength - 5);

                Console.Write("{0} |", leftColumn);
                ExtendedConsole.WriteInfoLine(new string('+', barWidth));
            }

            Console.WriteLine();
        }

        public void PrintSampleResponse(Wrex wrexInstance)
        {
            if (wrexInstance.SampleResponse != null)
            {
                // Increase buffer height if possible.
                ExtendedConsole.SetBufferSize(-1, Console.BufferHeight + wrexInstance.SampleResponse.Length);
                const string Title = "Sample Response:";
                Console.WriteLine(Title);
                Console.WriteLine(new string('-', Title.Length));
                Console.WriteLine();
                Console.WriteLine(wrexInstance.SampleResponse);
                Console.WriteLine();
            }
        }
    }
}