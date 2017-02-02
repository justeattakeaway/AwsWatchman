namespace Watchman.Configuration
{
    public class AlertEmail : AlertTarget
    {
        public string Email { get; set; }

        protected bool Equals(AlertEmail other)
        {
            return string.Equals(Email, other.Email);
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

            return Equals((AlertEmail) obj);
        }

        public override int GetHashCode()
        {
            return Email?.GetHashCode() ?? 0;
        }
    }
}
