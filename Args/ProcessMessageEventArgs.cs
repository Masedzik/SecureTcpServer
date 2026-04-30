namespace SecureTcpServer.Args;

using System.Threading.Channels;
using Helpers;

public class ProcessMessageEventArgs : EventArgs
{
    public Request request { get; }
    public ChannelWriter<Response> writer { get; }

    public ProcessMessageEventArgs(Request request, ChannelWriter<Response> writer)
    {
        this.request = request;
        this.writer = writer;
    }
}
