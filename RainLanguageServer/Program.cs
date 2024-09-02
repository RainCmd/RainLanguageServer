using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer
{
    internal class Program
    {
        [RequiresDynamicCode("Calls RainLanguageServer.Server.Server(Stream, Stream)")]
        static void Main(string[] args)
        {
#if DEBUG
            var plugin = Environment.CurrentDirectory;
            plugin = plugin.Substring(0, plugin.LastIndexOf("RainLanguagePlugin") + "RainLanguagePlugin".Length);
            var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            socket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.Socket, System.Net.Sockets.SocketOptionName.ReuseAddress, true);
            socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 14567));
            socket.Listen(1);
            var stream = new System.Net.Sockets.NetworkStream(socket.Accept());
            var log = File.CreateText(Path.Combine(plugin, "bin\\server.log"));
            var recorder = new RecorderStream(stream, log);
            var server = new Server(recorder, recorder, 10);
            server.OnTimeout += method => Console.WriteLine($"{method} 请求处理时间已超时");
            server.OnTimeoutRequestFinish += (method, time) => Console.WriteLine($"{method} 已完成，耗时 {time}ms");
            server.Listen().Wait();
            recorder?.Close();
#else
            var server = new Server(Console.OpenStandardInput(), Console.OpenStandardOutput());
            server.Listen().Wait();
#endif
        }
    }
}
