using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;

namespace CodePlayground.Grpc.Test;

[TestFixture]
public class CodePlaygroundServiceTests
{
        private GrpcChannel _channel;
        private CodePlaygroundService.CodePlaygroundServiceClient _client;

        [SetUp]
        public void Setup()
        {
            // Assuming the server is running locally on port 5000
            _channel = GrpcChannel.ForAddress("http://localhost:5000");
            _client = new CodePlaygroundService.CodePlaygroundServiceClient(_channel);
        }

        [TearDown]
        public void Teardown()
        {
            _channel.Dispose();
        }

        [Test]
        public async Task Collaborate_ShouldExchangeMessages()
        {
            // Start the bidirectional stream
            using var call = _client.Collaborate();

            // Simulate receiving messages in the background
            var receivedUpdates = new List<CodeUpdate>();
            var responseTask = Task.Run(async () =>
            {
                await foreach (var update in call.ResponseStream.ReadAllAsync())
                {
                    receivedUpdates.Add(update);
                }
            });

            // Simulate sending messages
            var updatesToSend = new List<CodeUpdate>
            {
                new CodeUpdate
                {
                    ClientId = "client1",
                    Code = "console.log('First message');",
                    Operation = "insert",
                    Position = 0,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                },
                new CodeUpdate
                {
                    ClientId = "client2",
                    Code = "console.log('Second message');",
                    Operation = "insert",
                    Position = 23,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            };

            foreach (var update in updatesToSend)
            {
                await call.RequestStream.WriteAsync(update);
            }

            // Complete the request stream to signal the end of sending
            await call.RequestStream.CompleteAsync();

            // Wait for the response task to finish
            await responseTask;

            // Assert that the expected number of updates were received
            updatesToSend.Count.Should().Be(receivedUpdates.Count);

            // Further assertions can be made to check if the received updates match the sent updates
            for (int i = 0; i < updatesToSend.Count; i++)
            {
                updatesToSend[i].Code.Should().Be(receivedUpdates[i].Code);
                updatesToSend[i].Operation.Should().Be(receivedUpdates[i].Operation);
                updatesToSend[i].Position.Should().Be(receivedUpdates[i].Position);
                updatesToSend[i].ClientId.Should().Be(receivedUpdates[i].ClientId);
            }
        }
}