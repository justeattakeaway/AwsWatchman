using Newtonsoft.Json;

namespace Watchman.Configuration
{
    public class ReportTarget
    {
        public string Email { get; }

        [JsonConstructor]
        public ReportTarget(string email)
        {
            Email = email;
        }

        public override int GetHashCode()
        {
            return Email?.GetHashCode() ?? 0;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ReportTarget;
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            return Email?.Equals(other.Email) ?? false;
        }
    }
}
