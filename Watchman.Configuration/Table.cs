namespace Watchman.Configuration
{
    public class Table
    {
        public string Name { get; set; }
        public string Pattern { get; set; }
        public double? Threshold { get; set; }
        public bool? MonitorWrites { get; set; }

        public static implicit operator Table(string text)
        {
            return new Table
            {
                Name = text
            };
        }

        public override string ToString()
        {
            return Name ?? Pattern;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Table))
            {
                return false;
            }

            return Equals((Table)obj);
        }

        protected bool Equals(Table other)
        {
            return
                string.Equals(Name, other.Name)
                && string.Equals(Pattern, other.Pattern)
                && Threshold.Equals(other.Threshold)
                && MonitorWrites.Equals(other.MonitorWrites);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    (Threshold?.GetHashCode()*397 ?? 0)
                    ^ (Pattern?.GetHashCode() * 211 ?? 0)
                    ^ (Name?.GetHashCode() ?? 0);
            }
        }
    }
}
