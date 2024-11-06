using System.Net;
using System.Text.Json;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ReadProjectListOnDynamoDbLambda;

public class Function
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;

    public Function()
    {
        _dynamoDbClient = new AmazonDynamoDBClient(RegionEndpoint.USEast1);
    }
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        ScanResponse? response = null;
        
        int statusCode = 200;

        context.Logger.LogInformation("Lendo dados na tabela projects do DynamoDB...");
        
        try
        {
            var scanRequest = new ScanRequest
            {
                TableName = "projects"
            };

            try
            {
                response = await _dynamoDbClient.ScanAsync(scanRequest);
            }
            catch (ResourceNotFoundException ex)
            {
                context.Logger.LogError($"Tabela não encontrada: {ex.Message}");
            }
            catch (AmazonDynamoDBException ex)
            {
                context.Logger.LogError($"Erro ao acessar o DynamoDB: {ex.Message}");
            }
            
            var items = new List<Dictionary<string, string>>();

            if (response != null && response.Items.Any())
            {
                foreach (var item in response.Items)
                {
                    var projectItem = new Dictionary<string, string>();

                    foreach (var attribute in item)
                    {
                        if (attribute.Key.ToLower() == "id")
                        {
                            projectItem[attribute.Key] = attribute.Value.N;
                        }
                        else
                        {
                            projectItem[attribute.Key] = attribute.Value.S;    
                        }
                    }
                
                    items.Add(projectItem);
                    context.Logger.LogInformation("Dados lidos com sucesso na tabela projects do DynamoDB!");
                }

                statusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                statusCode = (int)HttpStatusCode.NotFound;
            }
            
            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Body = JsonSerializer.Serialize(items),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" },
                    {
                        "Access-Control-Allow-Headers",
                        "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token"
                    },
                    { "Access-Control-Allow-Methods", "GET,POST,OPTIONS" },
                    { "Access-Control-Allow-Credentials", "true" }
                }
            };
        }
        catch (Exception ex)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = $"Erro ao ler dados na tabela do DynamoDB: {ex.Message}"
            };
        }
    }
}