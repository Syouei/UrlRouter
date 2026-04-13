using System.IO.Pipes;
using System.Text;
using UrlRouter.Storage;

namespace UrlRouter.Windows;

internal static class SingleInstanceHelper
{
    private const string MutexName = "Global\\UrlRouter_SingleInstance_v1";
    private const string PipeName = "\\\\.\\pipe\\UrlRouter_IPC_v1";

    public static bool IsPrimaryInstance(out Mutex? mutex)
    {
        mutex = new Mutex(true, MutexName, out bool isNew);
        return isNew;
    }

    public static void SendUrlToRunningInstance(string url)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            try { client.Connect(500); } catch { }
            if (client.IsConnected)
            {
                var bytes = Encoding.UTF8.GetBytes(url + "\n");
                client.Write(bytes, 0, bytes.Length);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn("SingleInstanceHelper.SendUrl", ex.Message);
        }
    }

    public static void StartPipeServer(Action<string> onUrlReceived)
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                while (true)
                {
                    using var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1,
                        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                    server.WaitForConnection();
                    string? url;
                    using (var reader = new StreamReader(server, Encoding.UTF8))
                    {
                        url = reader.ReadLine();
                    }
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        onUrlReceived(url);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("SingleInstanceHelper.PipeServer", ex.Message);
            }
        });
    }
}
