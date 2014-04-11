// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 11:29 PM 04-04-2014

namespace Wrex
{
    using System;
    using System.Net;

    public class WrexOptions
    {
        public WrexOptions()
        {
            HeaderCollection = new WebHeaderCollection();
        }

        public WebHeaderCollection HeaderCollection { get; set; }
        public int NumberOfRequests { get; set; }
        public int Concurrency { get; set; }
        public string HttpMethod { get; set; }
        public string RequestBody { get; set; }
        public string ContentType { get; set; }
        public bool MultiThreaded { get; set; }
        public IWebProxy Proxy { get; set; }
        public Uri Uri { get; set; }
    }
}