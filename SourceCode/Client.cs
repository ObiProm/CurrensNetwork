using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
        public delegate void _OnReceivingDataFailure(string error);
        public delegate void _OnConnectionTerminated();
        public delegate void _OnDataReceiveProgress(int loaded);
        public delegate void _OnClientConnectionFailure(string error);


        /// <summary>
        /// Event handler for data received from the host.
        /// </summary>
        public event _OnDataReceived OnDataReceived;
        /// <summary>
        /// Event handler for successful connection to the remote server.
        /// </summary>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Event handler for disconnection from the host.
        /// </summary>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Event handler for termination of the connection with the host.
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


        public int ConnectTimeout { get; set; } = 10000;

        private TcpClient client = new TcpClient();
        private NetworkStream stream;

        /// <summary>
        /// Establishes a connection to a remote server with the specified IP address and port.
        /// </summary>
        /// <param name="IP">The IP address of the remote server.</param>
        /// <param name="Port">The port number for the connection.</param>
        public void Connect(string IP, int Port)
        {
            if (Port < 0 || Port > 65536)
                throw new ArgumentOutOfRangeException("Port", $"Port value must be between 1 and 65536");
            try
            {
                client = new TcpClient();
                if (!client.ConnectAsync(IP, Port).Wait(ConnectTimeout))
                {
                    OnClientConnectionFailure?.Invoke("Time limit expired");
                    return;
                }
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                OnClientConnectionFailure?.Invoke(ex.Message);
                return;
            }

            Networking.ClientStream = stream;
            Networking.Client = this;
            Networking.Host = null;
            Networking.NetworkState = NetworkState.Client;

            var ip = client.Client.LocalEndPoint as IPEndPoint;
            Networking.UniqueID = ulong.Parse(ip.Address.MapToIPv4().ToString().Replace(".", "") + ip.Port.ToString());

            OnClientConnected?.Invoke();

            _ = Task.Run(async () => await DataReciever());
        }
        /// <summary>
        /// Establishes a connection to a remote server with the specified IP address and port.
        /// </summary>
        /// <param name="IP">The IP address of the remote server.</param>
        /// <param name="Port">The port number for the connection.</param>
        public void Connect(IPAddress IP, int Port)
        {
            if (Port < 0 || Port > 65536)
                throw new ArgumentOutOfRangeException($"Port value must be between 1 and 65536");
            try
            {
                client = new TcpClient();
                if (!client.ConnectAsync(IP, Port).Wait(ConnectTimeout))
                {
                    OnClientConnectionFailure?.Invoke("Time limit expired");
                    return;
                }
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                OnClientConnectionFailure?.Invoke(ex.Message);
                return;
            }

            Networking.ClientStream = stream;
            Networking.Client = this;
            Networking.Host = null;
            Networking.NetworkState = NetworkState.Client;

            var ip = client.Client.LocalEndPoint as IPEndPoint;
            Networking.UniqueID = ulong.Parse(ip.Address.MapToIPv4().ToString().Replace(".", "") + ip.Port.ToString());

            OnClientConnected?.Invoke();

            _ = Task.Run(async () => await DataReciever());
        }

        /// <summary>
        /// Disconnects the client from the remote connection.
        /// </summary>
        public void Disconnect()
        {
            Networking.NetworkState = NetworkState.None;
            OnClientDisconnected?.Invoke();
            if (client == null)
                throw new Exception("Client is already disconnected!");

            try { stream.Close(); } catch { }
            try { client.Close(); } catch { }
            
            Networking.ClientStream = null;
            Networking.Client = null;
        }
        /// <summary>
        /// Gets <see cref="NetworkStream"/> of <see cref="TcpClient"/>
        /// </summary>
        /// <returns></returns>
        public NetworkStream GetStream() { return stream; }
        private async Task<Packet> ReceivePacketAsync()
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
            return (Packet)serializer.Deserialize(reader);
        }
        private async Task DataReciever()
        {
            while (client.Connected)
            {
                try
                {
                    Packet receivedPacket = ReceivePacketAsync().Result;
                    Networking.InvokeRpcMethod(receivedPacket);
                }
                catch (Exception ex)
                {
                    OnReceivingDataFailure?.Invoke(ex.Message);
                }
            }
            Disconnect();
            OnConnectionTerminated?.Invoke();
        }
    }
}
