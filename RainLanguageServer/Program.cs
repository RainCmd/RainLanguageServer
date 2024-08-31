using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace RainLanguageServer
{
    internal class Program
    {
        [RequiresDynamicCode("Calls RainLanguageServer.Server.Server(Stream, Stream)")]
        static void Main(string[] args)
        {
#if DEBUG
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 14567));
            socket.Listen(1);
            var stream = new NetworkStream(socket.Accept());
            var log = File.CreateText("D:\\Projects\\CPP\\RainLanguage\\RainLanguagePlugin\\bin\\server.log");
            var recorder = new RecorderStream(stream, log);
            var server = new Server(recorder, recorder);
            server.Listen().Wait();
            recorder?.Close();
#else
            var server = new Server(Console.OpenStandardInput(), Console.OpenStandardOutput());
            server.Listen().Wait();
#endif
        }
    }
}
