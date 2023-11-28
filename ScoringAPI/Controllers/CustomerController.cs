using Dapr.Client;
using ScoringAPI;
using ScoringAPI.Controllers;

namespace ScoringAPI.Controllers;

[Route("api/v1/customer")]
[ApiController]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> logger;

    public CustomerController(ILogger<CustomerController> logger)
    {
        this.logger = logger;
    }

    [HttpPost("score")]
    [ProducesResponseType(typeof(ScoringResult),(int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<ScoringResult>> ScoreCustomer(ScoringRequest scoringRequest, [FromServices] DaprClient daprClient)
    {
        var request = new CreateInstanceRequest(Processes.CustomerScoring, null /* last */, true, "10s",
            null /* all root scoped back */, scoringRequest);
        
        logger.LogInformation("Process {BpmnProcessId} requested scoring for rating {Rating} and monthly income {Income}", 
            request.BpmnProcessId, scoringRequest.Rating, scoringRequest.MonthlyIncome);
        
        var response = await daprClient.InvokeBindingAsync<CreateInstanceRequest, CreateInstanceResponse>(
            Commands.ZeebeCommand, Commands.CreateInstance, request);

        logger.LogInformation("Process {BpmnProcessId} for instance {ProcessInstanceKey} finished with result: {Variables}", 
            response.BpmnProcessId, response.ProcessInstanceKey, response.Variables);
        
        var scoringResult = JsonSerializer.Deserialize<ScoringResult>(response.Variables!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        
        return Ok(scoringResult);
    }

    [HttpPost("approve-reject")]
    public async Task<NullResponse> ApproveCustomerProcess(ScoringRequest scoringRequest, [FromServices] DaprClient daprClient)
    {
        var request = new CreateInstanceRequest(Processes.CustomerScoringApprovalProcessAsync, null /* last */, false, "10s",
            null /* all root scoped back */, scoringRequest);
        
        await daprClient.InvokeBindingAsync<CreateInstanceRequest, CreateInstanceResponse>(
            Commands.ZeebeCommand, Commands.CreateInstance, request);

        return new NullResponse();
    }

    [HttpPost("/approve-customer")]
    public NullResponse ApproveCustomer()
    {
        logger.LogInformation("Customer approved");
        return new NullResponse();

    }
    
    [HttpPost("/reject-customer")]
    public NullResponse RejectCustomer()
    {
        logger.LogInformation("Customer rejected");
        return new NullResponse();
    }
    
    [HttpPost("/approve")]
    public NullResponse Approve()
    {
        logger.LogInformation("Customer approved");
        return new NullResponse();

    }
}

public record NullResponse
{
}