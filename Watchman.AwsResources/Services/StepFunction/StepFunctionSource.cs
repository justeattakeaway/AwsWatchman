using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

namespace Watchman.AwsResources.Services.StepFunction
{
    public class StepFunctionSource : ResourceSourceBase<StateMachineListItem>
    {
        private readonly IAmazonStepFunctions _amazonStepFunctions;

        public StepFunctionSource(IAmazonStepFunctions amazonStepFunctions)
        {
            _amazonStepFunctions = amazonStepFunctions;
        }

        protected override string GetResourceName(StateMachineListItem resource) => resource.Name;

        protected override async Task<IEnumerable<StateMachineListItem>> FetchResources()
        {
            var results = new List<IEnumerable<StateMachineListItem>>();
            string marker = null;

            do
            {
                var response = await _amazonStepFunctions.ListStateMachinesAsync(
                    new ListStateMachinesRequest
                    {
                        NextToken = marker
                    });

                results.Add(response.StateMachines);
                marker = response.NextToken;
            }
            while (!string.IsNullOrEmpty(marker));

            return results.SelectMany(x => x).ToList();
        }
    }
}
