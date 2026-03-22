namespace iLearning.Web.Services.Salesforce
{
    public class SalesforceOptions
    {
        public string LoginUrl { get; set; } = "https://login.salesforce.com";
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string ApiVersion { get; set; } = "v66.0";
    }
}
