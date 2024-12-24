using System.Text;

namespace ScoringAPI.Models;

public class Customer
{
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerRating { get; set; }
    public int? CustomerScore { get; set; }
    public bool IsEligible { get; set; }

    public HashSet<Document> Documents { get; init; } = [];
    
    public List<ScoreRequest> ScoreRequests { get; init; } = [];

    public Document? GetDocument(string id)
    {
        return Documents.FirstOrDefault(d => d.Id == id);
    }
    
    public void AddDocument(Document document)
    {
        Documents.Remove(document);
        Documents.Add(document);
    }
    
    public void RemoveDocument(Document document)
    {
        Documents.Remove(document);
    }
    
    public void AddRequest(ScoreRequest request)
    {
        ScoreRequests.Add(request);
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is Customer other)
        {
            return CustomerEmail == other.CustomerEmail;
        }
        return false;
    }

    protected bool Equals(Customer other)
    {
        return CustomerEmail == other.CustomerEmail;
    }

    public override int GetHashCode()
    {
        return CustomerEmail.GetHashCode();
    }
    
    public ScoreRequest? LastScoreRequest => ScoreRequests.LastOrDefault();
    
    public Document? LastDocument => Documents.LastOrDefault();
    
    public int MonthlyIncome => LastScoreRequest?.MonthlyIncome ?? 0;
        
    
    public record ScoreRequest(string RequestId, string? ProcessInstanceKey, DateTime RequestTime, int MonthlyIncome);
    
    public static string GetId(string email)
    {
        var input = $"{email.Trim().ToLowerInvariant()}";
        var bytes = Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes).ToLowerInvariant();
        return base64.TrimEnd('=');
    }
}

public class Document : IComparable<Document>
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; }

    public int CompareTo(Document? other)
    {
        if (other is null) return 1;
        return string.Compare(Id, other.Id, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        if (obj is Document other)
        {
            return Id == other.Id;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

