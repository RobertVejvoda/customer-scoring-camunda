using Dapr.Client;

namespace ScoringAPI.Controllers;

[Route("api/v1/customer")]
[ApiController]
public class CustomerController(ILogger<CustomerController> logger) : ControllerBase
{
    [HttpPost("score")]
    [ProducesResponseType<ScoringResult>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<ScoringResult>> ScoreCustomer(ScoringRequest scoringRequest, [FromServices] DaprClient daprClient)
    {
        var request = new CreateInstanceWithResultRequest(Processes.CustomerScoring, null /* last */, true, "10s",
            null /* all root scoped back */, scoringRequest);
        
        logger.LogInformation("Process {BpmnProcessId} requested scoring for rating {Rating} and monthly income {Income}", 
            request.BpmnProcessId, scoringRequest.Rating, scoringRequest.MonthlyIncome);
        
        var response = await daprClient.InvokeBindingAsync<CreateInstanceWithResultRequest, CreateInstanceWithResultResponse>(
            Commands.ZeebeCommand, Commands.CreateInstance, request);

        logger.LogInformation("Process {BpmnProcessId} for instance {ProcessInstanceKey} finished with result: {Variables}", 
            response.BpmnProcessId, response.ProcessInstanceKey, response.Variables);
        
        var scoringResult = JsonSerializer.Deserialize<ScoringResult>(response.Variables!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        
        return Ok(scoringResult);
    }
}