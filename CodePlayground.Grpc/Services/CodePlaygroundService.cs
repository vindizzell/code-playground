using System.Collections.Concurrent;
using Grpc.Core;

namespace CodePlayground.Grpc.Services;

public class CodePlaygroundService : Grpc.CodePlaygroundService.CodePlaygroundServiceBase
{
    private readonly ILogger<CodePlaygroundService> _logger;
    private readonly ConcurrentDictionary<string, IServerStreamWriter<CodeUpdate>> _clients;
    
    public CodePlaygroundService(ILogger<CodePlaygroundService> logger)
    {
        _logger = logger;
        _clients = new ConcurrentDictionary<string, IServerStreamWriter<CodeUpdate>>();
    }

    public override async Task Collaborate(
        IAsyncStreamReader<CodeUpdate> requestStream, 
        IServerStreamWriter<CodeUpdate> responseStream, 
        ServerCallContext context)
    {
        string clientId = null;

        try
        {
            await foreach (var update in requestStream.ReadAllAsync())
            {
                clientId = update.ClientId;

                _clients.TryAdd(clientId, responseStream);

                foreach (var client in _clients)
                {
                    if (client.Key != clientId)
                    {
                        await client.Value.WriteAsync(update);
                    }
                }
            }
        }
        finally
        {
            if (clientId != null)
            {
                _clients.TryRemove(clientId, out _);
            }
        }
    }
}