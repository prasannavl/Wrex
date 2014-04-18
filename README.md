Wrex
====

Wrex is an event-based asynchronous http-load testing tool. It can also use multi-threaded testing to emulate apache benchmark (ab). The default mode is to event-based, and fully asynchronous.

Wrex.Console is console program that uses the library to run load-tests at command-line.

      Wrex Console 1.7.0.0
      Http-load testing tool.
      Copyright 2014, Launchark.
      
      Usage : wrex [options] <url>
      
          <url> - The url of the resource to be load-tested
              as scheme://host:port/resource
      
      Options:
      
        -v, --verbose        (Default: False) Enable verbose output.
      
        -n, --requests       (Default: 1) Number of requests.
      
        -c, --concurrency    (Default: 1) Concurrency level.
      
        -m, --method         (Default: GET) HTTP request method.
      
        -H, --header         HTTP headers list in the format -H "name1:value1" -H
                             "name2:value2"
      
        -b, --body           Content body as string.
      
        -t, --type           (Default: text/plain) Content-type as string.
      
        -p, --proxy          Proxy address as host:port
      
        -s, --threaded-sync  (Default: False) Use multi-threaded testing, instead of
                             an event-based pattern.
      
        -h, --help           Display this screen.
