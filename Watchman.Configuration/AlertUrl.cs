namespace Watchman.Configuration
{
    public class AlertUrl : AlertTarget
    {
        protected bool Equals(AlertUrl other)
        {
            return string.Equals(Url, other.Url);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return Equals((AlertUrl) obj);
        }

        public override int GetHashCode()
        {
            return Url?.GetHashCode() ?? 0;
        }

        public string Url { get; set; }
    }
}
