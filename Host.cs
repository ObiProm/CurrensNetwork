using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace CurrensNetwork
{
    public class Host
    {
        public delegate void _OnClientConnected(TcpClient client);
        public delegate void _OnClientDisconnected(EndPoint endPoint);
        public delegate void _OnDataRecieved(string method, params object[] args);
        public delegate void _OnHostCreated();
        public delegate void _OnHostCreationFailture(Exception ex);

        public event _OnClientConnected OnClientConnected;
        public event _OnClientDisconnected OnClientDisconnected;
        public event _OnDataRecieved OnDataRecieved;
        public event _OnHostCreated OnHostCreated;
        public event _OnHostCreationFailture OnHostCreationFailture;

        private TcpListener listener;
        private Dictionary<EndPoint, TcpClient> connectedClients = new Dictionary<EndPoint, TcpClient>();

        private BackgroundWorker connectionsChecker = new BackgroundWorker();
        private BackgroundWorker dataReciever = new BackgroundWorker();
        public void Create(int Port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                Networking.IsHost = true;
            }
            catch (Exception ex)
            { 
                OnHostCreationFailture?.Invoke(ex);
            }
            Networking.UniqueID = 1;
            OnHostCreated?.Invoke();
            connectionsChecker.DoWork += (s, e) => ConnectionsChecker();
            connectionsChecker.RunWorkerAsync();

            dataReciever.DoWork += (s, e) => DataReciever();
            dataReciever.RunWorkerAsync();
        }

        private void ConnectionsChecker()
        {
            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                connectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                Networking.ConnectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                Networking.ClientIds.Add(ulong.Parse(tcpClient.Client.RemoteEndPoint.ToString().Replace(".", "").Replace(":", "")), tcpClient.Client.RemoteEndPoint);
                OnClientConnected?.Invoke(tcpClient);
            }
        }

        private void DataReciever()
        {
            while (true)
            {
                if (connectedClients.Count != 0)
                    foreach (var client in connectedClients.Keys.ToList())
                    {
                        if (connectedClients[client].Connected)
                        {
                            var stream = connectedClients[client].GetStream();
                            if (stream.DataAvailable)
                            {
                                BinaryFormatter formatter = new BinaryFormatter();
                                Packet RecievedPacket = (Packet)formatter.Deserialize(stream);
                                string methodName = RecievedPacket.Name;
                                object[] args = RecievedPacket.Params;
                                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                                foreach (Assembly assembly in assemblies)
                                {
                                    MethodInfo[] methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                                    .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0).ToArray(); // получаем долбанные методы

                                    foreach (var method in methods)
                                        if (method.Name == methodName && method.GetParameters().Length == args.Length)
                                        {
                                            if (RecievedPacket.SendTo == 0 || RecievedPacket.SendTo == 1)
                                            {
                                                var ClassInstance = Activator.CreateInstance(method.DeclaringType);
                                                method.Invoke(ClassInstance, args);
                                            }
                                        }
                                }
                                OnDataRecieved?.Invoke(methodName, args.ToArray());
                                if (RecievedPacket.SendTo == 0)
                                    Networking.Rpc(RecievedPacket);
                                else
                                    if(RecievedPacket.SendTo != 1)
                                        Networking.RpcTo(RecievedPacket.SendTo, RecievedPacket);
                            }
                        }
                        else
                        {
                            connectedClients.Remove(client);
                            OnClientDisconnected?.Invoke(client);
                        }

                    }
            }
        }
    }
}
