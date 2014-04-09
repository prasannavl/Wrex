// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 1:29 PM 07-04-2014

namespace Wrex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class Wrex
    {
        public const string DefaultContentType = "text/plain";
        private List<ResultValue> results;
        public bool VerifyResponseMatch;
        private int executedRequests;

        public Wrex(WrexOptions options, bool verifyResponseMatch = false)
        {
            Options = options;
            VerifyResponseMatch = verifyResponseMatch;
        }

        public WrexOptions Options { get; set; }
        public string SampleResponse { get; set; }

        public int ExecutedRequests
        {
            get
            {
                return executedRequests;
            }
            private set
            {
                executedRequests = value;
            }
        }

        public TimeSpan TotalTimeTaken { get; set; }

        public IEnumerable<ResultValue> Results
        {
            get
            {
                return results;
            }
        }

        public async Task RunAsync(Action<int, ResultValue> onProgress = null, Action<Exception> onError = null)
        {
            if (!Options.IsValidated)
            {
                Options.Validate(true);
            }

            ExecutedRequests = 0;
            results = new List<ResultValue>(Options.NumberOfRequests);

            await ProcessAsync(onProgress, onError).ConfigureAwait(false);
        }

        public async Task ProcessAsync(Action<int, ResultValue> onProgress = null, Action<Exception> onError = null)
        {
            var sw = new Stopwatch();
            sw.Start();

            Func<int, Task> action;

            if (Options.MultiThreaded)
            {
                action = x => Task.Run(async () => await ProcessTaskAsync(x, onProgress, onError).ConfigureAwait(false));
            }
            else
            {
                action = x => ProcessTaskAsync(x, onProgress, onError);
            }

            var i = 0;
            while (i < Options.NumberOfRequests)
            {
                var target = Options.NumberOfRequests - i;
                if (Options.Concurrency < target)
                {
                    target = Options.Concurrency;
                }

                var tasks = new List<Task>(target);
                target = target + i;

                while (i < target)
                {
                    tasks.Add(action(i));
                    i++;
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            sw.Stop();
            TotalTimeTaken = sw.Elapsed;
        }

        public async Task ProcessTaskAsync(int taskId, Action<int, ResultValue> onProgress, Action<Exception> onError)
        {
            var result = new ResultValue();
            var sw = new Stopwatch();

            var webRequest = WebRequest.CreateHttp(Options.Uri);
            webRequest.Headers = Options.HeaderCollection;
            if (Options.ContentType != null)
            {
                webRequest.ContentType = Options.ContentType;
            }
            webRequest.Method = Options.HttpMethod;

            HttpWebResponse response = null;
            try
            {
                sw.Start();
                response = (HttpWebResponse)await webRequest.GetResponseAsync().ConfigureAwait(false);
                sw.Stop();
                result.StatusCode = response.StatusCode;
            }
            catch (WebException ex)
            {
                sw.Stop();
                if (ex.Response != null)
                {
                    var resp = (HttpWebResponse)ex.Response;
                    result.StatusCode = resp.StatusCode;
                    if (onError != null)
                    {
                        onError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                if (onError != null)
                {
                    onError(ex);
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }

            result.TimeTaken = sw.Elapsed;
            results.Add(result);
            Interlocked.Increment(ref executedRequests);

            if (onProgress != null)
            {
                onProgress(ExecutedRequests, result);
            }
        }

        public class ResultValue
        {
            public TimeSpan TimeTaken { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
    }
}