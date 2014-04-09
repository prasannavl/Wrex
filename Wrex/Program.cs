﻿// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 11:36 PM 03-04-2014

namespace Wrex
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using CommandLine.Text;

    internal class Program
    {
        private static WrexOptions options;
        private static void Main(string[] args)
        {
            Console.TreatControlCAsInput = false;
            options = new WrexOptions();
            var wrex = new Wrex(options);
            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    lock (Console.Out)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("Canceled.");
                        Console.WriteLine();
                        Console.ResetColor();
                    }
                };
            options.Process(args, () => Task.Run(async () => await RunWrexAsync(wrex)).Wait());
        }

        private static async Task RunWrexAsync(Wrex wrex = null)
        {
            try
            {
                if (wrex == null)
                {
                    wrex = new Wrex(options);
                }

                var validation = options.Validate();
                if (!validation.IsValid)
                {
                    var indent = new string(' ', 2);
                    var errors = String.Join(Environment.NewLine, validation.ErrorMessages.Select(x => indent + x));
                    throw new Exception(options.CustomErrorWithUsage(errors));
                }

                Console.WriteLine(options.GetHeader(new HelpText()));
                Console.WriteLine();

                var consolePrinter = new ConsolePrinter();

                var cancelSource = new CancellationTokenSource();
                var progressDisplayTask =
                    Task.Run(async () => await consolePrinter.ShowProgress(wrex, cancelSource.Token));
                await wrex.RunAsync(null, consolePrinter.HandleError);

                cancelSource.Cancel();
                await progressDisplayTask;

                var analyzer = new WrexAnalyzer(wrex);
                consolePrinter.PrintSummary(analyzer.GetSummary());

            }
            catch (Exception ex)
            {
                lock (Console.Out)
                {
                    Console.WriteLine(ex.Message);                    
                }
            }
        }
    }
}