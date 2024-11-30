using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Lê a chave de API do sandbox do arquivo de configuração
var sandboxApiKey = builder.Configuration["Sandbox:ApiKey"];

// Adiciona o serviço de HttpClient usando IHttpClientFactory
builder.Services.AddHttpClient();

var app = builder.Build();

// Habilita arquivos estáticos
app.UseStaticFiles();

// Endpoint POST para realizar a transferência PIX
app.MapPost("api/pix", async (PixTransferDto dto, IHttpClientFactory httpClientFactory) =>
{
    // Criação do HttpClient usando IHttpClientFactory
    var client = httpClientFactory.CreateClient();
    
    // Configura os cabeçalhos necessários para a requisição
    client.DefaultRequestHeaders.Add("access_token", sandboxApiKey);
    client.DefaultRequestHeaders.Add("User-Agent", "PixPaymentIntegrationTest");

    // Criação do conteúdo da requisição (formulário)
    var postForm = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("value", dto.Value.ToString("N1")),
        new KeyValuePair<string, string>("pixAddressKey", dto.PixAddressKey),
        new KeyValuePair<string, string>("pixAddressKeyType", dto.PixAddressKeyType),
        new KeyValuePair<string, string>("operationType", "PIX"),
    });
    
    //Executa a requisição
    var response = await client.PostAsync("https://sandbox.asaas.com/api/v3/transfers", postForm);
    if (!response.IsSuccessStatusCode)
    {
        var errorResponse = JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync());
        return Results.BadRequest(errorResponse);
    }
    
    var transferResponse = JsonConvert.DeserializeObject<TransferResponse>(await response.Content.ReadAsStringAsync());
    return Results.Ok(transferResponse);
});

app.UseHttpsRedirection();
app.Run();

// Definição do DTO (Data Transfer Object) para a transferência Pix
public record PixTransferDto(string PixAddressKey, decimal Value, string PixAddressKeyType = "EMAIL");

// Resposta da API de transferência
public record TransferResponse(
    string Object,
    string Id,
    decimal Value,
    decimal NetValue,
    decimal TransferFee,
    string DateCreated,
    string Status,
    string? EffectiveDate,
    string? ConfirmedDate,
    string? EndToEndIdentifier,
    string? TransactionReceiptUrl,
    string OperationType,
    string? FailReason,
    string? WalletId,
    string? Description,
    bool CanBeCancelled,
    string? ExternalReference,
    bool Authorized,
    string ScheduleDate,
    string Type,
    BankAccount BankAccount,
    string? Recurring
);

// Detalhes da conta bancária envolvida na transferência
public record BankAccount(
    Bank Bank,
    string? AccountName,
    string OwnerName,
    string CpfCnpj,
    string Type,
    string Agency,
    string? AgencyDigit,
    string Account,
    string AccountDigit,
    string PixAddressKey
);

// Detalhes do banco envolvido na transferência
public record Bank(
    string? Code,
    string Name,
    string Ispb
);

// Resposta de erro da API externa
public record ErrorResponse(
    IList<Error> Errors
);

// Detalhes do erro retornado pela API externa
public record Error(
    string Code,
    string Description
);
