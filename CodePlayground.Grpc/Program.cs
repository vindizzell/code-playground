using CodePlayground.Grpc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<CodePlaygroundService>();

app.MapGet("/", () => "");

app.Run();
