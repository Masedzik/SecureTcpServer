namespace SecureTcpServer;

using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using Helpers;
using Args;

public class SecureTcpServer
{
    public event EventHandler<ProcessMessageEventArgs>? OnProcessMessage;
    private short _port;
    private CryptoHelper _cryptoHelper;
    public SecureTcpServer(short Port, string Key, string Iv)
    {
        _port = Port;
        _cryptoHelper = new CryptoHelper(Key, Iv);
    }
    public async Task Start()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        while (true)
        {
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClient(client);
        }
    }
    public async Task HandleClient(TcpClient client)
    {
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream))
        using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
        {
            Channel<Response> channel = Channel.CreateBounded<Response>(new BoundedChannelOptions(100)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (SemaphoreSlim semaphore = new SemaphoreSlim(10))
            {
                Task? writerTask = WriterLoop(channel.Reader, writer, cts.Token);
                try
                {
                    while (true)
                    {
                        string line = await reader.ReadLineAsync();
                        if (line == null) break;
                        byte[] encrypted = Convert.FromBase64String(line);
                        string json = _cryptoHelper.Decrypt(encrypted);
                        await semaphore.WaitAsync();
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessMessage(json, channel.Writer);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        });
                    }
                }
                catch (IOException ex) when (ex.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.ConnectionReset)
                {
                }
                catch (Exception ex)
                {
                }
                cts.Cancel();
                channel.Writer.Complete();
                client.Close();
            }
        }
    }
    async Task WriterLoop(ChannelReader<Response> reader, StreamWriter writer, CancellationToken token)
    {
        try
        {
            await foreach (var response in reader.ReadAllAsync(token))
            {
                string json = JsonSerializer.Serialize(response);
                byte[] encrypted = _cryptoHelper.Encrypt(json);
                string base64 = Convert.ToBase64String(encrypted);
                await writer.WriteLineAsync(base64);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
        }
    }
    async Task ProcessMessage(string json, ChannelWriter<Response> writer)
    {
        Request? request = JsonSerializer.Deserialize<Request>(json);
        if (request?.Id == null) return;

        OnProcessMessage?.Invoke(this, new ProcessMessageEventArgs(request, writer));
    }
}