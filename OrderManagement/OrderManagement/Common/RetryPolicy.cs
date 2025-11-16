using Polly;
using Polly.Extensions.Http;

namespace OrderManagement.Common;

public static class RetryPolicy
{
    public static IAsyncPolicy<HttpResponseMessage> Create()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                retryAttempt)));
    }
}