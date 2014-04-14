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
        private int totalTransferedBytes;
        private TaskCompletionSource<bool> groupTask;
        private int completedItemsInGroup;
        private int groupTarget;
        private Action<int, ResultValue> onProgressAction;
        private Action<Exception> onErrorAction;

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

        public bool Started { get; private set; }
        public bool Completed { get; private set; }

        public async Task RunAsync(Action<int, ResultValue> onProgress = null, Action<Exception> onError = null)
        {
            if (Started && !Completed)
            {
                throw new Exception("Cannot run when an operation is already in progress.");
            }

            Started = true;

            ServicePointManager.DefaultConnectionLimit = Options.Concurrency;
            ServicePointManager.MaxServicePoints = Options.NumberOfRequests;
            ServicePointManager.SetTcpKeepAlive(true, 45000, 20000);
            ServicePointManager.MaxServicePointIdleTime = 20000;

            ExecutedRequests = 0;
            TotalTransferedBytes = 0;

            results = new ConcurrentBag<ResultValue>();
            onProgressAction = onProgress;
            onErrorAction = onError;

            try
            {
                await ProcessAsync().ConfigureAwait(false);
            }
            finally
            {
                Completed = true;
            }
        }

        private async Task ProcessAsync()
        {
            var sw = new Stopwatch();
            sw.Start();

            groupTarget = Options.Concurrency;

            var i = Options.NumberOfRequests;
            var c = Options.Concurrency;

            var x = 0;

            Action fireRequest;
            if (Options.ThreadedSynchronousMode)
            {
                fireRequest = FireRequest;
            }
            else
            {
                fireRequest = FireRequestAsync;
            }

            while (i > c)
            {
                groupTask = new TaskCompletionSource<bool>();
                completedItemsInGroup = 0;
                x = 0;

                for (; x < c; x++)
                {
                    fireRequest();
                }

                i = i - x;
                await groupTask.Task.ConfigureAwait(false);
            }

            if (i > 0)
            {
                groupTask = new TaskCompletionSource<bool>();
                completedItemsInGroup = 0;
                groupTarget = i;

                for (; i > 0; i--)
                {
                    fireRequest();
                }

                await groupTask.Task.ConfigureAwait(false);
            }

            sw.Stop();
            TotalTimeTaken = sw.Elapsed;
        }

        private void HandleError(Exception ex)
        {
            if (onErrorAction != null)
            {
                onErrorAction(ex);
            }
        }

        private void HandleProgress(ResultValue result)
        {
            if (onProgressAction != null)
            {
                onProgressAction(ExecutedRequests, result);
            }
        }

        private void HandleResponse(
            ResponseState state,
            Func<HttpWebResponse> getResponse,
            Func<Stream, string> getResponseOutput)
        {
            var result = new ResultValue();
            try
            {
                using (var response = getResponse())
                {
                    state.Stopwatch.Stop();
                    result.StatusCode = response.StatusCode;
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            if (response.ContentLength > 0 && SampleResponse != null)
                            {
                                Interlocked.Add(ref totalTransferedBytes, (int)response.ContentLength);
                            }
                            else
                            {
                                var strOut = getResponseOutput(stream);

                                if (!string.IsNullOrEmpty(strOut))
                                {
                                    Interlocked.Add(ref totalTransferedBytes, strOut.Length);
                                    SampleResponse = strOut;
                                }
                            }
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                state.Stopwatch.Stop();
                if (ex.Response != null)
                {
                    using (var resp = (HttpWebResponse)ex.Response)
                    {
                        result.StatusCode = resp.StatusCode;
                        HandleError(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                state.Stopwatch.Stop();
                HandleError(ex);
            }

            result.TimeTaken = state.Stopwatch.Elapsed;
            results.Add(result);

            Interlocked.Increment(ref executedRequests);
            HandleProgress(result);
            SetOperationStatus();
        }

        private void SetOperationStatus()
        {
            var itemsCompleted = Interlocked.Increment(ref completedItemsInGroup);
            if (itemsCompleted < groupTarget)
            {
                return;
            }

            groupTask.TrySetResult(true);
        }

        private WebRequest CreateRequest()
        {
            var request = WebRequest.Create(Options.Uri);
            request.Headers = Options.HeaderCollection;
            if (Options.ContentType != null)
            {
                request.ContentType = Options.ContentType;
            }

            request.Timeout = Options.Timeout;
            request.Method = Options.HttpMethod;
            if (!request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) && Options.RequestBody != null)
            {
                var stream = request.GetRequestStream();
                var bytes = Encoding.UTF8.GetBytes(Options.RequestBody);
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }

            return request;
        }

        private void FireRequest()
        {
            Task.Run(
                () =>
                    {
                        try
                        {
                            var request = CreateRequest();
                            HandleResponse(
                                new ResponseState { Request = request, Stopwatch = Stopwatch.StartNew() },
                                () => (HttpWebResponse)request.GetResponse(),
                                (stream) => stream != null ? new StreamReader(stream).ReadToEnd() : null);
                        }
                        catch (Exception ex)
                        {
                            HandleError(ex);
                            SetOperationStatus();
                        }
                    });
        }

        private void FireRequestAsync()
        {
            var request = CreateRequest();
            request.BeginGetResponse(
                ResponseCallback,
                new ResponseState { Request = request, Stopwatch = Stopwatch.StartNew() });
        }

        private void ResponseCallback(IAsyncResult ar)
        {
            var state = (ResponseState)ar.AsyncState;
            HandleResponse(
                state,
                () => (HttpWebResponse)state.Request.EndGetResponse(ar),
                (stream) => stream != null ? new StreamReader(stream).ReadToEndAsync().Result : null);
        }

        private struct ResponseState
        {
            public WebRequest Request;
            public Stopwatch Stopwatch;
        }

        public class ResultValue
        {
            public TimeSpan TimeTaken { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }
    }
}