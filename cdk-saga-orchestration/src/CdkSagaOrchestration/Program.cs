using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CdkSagaOrchestration
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new CdkSagaOrchestrationStack(app, "CdkSagaOrchestrationStack");

            app.Synth();
        }
    }
}
