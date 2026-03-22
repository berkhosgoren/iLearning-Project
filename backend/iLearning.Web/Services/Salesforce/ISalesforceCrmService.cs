namespace iLearning.Web.Services.Salesforce
{
    public interface ISalesforceCrmService
    {
        Task<SalesforceCreateResult> CreateAccountWithContactAsync(SalesforceCreateRequest request, CancellationToken cancellationToken = default);
    }

    public class SalesforceCreateRequest
    {
        public string AccountName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Title { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
    }

    public class SalesforceCreateResult
    {
        public string AccountId { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
    }
}
