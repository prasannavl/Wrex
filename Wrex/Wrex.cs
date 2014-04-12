// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 1:29 PM 07-04-2014

namespace Wrex
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class Wrex
    {
        public const string DefaultContentType = "text/plain";

        private ConcurrentBag<ResultValue> results;
        private int executedRequests;
        private ConcurrentStack<WebRequest> webRequestStack;
        private int responseSimilarityCount;
        private int totalTransferedBytes;

        public Wrex(WrexOptions options)
        {
            Options = options;
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

        public int ResponseSimilarityCount
        {
            get
            {
                return responseSimilarityCount;
            }
            set
            {
                responseSimilarityCount = value;
            }
        }

        public int TotalTransferedBytes
        {
            get
            {
                return totalTransferedBytes;
            }
            set
            {
                totalTransferedBytes = value;
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
            ExecutedRequests = 0;
            TotalTransferedBytes = 0;
            ResponseSimilarityCount = 0;

            results = new ConcurrentBag<ResultValue>();

            await ProcessAsync(onProgress, onError).ConfigureAwait(false);
        }

        public async Task ProcessAsync(Action<int, ResultValue> onProgress = null, Action<Exception> onError = null)
        {
            BuildWebRequest();

            var sw = new Stopwatch();
            sw.Start();

            Func<Task> action;

            if (Options.MultiThreaded)
            {
                action = () => Task.Run(async () => await ProcessTaskAsync(onProgress, onError).ConfigureAwait(false));
            }
            else
            {
                action = () => ProcessTaskAsync(onProgress, onError);
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
                    tasks.Add(action());
                    i++;
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            sw.Stop();
            TotalTimeTaken = sw.Elapsed;
        }

        public void BuildWebRequest()
        {
            webRequestStack = new ConcurrentStack<WebRequest>();
            for (int i = 0; i < Options.NumberOfRequests; i++)
            {
                var request = WebRequest.CreateHttp(Options.Uri);
                request.Headers = Options.HeaderCollection;
                if (Options.ContentType != null)
                {
                    request.ContentType = Options.ContentType;
                }
                request.Method = Options.HttpMethod;
                if (request.Method != "GET" && Options.RequestBody != null)
                {
                    var stream = request.GetRequestStream();
                    var bytes = Encoding.UTF8.GetBytes(Options.RequestBody);
                    stream.WriteAsync(bytes, 0, bytes.Length);
                    stream.Close();
                }
                webRequestStack.Push(request);
            }
        }

        public async Task ProcessTaskAsync(Action<int, ResultValue> onProgress, Action<Exception> onError)
        {
            var result = new ResultValue();
            var sw = new Stopwatch();

            HttpWebResponse response = null;
            try
            {
                WebRequest request;
                webRequestStack.TryPop(out request);
                sw.Start();
                response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
                sw.Stop();
                result.StatusCode = response.StatusCode;
                var stream = response.GetResponseStream();
                if (stream != null)
                {
                    var strOut = await new StreamReader(stream).ReadToEndAsync();
                    if (strOut == SampleResponse)
                    {
                        Interlocked.Increment(ref responseSimilarityCount);
                        Interlocked.Add(ref totalTransferedBytes, strOut.Length);
                    }
                    else if (SampleResponse == null)
                    {
                        SampleResponse = strOut;
                        Interlocked.Increment(ref responseSimilarityCount);
                    }
                    else
                    {
                        Interlocked.Add(ref totalTransferedBytes, strOut.Length);
                    }
                }
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