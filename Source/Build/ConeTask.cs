﻿using System;
using Cone.Runners;
using Microsoft.Build.Framework;

namespace Cone.Build
{
    public class ConeTask : MarshalByRefObject, ITask, ICrossDomainLogger
    {
        const string SenderName = "Cone";
        bool noFailures;

        public IBuildEngine BuildEngine { get; set; }

        public bool Execute() {
            try {
                noFailures = true;
                CrossDomainConeRunner.RunTestsInTemporaryDomain(this,
                    System.IO.Path.GetDirectoryName(Path),
                    new []{ Path });
                return noFailures;
            } catch(Exception e) {
                BuildEngine.LogErrorEvent(new BuildErrorEventArgs("RuntimeFailure", string.Empty, string.Empty, 0, 0, 0, 0, string.Format("{0}", e), string.Empty, SenderName));
                return false;
            }
        }

        void ICrossDomainLogger.Info(string message) {
            BuildEngine.LogMessageEvent(new BuildMessageEventArgs(message, string.Empty, SenderName, MessageImportance.High));
        }

        void ICrossDomainLogger.Failure(string file, int line, int column, string message) {
            noFailures = false;
            BuildEngine.LogErrorEvent(new BuildErrorEventArgs("Test ", string.Empty, file, line, 0, 0, column, message, string.Empty, SenderName));
        }

        public ITaskHost HostObject { get; set; }

        [Required]
        public string Path { get; set; }
    }
}
