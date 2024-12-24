using Dapr;
using Dapr.Client;
using ScoringAPI.Camunda;
using ScoringAPI.Models;
using static ScoringAPI.Camunda.References.Dapr;

namespace ScoringAPI.Controllers;

[Route("api/v1/customer")]
[ApiController]
public class CustomerController(ILogger<CustomerController> logger, CustomerRepository repository) : ControllerBase
{
     // public endpoints
     
    [HttpGet("{customerId}")]
    public async Task<ActionResult<Customer>> Get([Required] string customerId)
    {
        var customer = await repository.GetAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }
        
        return Ok(customer);
    } 
    
    [HttpPost("find")]
    public async Task<ActionResult<Customer[]>> Find([Required] FindCustomerRequest request)
    {
        // this is a fake implementation, in real world it would be a database query
        var customerId = Customer.GetId(request.CustomerEmail);
        var customer = await repository.GetAsync(customerId);
        if (customer == null)
        {
            return NotFound();
        }
        
        return Ok(new[] { customer });
    } 
     
    [HttpPost("score")]
    public async Task<ActionResult> Score([Required, FromBody] ScoreCustomerRequest scoreCustomerRequest, [FromServices] DaprClient daprClient)
    {
        var requestId = Guid.NewGuid().ToString();
        var request = new CreateInstanceRequest(Processes.CustomerScoring, null, "10s", null, 
            new ScoreCustomerRequestExt(scoreCustomerRequest.CustomerName, scoreCustomerRequest.CustomerEmail, 
             scoreCustomerRequest.CustomerPhone, scoreCustomerRequest.MonthlyIncome, requestId));

        try 
        {
            await daprClient.InvokeBindingAsync<CreateInstanceRequest, CreateInstanceResponse>(
                Bindings.ZeebeCommand, Operations.Zeebe.CreateInstance, request);
        } 
        catch (DaprException ex)
        {
            logger.LogError(ex, "Error while creating instance {BpmnProcessId} with request {Request}", 
                request.BpmnProcessId, request);
            return BadRequest(new { Error = "Score: error while invoking Dapr binding.", requestId });
        }

        /*
            The process could be run synchronously,
            but it involves a combination of synchronous (initial part) and asynchronous (subsequent part) operations.
            There is no way how to start synchronous process and then continue asynchronously.

            One solution (and perhaps cleaner) would be to split the process into two independent parts.

            Another solution, to maintain visibility over the entire process without splitting it into separate parts,
            is to wait for the process response.

            However, we can't block and wait for response due to its asynchronous nature
            and the need to return a response to client.
            This solution saves the state of the process in the repository and polls it until completion.
                        
            Polling loop to check for process completion
        */
        
        var timeOut = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < timeOut)
        {
            var customerId = Customer.GetId(scoreCustomerRequest.CustomerEmail);
            var customer = await repository.GetAsync(customerId);
            if (customer?.CustomerId != null)
            {
                return Ok(new
                {
                    RequestId = requestId,
                    RequestTime = DateTime.UtcNow,
                    customer.CustomerId
                });
            }
            
            await Task.Delay(1000);
        }
        
        return BadRequest(new { Error = "Couldn't get response from the process in time. Please try again later.", requestId });

    }
    
    [HttpPost("upload-document")]
    public async Task<ActionResult> UploadDocument([Required, FromForm] string requestId, [Required, FromForm] string customerId, [Required, FromForm] IFormFile document, [FromServices] DaprClient daprClient)
    {
        var fileLength = document.Length;
        if (fileLength > 1024 * 1024 * 5)
        {
            return BadRequest("File is too large, exceeded 5MB limit");
        }

        byte[] fileBytes;
        using (var memoryStream = new MemoryStream())
        {
            await document.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }
        
        try 
        {
            // Save the document content to a storage
            await daprClient.InvokeBindingAsync(Bindings.Storage, Operations.Storage.Create, 
                new SaveBinaryFileRequest(document.FileName, Convert.ToBase64String(fileBytes), null));
        } 
        catch (DaprException ex) 
        {
            logger.LogError("Error while uploading document {DocumentName} for customer {CustomerId} and request {RequestId}", 
                document.FileName, customerId, requestId);
            
            return BadRequest(new { Error = "UploadDocument: error while uploading document.", requestId });
        }

        try
        {
            // invoke document processing
            await daprClient.InvokeBindingAsync<PublishMessageRequest, PublishMessageResponse>(
                Bindings.ZeebeCommand, Operations.Zeebe.PublishMessage,
                new PublishMessageRequest(MessageNames.OnUploadDocument, requestId, null, "10s",
                    new UploadDocumentRequest(customerId, document.FileName)));
        }
        catch (DaprException)
        {
            logger.LogError("Error while publishing message {MessageName} for customer {CustomerId} and request {RequestId}", 
                MessageNames.OnUploadDocument, customerId, requestId);
            
            return BadRequest(new { Error = "UploadDocument: error while invoking Dapr binding.", requestId });
        }

        return Ok(new { requestId });
    }

    // Zeebe endpoints

    [HttpPost("/register-customer")]
    public async Task<ActionResult> RegisterCustomer([Required, FromBody] RegisterCustomerRequest registerCustomerRequest, [FromServices] IHttpContextAccessor contextAccessor) 
    {
        var processInstanceKey = contextAccessor.HttpContext!.Request.Headers[References.Headers.XZeebeProcessInstanceKey].FirstOrDefault();
        
        var customerId = Customer.GetId(registerCustomerRequest.CustomerEmail.Trim());
        var customer = await repository.GetAsync(customerId) ?? new Customer
        {
            CustomerId = customerId,
            CustomerEmail = registerCustomerRequest.CustomerEmail.Trim(),
            CustomerName = registerCustomerRequest.CustomerName.Trim(),
            CustomerPhone = registerCustomerRequest.CustomerPhone.Trim()
        };
        customer.AddRequest(new Customer.ScoreRequest(registerCustomerRequest.RequestId, processInstanceKey, DateTime.UtcNow, registerCustomerRequest.MonthlyIncome));
        await repository.AddAsync(customer);

        return Ok(new { customer.CustomerId });
    }

    [HttpPost("/rate-customer")]
    public async Task<ActionResult> DetermineCustomerRating([Required, FromBody] RateCustomerRequest rateCustomerRequest)
    {

        var customer = await repository.GetAsync(rateCustomerRequest.CustomerId);
        if (customer == null)
        {
            return BadRequest("Customer doesn't exist");
        }

        // If the customer already has a rating, return it
        if (customer.CustomerRating != null)
        {
            return Ok(new { customer.CustomerRating });
        }
        
        // Generate a random rating (simulates external rating service call)
        customer.CustomerRating = Random.Shared.Next(1, 4) switch
        {
            1 => "Good",
            2 => "Bad",
            _ => "Neutral"
        };
        await repository.AddAsync(customer);

        return Ok(new { customer.CustomerId, customer.CustomerRating });
    }
    
    [HttpPost("/customer-eligibility")]
    public async Task<ActionResult> UpdateCustomerEligibility([Required, FromBody] UpdateCustomerEligibilityRequest updateCustomerEligibilityRequest)
    {
        var customer = await repository.GetAsync(updateCustomerEligibilityRequest.CustomerId);
        if (customer == null)
        {
            return BadRequest("Customer doesn't exist");
        }

        customer.CustomerScore = updateCustomerEligibilityRequest.CustomerScore;
        customer.IsEligible = updateCustomerEligibilityRequest.IsEligible;
        await repository.AddAsync(customer);

        return Ok(new { customer.LastScoreRequest?.ProcessInstanceKey });
    }

    [HttpPost("/notify-customer")]
    public async Task<ActionResult> NotifyCustomer([Required, FromBody] NotifyCustomerRequest notifyCustomerRequest, [FromServices] DaprClient daprClient)
    {
        var customer = await repository.GetAsync(notifyCustomerRequest.CustomerId);
        if (customer == null)
            return BadRequest();
        
        // send email
        var body = $"Dear {customer.CustomerName},\n\n" +
               $"We have completed the scoring process for you. Here are the results:\n\n" +
               $"Customer Rating: {customer.CustomerRating}\n" +
               $"Eligibility: {(customer.IsEligible ? "Eligible" : "Not Eligible")}\n" +
               $"Score: {customer.CustomerScore}\n\n" +
               $"Thank you for choosing our services.\n\n" +
               $"Best regards,\n" +
               $"Incredible Inc.";
        var metadata = new Dictionary<string, string?>
        {
            ["emailFrom"] = "noreply@incredible.inc",
            ["emailTo"] = customer.CustomerEmail,
            ["subject"] = "Scoring Results"
        };

        await daprClient.InvokeBindingAsync(Bindings.Smtp, Operations.Smtp.Create, body, metadata);

        return Ok(new { customer.LastScoreRequest?.ProcessInstanceKey });
    }
    
    [HttpPost("/upload-document")]
    public async Task<ActionResult> OnUploadDocument([Required, FromBody] UploadDocumentRequest uploadDocumentRequest)
    {
        
        var customer = await repository.GetAsync(uploadDocumentRequest.CustomerId);
        if (customer == null)
            return BadRequest();
        
        var document = new Document
        {
            Id = Guid.NewGuid().ToString(),
            Name = uploadDocumentRequest.FilePath,
            Status = "New"
        };
        customer.AddDocument(document);
        await repository.AddAsync(customer);
        
        return Ok(new { document.Id, DocumentName = document.Name, DocumentStatus = document.Status });
    }

    [HttpPost("/process-document")]
    public async Task<ActionResult> ProcessDocument([Required, FromBody] ProcessDocumentRequest processDocumentRequest)
    {
        var customer = await repository.GetAsync(processDocumentRequest.CustomerId);
        if (customer == null)
            return BadRequest();

        var document = customer.GetDocument(processDocumentRequest.DocumentId);
        if (document == null)
            return BadRequest();
        
        document.Status = "Processed";
        await repository.AddAsync(customer);
        
        return Ok(new { document.Id, DocumentName = document.Name, DocumentStatus = document.Status });
    }
}