using Dapr.Client;
using ScoringAPI.Models;

namespace ScoringAPI.Repositories;

public class CustomerRepository(DaprClient daprClient)
{
    private const string StateStoreName = "statestore";

    public Task AddAsync(Customer model)
    {
        var key = $"customer-{model.CustomerId}";
        return daprClient.SaveStateAsync(StateStoreName, key, model);
    }

    public async Task<Customer?> GetAsync(string customerId)
    {
        var key = $"customer-{customerId}";
        var model = await daprClient.GetStateAsync<Customer>(StateStoreName, key);
        return model;
    }
}