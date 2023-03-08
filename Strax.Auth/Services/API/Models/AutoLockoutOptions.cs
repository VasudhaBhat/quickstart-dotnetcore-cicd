namespace API.Models
{
    public class AutoLockoutOptions
    {
        public int DefaultLockoutTimeSpan { get; set; }
        public int MaxFailedAccessAttempts { get; set; }
        public bool AllowedForNewUsers { get; set; }
    }
}
