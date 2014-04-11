// Author: Prasanna V. Loganathar
// Project: Wrex.Console
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 8:49 PM 10-04-2014

namespace Wrex.Console
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CommandLine.Text;

    using Nito.AsyncEx;

    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                AsyncContext.Run(() => MainAsync(args));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void MainAsync(string[] args)
        {
            Console.TreatControlCAsInput = false;

            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    lock (Console.Out)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("Canceled.");
                        Console.WriteLine();
                    }
                };

            var cmdOptions = new CmdOptions();
            cmdOptions.Process(args, async () => await RunWrexAsync(cmdOptions));
        }

        private static async Task RunWrexAsync(CmdOptions cmdOptions)
        {
            var validation = cmdOptions.Validate();

            if (!validation.IsValid)
            {
                var indent = new string(' ', 2);
                var errors = String.Join(Environment.NewLine, validation.ErrorMessages.Select(x => indent + x));
                throw new Exception(cmdOptions.CustomErrorWithUsage(errors));
            }

            Console.WriteLine(cmdOptions.GetHeader(new HelpText()));
            Console.WriteLine();

            try
            {
                var wrex = new Wrex(cmdOptions.GetWrexOptions());
                var consoleOut = new ConsoleOutput();

                if (cmdOptions.Verbose)
                {
                    const string OptionsText = "Options:";
                    Console.WriteLine(OptionsText);
                    Console.WriteLine(new string('-', OptionsText.Length));
                    Console.WriteLine();
                    consoleOut.PrintWrexOptions(wrex.Options);
                    Console.WriteLine();
                    const string ProgressText = "Progress:";
                    Console.WriteLine(ProgressText);
                    Console.WriteLine(new string('-', ProgressText.Length));
                    Console.WriteLine();
                }

                var cancelSource = new CancellationTokenSource();
                var progressDisplayTask = consoleOut.ShowProgress(wrex, cancelSource.Token);
                await wrex.RunAsync(null, consoleOut.HandleError);

                cancelSource.Cancel();
                await progressDisplayTask;

                var analyzer = new WrexAnalyzer(wrex);
                var summary = analyzer.GetSummary();
                consoleOut.PrintSummary(summary, wrex);
                consoleOut.PrintStatusDistribution(analyzer.GetStatusDistribution(), wrex);
                consoleOut.PrintResponseTimeHistogram(analyzer.GetReponseTimeDistribution(summary), wrex);
                if (cmdOptions.Verbose)
                {
                    consoleOut.PrintSampleResponse(wrex);
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                lock (Console.Out)
                {
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}