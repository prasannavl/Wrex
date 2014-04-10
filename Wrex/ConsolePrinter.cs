﻿// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 4:24 PM 08-04-2014

namespace Wrex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ConsoleUtils;

    internal class ConsolePrinter
    {
        private readonly DisplayProperties properties;

        public ConsolePrinter()
        {
            properties = new DisplayProperties();
        }

        public async Task ShowProgress(Wrex wrexInstance, CancellationToken token)
        {
            bool firstRun = true;
            while (!token.IsCancellationRequested)
            {
                if (firstRun)
                {
                    firstRun = false;
                }
                else
                {
                    await Task.Delay(500);
                }
                WriteProgress(wrexInstance.ExecutedRequests, wrexInstance.Options.NumberOfRequests);
            }

            WriteProgress(wrexInstance.ExecutedRequests, wrexInstance.Options.NumberOfRequests);
            Console.WriteLine();
            Console.WriteLine();
        }

        private void WriteProgress(int count, int total)
        {
            var progressCounter = count;
            var progressFraction = ((double)progressCounter / total);

            if (!properties.IsEvaluated)
            {
                properties.Evaluate(total);
            }

            lock (Console.Out)
            {
                var currrentRequests = progressCounter.ToString().PadLeft(properties.RequestNumberStringLength, ' ');
                Console.Write(currrentRequests + " / " + total);
                Console.Write(" ");
                Console.Write("[");
                var currentIndicatorLength = (int)(progressFraction * properties.MaxLength);
                Console.Write(new string('=', currentIndicatorLength));
                Console.Write(new string(' ', properties.MaxLength - currentIndicatorLength));
                Console.Write("]");
                Console.Write(" ");
                var progress = (progressFraction * 100).ToString("###0.##").PadLeft(5, ' ');
                Console.Write("{0}", progress);
                Console.Write(" %");
                Console.Write("\r");
            }
        }

        public void HandleError(Exception ex)
        {
            const string ErrorString = "Error: ";
            lock (Console.Out)
            {
                ExtendedConsole.WriteErrorLine(ErrorString + ex.Message);
                if (ex.InnerException != null)
                {
                    ExtendedConsole.WriteErrorLine(ErrorString + ex.InnerException.Message);
                }
            }
        }

        public void PrintSummary(WrexAnalyzer.Summary summary)
        {
            const string title = "Summary:";
            Console.WriteLine(title);
            Console.WriteLine(new string('-', title.Length));
            Console.WriteLine();
            Console.WriteLine("Total time taken: {0}", summary.TotalTimeTaken);
            Console.WriteLine();
            Console.WriteLine("Fastest: {0}", summary.FastestTime);
            Console.WriteLine("Slowest: {0}", summary.SlowestTime);
            Console.WriteLine("Average: {0}", summary.AverageTime);
            Console.WriteLine("Req/second: {0}", summary.RequestsPerSecond);
            Console.WriteLine();
        }

        public void PrintStatusDistribution(IEnumerable<WrexAnalyzer.StatusCodeDistribution> statusDistributions)
        {
            const string Title = "Status code distribution:";
            Console.WriteLine(Title);
            Console.WriteLine(new string('-', Title.Length));
            Console.WriteLine();
            statusDistributions.ToList()
                .ForEach(
                    x =>
                    Console.WriteLine(
                        "{0} [{1}] : {2} response{3}",
                        (int)x.Status,
                        x.Status.ToString(),
                        x.ResponseCount,
                        x.ResponseCount > 1 ? "s" : string.Empty));
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

            var leftColumnLength = (responseTimeDistributions.Max(x => x.ResponseCount).ToString().Length
                                    + responseTimeDistributions.Max(x => x.TimeSpan).ToString().Length) + 10;

            foreach (var responseTimeDistribution in responseTimeDistributions)
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

        public class DisplayProperties
        {
            public int MaxLength;
            public int RequestNumberStringLength;
            public bool IsEvaluated = false;

            public void Evaluate(int totalRequests)
            {
                RequestNumberStringLength = totalRequests.ToString().Length;
                MaxLength = (Console.WindowWidth - 20 - (RequestNumberStringLength * 2));
                IsEvaluated = true;
            }
        }
    }
}