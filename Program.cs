// See https://aka.ms/new-console-template for more information
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Security.KeyVault.Secrets;


// Note: Start docker with the command below first before running this code.
// docker run --rm --name lowkey -d -p 8550:8443 nagyesta/lowkey-vault:2.6.15

await CreateSecret("https://localhost:8550/"); // using the exposed docker host port
await CreateSecret("https://localhost:8443/"); // using the low key vault container port


async Task CreateSecret(string vaultUrl)
{
    const string SecretName = "name";
    const string SecretValue = "value";

    var secretClient = new SecretClient(new Uri(vaultUrl), new NoopCredentials(), CreateSecretClientOption());

    try
    {
        await secretClient.SetSecretAsync(SecretName, SecretValue);

        Console.WriteLine($"Url: {vaultUrl} Create Secret Successful.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Url: {vaultUrl} Create Secret Failed. Error is {ex.Message}");
    }
}

SecretClientOptions CreateSecretClientOption()
{
    return GetClientOptions(new SecretClientOptions(SecretClientOptions.ServiceVersion.V7_4)
    {
        DisableChallengeResourceVerification = true,
        RetryPolicy = new RetryPolicy(0, DelayStrategy.CreateFixedDelayStrategy(TimeSpan.Zero))
    });
}

T GetClientOptions<T>(T options) where T : ClientOptions
{
    DisableSslValidationOnClientOptions(options);
    return options;
}

/// <summary>
/// Disables server certification callback.
/// <br/>
/// <b>WARNING: Do not use in production environments.</b>
/// </summary>
/// <param name="options"></param>
void DisableSslValidationOnClientOptions(ClientOptions options)
{
    options.Transport = new HttpClientTransport(CreateHttpClientHandlerWithDisabledSslValidation());
}

HttpClientHandler CreateHttpClientHandlerWithDisabledSslValidation()
{
    return new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
    //return new HttpClientHandler { ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; } };
}

internal class NoopCredentials : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new AccessToken("noop", DateTimeOffset.MaxValue);
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}