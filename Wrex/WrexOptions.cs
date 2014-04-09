// Author: Prasanna V. Loganathar
// Project: Wrex
// Copyright (c) Launchark. All rights reserved.
// See License.txt in the project root for license information.
//  
// Created: 11:29 PM 04-04-2014

namespace Wrex
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using CommandLine;
    using CommandLine.Text;

    using ConsoleUtils;

    public class WrexOptions : CommandLineOptions
    {
        public string HeaderRawString;
        private Uri uri;
        private IWebProxy proxy;

        public WrexOptions()
        {
            HeaderCollection = new WebHeaderCollection();
        }

        public bool IsValidated { get; set; }

        public WebHeaderCollection HeaderCollection { get; set; }

        [Option('v', "verbose", HelpText = "Enable verbose output.", DefaultValue = false)]
        public bool Verbose { get; set; }

        [Option('n', "requests", HelpText = "Number of requests.", DefaultValue = 1)]
        public int NumberOfRequests { get; set; }

        [Option('c', "concurrency", HelpText = "Concurrency level.", DefaultValue = 1)]
        public int Concurrency { get; set; }

        [Option('m', "method", HelpText = "HTTP request method.", DefaultValue = "GET")]
        public string HttpMethod { get; set; }

        [Option('H', "header", HelpText = "HTTP headers list in the format -H \"name1:value1\" -H \"name2:value2\"")]
        public string HeaderRawStringAdder
        {
            get
            {
                throw new Exception("HeaderRawStringAdder can only be used to add to raw string.");
            }
            set
            {
                HeaderRawString += value;
                var tuple = SplitHeaderItem(value);
                HeaderCollection.Add(tuple.Item1, tuple.Item2);
            }
        }

        [Option('b', "body", HelpText = "Content body as string.")]
        public string RequestBody { get; set; }

        [Option('t', "type", HelpText = "(Default: " + Wrex.DefaultContentType + ") Content-type as string.")]
        public string ContentType { get; set; }

        [Option('p', "proxy", HelpText = "Proxy address as host:port")]
        public string ProxyAddressString { get; set; }

        [Option('T', "use-threads", HelpText = "Use multi-threaded testing, instead of an event-based pattern.", DefaultValue = false)]
        public bool MultiThreaded { get; set; }
        public IWebProxy Proxy
        {
            get
            {
                return proxy ?? (proxy = new WebProxy(ProxyAddressString));
            }
            set
            {
                proxy = value;
            }
        }

        [ValueOption(0)]
        public string UrlString { get; set; }

        public Uri Uri
        {
            get
            {
                return uri ?? (uri = new Uri(UrlString));
            }
            set
            {
                uri = value;
            }
        }

        public ValidationResult Validate(bool throwException = false)
        {
            var result = new ValidationResult();
            try
            {
                if (uri == null)
                    Uri = new Uri(UrlString);
            }
            catch
            {
                result.ErrorMessages.Add("Invalid url");
            }
            try
            {
                if (ProxyAddressString != null && proxy == null)
                {
                    Proxy = new WebProxy(ProxyAddressString);
                }
            }
            catch
            {
                result.ErrorMessages.Add("Proxy address is invalid.");
            }

            if (result.ErrorMessages.Count == 0)
            {
                result.IsValid = true;
                IsValidated = true;
            }
            else if (throwException) {
                    throw new Exception("Invalid options.");
            }

            return result;
        }

        private Tuple<string, string> SplitHeaderItem(string rawString)
        {
            var split = rawString.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            var t1 = string.Empty;
            var t2 = string.Empty;

            if (split.Length > 0)
            {
                t1 = split[0].Trim();
                if (split.Length > 1)
                {
                    t2 = split[1].Trim();
                }
            }
            return new Tuple<string, string>(t1, t2);
        }

        public override HelpText GetHelpTextBuilder()
        {
            var builder = base.GetHelpTextBuilder();
            var preIndent = new string(' ', 2);
            builder.AddPreOptionsLine(Environment.NewLine + "Usage : wrex [options] <url>");
            builder.AddPreOptionsLine(string.Empty);
            builder.AddPreOptionsLine(preIndent + "<url> - The url of the resource to be load-tested");
            builder.AddPreOptionsLine(preIndent + "      as scheme://host:port/resource");
            return builder;
        }

        public class ValidationResult
        {
            public ValidationResult()
            {
                ErrorMessages = new List<string>();
            }

            public bool IsValid { get; set; }
            public List<string> ErrorMessages { get; set; }
        }
    }
}