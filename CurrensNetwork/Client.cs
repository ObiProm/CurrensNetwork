using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;

namespace CurrensNetwork
{
    internal class Client
    {
        public delegate void _OnDataRecieved(string method, params object[] args);
        public delegate void _OnClientConnected();
        public delegate void _OnClientDisconnected();
        public delegate void _OnConnectionTerminated();

        public event _OnDataRecieved OnDataRecieved;
        public event _OnClientConnected OnClientConnected;
        public event _OnClientDisconnected OnClientDisconnected;
        public event _OnConnectionTerminated OnConnectionTerminated;

        public TcpClient client = new TcpClient();
        public NetworkStream stream;

        public BackgroundWorker dataReciever = new BackgroundWorker();

        public void Connect(string IP, int Port)
        {
            client = new TcpClient();
            client.Connect(IP, Port);
            stream = client.GetStream();

            OnClientConnected?.Invoke();

            dataReciever.DoWork += (s, e) => DataReciever();
            dataReciever.WorkerSupportsCancellation = true;
            dataReciever.RunWorkerAsync();
        }

        public void Disconnect()
        {
            OnClientDisconnected?.Invoke();
            stream.Close();
            client.Close();
        }

        private void DataReciever()
        {
            while (true)
            {
                if (client.Connected)
                {
                    try
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        Packet RecievedPacket = (Packet)formatter.Deserialize(stream);
                        string methodName = RecievedPacket.Name;
                        object[] args = RecievedPacket.Params;

                        Attribute[] attrs = Attribute.GetCustomAttributes(typeof(RPC));
                        OnDataRecieved?.Invoke(methodName, args);
                        foreach (Attribute attr in attrs)
                            attr.GetType().GetMethod(methodName)?.Invoke(this, args);
                    }
                    catch { Console.WriteLine("Error"); }
                }
                else
                {
                    Disconnect();
                    OnConnectionTerminated?.Invoke();
                    dataReciever = null;
                    break;
                }
            }
        }
        public void SendData(string method, params object[] args)
        {
            Packet packet = new Packet() { Name = method, Params = args };
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, packet);

        }
        public void SendData(Packet packet)
        {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, packet);
        }
    }
}