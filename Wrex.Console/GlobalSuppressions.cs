// Author: Prasanna V. Loganathar
// Project: Wrex.Console
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
// Created: 10:36 AM 18-04-2014

using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member",
        Target = "Wrex.Console.Program.#MainAsync(System.String[])")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member",
        Target = "Wrex.Console.ConsoleOutput.#HandleError(System.Exception)")]
[assembly:
    SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member",
        Target = "Wrex.Console.ConsoleOutput.#WriteProgress(System.Int32,System.Int32)")]