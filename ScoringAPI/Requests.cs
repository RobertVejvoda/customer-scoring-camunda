namespace ScoringAPI;

// initial request/response
public record ScoreCustomerRequest([Required] string CustomerName, [Required] string CustomerEmail, [Required] string CustomerPhone, [Required] int MonthlyIncome);

internal record ScoreCustomerRequestExt([Required] string CustomerName, [Required] string CustomerEmail, [Required] string CustomerPhone, [Required] int MonthlyIncome, [Required] string RequestId) 
    : ScoreCustomerRequest(CustomerName, CustomerEmail, CustomerPhone, MonthlyIncome);
// public record UploadCustomerDocumentRequest(string CustomerId, string DocumentName, [Required] IFormFile Document);
public record FindCustomerRequest([Required] string CustomerEmail);

// zeebe endpoints
public record RegisterCustomerRequest([Required] string RequestId, [Required] string CustomerName, [Required] string CustomerEmail, [Required] string CustomerPhone, [Required] int MonthlyIncome);
public record RateCustomerRequest([Required] string CustomerId); 
public record UpdateCustomerEligibilityRequest([Required] string CustomerId, [Required] int CustomerScore, [Required] bool IsEligible);
public record NotifyCustomerRequest([Required] string CustomerId);

public record UploadDocumentRequest([Required] string CustomerId, [Required] string FilePath);
public record ProcessDocumentRequest([Required] string CustomerId, [Required] string DocumentId);

