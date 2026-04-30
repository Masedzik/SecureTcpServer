namespace SecureTcpServer.Helpers;

public class Request 
{ 
    public string Id { get; set; } 
    public ulong Discord { get; set; }
    public string Command { get; set; } 
    public string Param { get; set; }
}