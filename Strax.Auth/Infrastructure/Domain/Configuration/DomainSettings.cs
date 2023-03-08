namespace Domain.Configuration
{
    public class DomainSettings
    {
        public WebResource Client { get; set; }
        public WebResource Api { get; set; }
        public WebResource Auth { get; set; }
        public WebResource BusinessAPI { get; set; }
        public WebResource AngularClient { get; set; }
    }
}
