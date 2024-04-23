using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CurrensNetwork
{
    /// <summary>
    /// Represents a client for establishing connections to a remote server and handling data communication.
    /// </summary>
    public class Client
    {
        public delegate void _OnDataReceived(Packet packet);
        public delegate void _OnClientConnected();
        public delegate void _OnClientDisconnected();
        public delegate void _OnReceivingDataFailure(Exception exception);
        public delegate void _OnConnectionTerminated();
        public delegate void _OnDataReceiveProgress(int loaded);
        public delegate void _OnClientConnectionFailure(Exception exception);

        /// <summary>
        /// Event handler for data received from the remote server.
        /// </summary>
        public event _OnDataReceived OnDataReceived;
        /// <summary>
        /// Event handler for successful connection to the remote server.
        /// </summary>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Event handler for disconnection from the remote server.
        /// </summary>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Event handler for termination of the connection with the remote server.
        /// </summary>
        public event _OnConnectionTerminated OnConnectionTerminated;
        /// <summary>
        /// Event handler for failure to establish connection with the remote server.
        /// </summary>
        public event _OnClientConnectionFailure OnClientConnectionFailure;
        /// <summary>
        /// Event handler for failure to receive data from the remote server.
        /// </summary>
        public event _OnReceivingDataFailure OnReceivingDataFailure;
        
        /// <summary>
        /// Invokes on downloading data progress, returns count of readed bytes
        /// </summary>
        public event _OnDataReceiveProgress OnDataReceiveProgress;

        private TcpClient client = new TcpClient();
        private NetworkStream stream;

        /// <summary>
        /// Establishes a connection to a remote server with the specified IP address and port.
        /// </summary>
        /// <param name="IP">The IP address of the remote server.</param>
        /// <param name="Port">The port number for the connection.</param>
        public async Task Connect(string IP, int Port)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(IP, Port);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                OnClientConnectionFailure?.Invoke(ex);
                return;
            }

            Networking.ClientStream = stream;

            var ip = client.Client.LocalEndPoint as System.Net.IPEndPoint;
            Networking.SetID(ulong.Parse(ip.Address.MapToIPv4().ToString().Replace(".", "") + ip.Port.ToString()));

            OnClientConnected?.Invoke();

            await ReceiveDataAsync();
        }

        /// <summary>
        /// Disconnects the client from the remote connection.
        /// </summary>
        public void Disconnect()
        {
            OnClientDisconnected?.Invoke();
            stream.Close();
            client.Close();

            Networking.ClientStream = null;
        }

        private async Task ReceiveDataAsync()
        {
            while (client.Connected)
            {
                try
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;
                    StringBuilder stringBuilder = new StringBuilder();
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                    {
                        OnDataReceiveProgress?.Invoke(bytesRead);
                        stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        if (!stream.DataAvailable)
                            break;
                    }
                    string _prepacket = stringBuilder.ToString();
                    XmlSerializer serializer = new XmlSerializer(typeof(Packet));
                    StringReader reader = new StringReader(_prepacket);
                    Packet receivedPacket = (Packet)serializer.Deserialize(reader);

                    string methodName = receivedPacket.Name;
                    object[] args = receivedPacket.Params.ToArray();

                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    OnDataReceived?.Invoke(receivedPacket);
                    foreach (Assembly assembly in assemblies)
                    {
                        MethodInfo[] methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                            .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0).ToArray();

                        foreach (var method in methods)
                        {
                            if (method.Name == methodName && method.GetParameters().Length == args.Length)
                            {
                                var classInstance = Activator.CreateInstance(method.DeclaringType);
                                method.Invoke(classInstance, args);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnReceivingDataFailure?.Invoke(ex);
                }
            }
            Disconnect();
            OnConnectionTerminated?.Invoke();
        }
    }
}
