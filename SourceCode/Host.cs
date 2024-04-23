using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Collections;
using System.Text;
using System.Xml;

namespace CurrensNetwork
{
    /// <summary>
    /// Represents a host for managing network connections and data communication.
    /// </summary>
    public class Host
    {
        public delegate void _OnClientConnected(ulong ID, TcpClient client);
        public delegate void _OnClientDisconnected(ulong ID);
        public delegate void _OnDataRecieved(Packet packet);
        public delegate void _OnHostCreated();
        public delegate void _OnHostCreationFailure(Exception ex);
        public delegate void _OnDataReceiveProgress(int loaded);

        /// <summary>
        /// Event that occurs when a client is connected.
        /// </summary>
        /// <returns><see cref="TcpClient"/> of connected client</returns>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Event that occurs when a client is disconnected.
        /// </summary>
        /// <returns><see cref="ulong"/> - ID of disconected client</returns>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Event that occurs when data is received.
        /// </summary>
        /// <returns><see cref="Packet"/> object containing the received data</returns>
        public event _OnDataRecieved OnDataRecieved;
        /// <summary>
        /// Event that occurs when a host is succesfully created.
        /// </summary>
        public event _OnHostCreated OnHostCreated;
        /// <summary>
        /// Event that occurs when host creation fails.
        /// </summary>
        /// <returns><see cref="Exception"/> - reason if Failure </returns>
        public event _OnHostCreationFailure OnHostCreationFailure;
        /// <summary>
        /// Invokes on downloading data progress, returns count of readed bytes
        /// </summary>
        public event _OnDataReceiveProgress OnDataReceiveProgress;

        private TcpListener listener;
        private Dictionary<EndPoint, TcpClient> connectedClients = new Dictionary<EndPoint, TcpClient>();

        private BackgroundWorker connectionsChecker = new BackgroundWorker();

        public object RecievedPacket { get; private set; }

        /// <summary>
        /// Creates a host on the specified port to accept incoming connections.
        /// </summary>
        /// <param name="Port">The port number for hosting the server.</param>
        public async void Create(int Port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                Networking.SetIsHost(true);
            }
            catch (Exception ex)
            {
                OnHostCreationFailure?.Invoke(ex);
            }
            Networking.SetID(1);
            OnHostCreated?.Invoke();
            connectionsChecker.DoWork += (s, e) => ConnectionsChecker();
            connectionsChecker.RunWorkerAsync();

            await DataReciever();
        }

        private void ConnectionsChecker()
        {
            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                connectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                ulong ID = ulong.Parse(tcpClient.Client.RemoteEndPoint.ToString().Replace(".", "").Replace(":", ""));
                Networking.ConnectedClients.Add(ID, tcpClient);
                OnClientConnected?.Invoke(ID, tcpClient);
            }
        }

        private async Task DataReciever()
        {
            while (true)
            {
                if (connectedClients.Count != 0)
                {
                    foreach (var client in connectedClients.Keys.ToList())
                    {
                        if (connectedClients[client].Connected)
                        {
                            var stream = connectedClients[client].GetStream();
                            if (stream.DataAvailable)
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
                                foreach (Assembly assembly in assemblies)
                                {
                                    MethodInfo[] methods = assembly.GetTypes()
                                        .SelectMany(t => t.GetMethods())
                                        .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0)
                                        .ToArray();

                                    foreach (var method in methods)
                                    {
                                        if (method.Name == methodName && method.GetParameters().Length == args.Length)
                                        {
                                            if (receivedPacket.SendTo == 0 || receivedPacket.SendTo == 1)
                                            {
                                                object classInstance = Activator.CreateInstance(method.DeclaringType);
                                                method.Invoke(classInstance, args);
                                            }
                                        }
                                    }
                                }

                                OnDataRecieved?.Invoke(receivedPacket);
                                if (receivedPacket.SendTo == 0)
                                    Networking.Rpc(receivedPacket);
                                else
                                    if (receivedPacket.SendTo != 1)
                                    Networking.RpcTo(receivedPacket.SendTo, receivedPacket);
                            }
                        }
                        else
                        {
                            connectedClients.Remove(client);
                            OnClientDisconnected?.Invoke(ulong.Parse(client.ToString().Replace(".", "").Replace(":", "")));
                        }
                    }
                }
            }
        }
    }
}
