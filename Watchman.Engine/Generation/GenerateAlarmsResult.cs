namespace Watchman.Engine.Generation
{
    public class GenerateAlarmsResult
    {
        public IList<string> FailingGroups { get; }
        public bool Success => !FailingGroups.Any();

        public GenerateAlarmsResult(IList<string> failingGroups = null)
        {
            FailingGroups = failingGroups ?? new string[0];
        }
    }
}
