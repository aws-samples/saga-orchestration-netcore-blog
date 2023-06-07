using Amazon.CDK;

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
