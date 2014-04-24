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
        private Action<int, ResultValue> onProgressAction;
        private Action<Exception> onErrorAction;
        private SemaphoreSlim throttle;
        private TaskCompletionSource<bool> completion;

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
            Action fireRequest;
            if (Options.ThreadedSynchronousMode)
            {
                fireRequest = FireRequest;
            }
            else
            {
                fireRequest = FireRequestAsync;
            }

            throttle = new SemaphoreSlim(Options.Concurrency);
            completion = new TaskCompletionSource<bool>();

            var sw = Stopwatch.StartNew();

            // ThreadPool.QueueUserWorkItem(StartNonIntrusiveAsyncCallbackTimeout, this);

            for (int i = 0; i < Options.NumberOfRequests; i++)
            {
                await throttle.WaitAsync();
                fireRequest();
            }
            try
            {
                await completion.Task;
            }
            catch (Exception ex)
            {
                HandleError(ex);
            }
            finally
            {
                sw.Stop();
                TotalTimeTaken = sw.Elapsed;
            }
        }

        // Cancellation - Method 1 - Non intensive, non-accurate.
        // 
        //private List<State> activeAsyncList = new List<State>();
        // 
        //private async void StartNonIntrusiveAsyncCallbackTimeout(object wrexInstanceWrapper)
        //{
        //    var instance = (Wrex)wrexInstanceWrapper;
        //    var lastValue = 0;
        //    while (true)
        //    {
        //        await Task.Delay(instance.Options.Timeout);
        //        var exec = instance.ExecutedRequests;
        //        if (lastValue == exec)
        //        {
        //            lastValue = exec;
        //            try
        //            {
        //                foreach (var queue in activeAsyncList)
        //                {
        //                    try
        //                    {
        //                        queue.Request.Abort();
        //                        queue.AsyncResult.AsyncWaitHandle.Dispose();
        //                    }
        //                    catch
        //                    {
        //                    }
        //                    completion.TrySetException(new Exception("Request timed out."));
        //                }
        //            }
        //            catch
        //            {
        //            }
        //        }
        //    }
        //}

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
            State state,
            Func<State, HttpWebResponse> getResponse,
            Func<Stream, string> getResponseOutput)
        {
            state.Stopwatch.Stop();
            // activeAsyncList.Remove(state);
            var result = new ResultValue();

            try
            {
                using (var response = getResponse(state))
                {
                    result.StatusCode = response.StatusCode;
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            var strOut = getResponseOutput(stream);

                            if (!string.IsNullOrEmpty(strOut))
                            {
                                Interlocked.Add(ref totalTransferedBytes, strOut.Length);
                                if (SampleResponse == null)
                                {
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
            throttle.Release();
            if (executedRequests == Options.NumberOfRequests)
            {
                completion.SetResult(true);
            }
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
                                new State { Request = request, Stopwatch = Stopwatch.StartNew() },
                                GetResponse,
                                GetStringFromStream);
                        }
                        catch (Exception ex)
                        {
                            HandleError(ex);
                            SetOperationStatus();
                        }
                    });
        }

        private static HttpWebResponse GetResponse(State state)
        {
            return (HttpWebResponse)state.Request.GetResponse();
        }

        private static HttpWebResponse GetResponseAsync(State state)
        {
            return (HttpWebResponse)state.Request.EndGetResponse(state.AsyncResult);
        }

        private static string GetStringFromStream(Stream stream)
        {
            return stream != null ? new StreamReader(stream).ReadToEnd() : null;
        }

        private void FireRequestAsync()
        {
            var request = CreateRequest();
            var state = new State { Request = request, Stopwatch = Stopwatch.StartNew() };
            request.BeginGetResponse(ResponseCallback, state);

            //state.AsyncResult = iar;
            //activeAsyncList.Add(state);
            // ThreadPool.RegisterWaitForSingleObject(iar.AsyncWaitHandle, HandleTimeout, state, Options.Timeout, true);
        }

        // Proper method for cancellation.
        // Too intensive on high-concurrency, which is the normal scenario.
        // 
        //private void HandleTimeout(object stateObject, bool timedOut)
        //{
        //    if (timedOut)
        //    {
        //        var state = (State)stateObject;
        //        if (!state.AsyncResult.IsCompleted)
        //        {
        //            // Time out the operation in a lock-free manner. 
        //            // Interlocked.CompareExchange(ref state.TimedOutState, 1, 0);
        //            state.Request.Abort();
        //            state.Stopwatch.Stop();
        //            state.AsyncResult.AsyncWaitHandle.Dispose();
        //            HandleError(new Exception("Operation timed out."));
        //            Interlocked.Increment(ref executedRequests);
        //            SetOperationStatus();
        //        }
        //    }
        //}

        private void ResponseCallback(IAsyncResult ar)
        {
            // Make sure the operation hasn't timed out, in a synchronized way.
            // if (Interlocked.CompareExchange(ref state.TimedOutState, 1, 1) == 1) return;

            var state = (State)ar.AsyncState;
            state.AsyncResult = ar;
            HandleResponse(state, GetResponseAsync, GetStringFromStream);
        }

        public class ResultValue
        {
            public TimeSpan TimeTaken { get; set; }
            public HttpStatusCode StatusCode { get; set; }
        }

        private class State
        {
            // public int TimedOutState;
            public WebRequest Request;
            public Stopwatch Stopwatch;
            public IAsyncResult AsyncResult;
        }
    }
}