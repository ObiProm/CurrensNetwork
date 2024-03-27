using CurrensNetwork;

namespace Chat
{
    internal class Chat
    {
        public static Client client = new();
        public static Host host = new();
        public static string Name = "Player";
        public static Dictionary<ulong, string> Users = new Dictionary<ulong, string>();
        public static void Main(string[] ars)
        {
            Random random = new Random();
            Name += random.Next(1000, 9999);
            Menu();
        }
        static void Menu()
        {
            string? input = Console.ReadLine()?.ToLower();

            if (input == "/host")
            {
                host = new Host();
                Console.Write("Enter port: ");
                host.OnHostCreated += () => { Console.WriteLine("---------Host started---------"); };
                host.Create(int.Parse(Console.ReadLine()));
                Users[1] = Name;
                Chatting();
            }
            else if (input == "/client")
            {
                client = new Client();
                Console.Write("Enter IP: ");
                var ip = Console.ReadLine();
                Console.Write("Enter port: ");
                client.OnConnectionTerminated += () => { Console.WriteLine("\n---------Host stopped connection!---------\n"); };
                client.OnClientConnected += () => { Networking.Rpc("WriteToChat", Name + " connected!"); Networking.RpcTo(1, "AddToUsersList", Networking.UniqueID, Name); Console.WriteLine("---------Connected---------"); };
                client.OnClientDisconnected += () => { Networking.Rpc("WriteToChat", Name + " disconnected!"); };
                client.OnClientConnectionFailture += () => { Console.WriteLine("Connection failed!"); Menu(); return; };
                client.Connect(ip, int.Parse(Console.ReadLine()));
                Chatting();
            }
            else if (input == "/setnick")
            {
                Console.Write("Enter new nickname: ");
                Name = Console.ReadLine();
                Menu();
            }
        }
        static void Chatting()
        {
            while (true)
            {
                string? input = Console.ReadLine();

                if (input == "/leave")
                    break;
                else if (input.StartsWith("/to"))
                {
                    Console.Write("Write personal message: ");
                    Networking.RpcTo(ulong.Parse(input.Split()[1]), "WriteToChat", "(Personal message) " + Name + ": " + Console.ReadLine());
                    continue;
                }
                // Does not work! If host will use it, he will kick user, but program will crash
                /*else if (input.StartsWith("/kick"))
                {
                    if (Networking.IsHost)
                    {
                        Console.Write("Write reason: ");
                        Networking.RpcTo(ulong.Parse(input.Split()[1]), "Kick", Console.ReadLine());
                        continue;
                    }
                    else
                        Console.WriteLine("You are not host!");
                }*/
                else if (input.StartsWith("/users"))
                {
                    Console.WriteLine("Connected users: " + Users.Count);
                    foreach(var user in Users)
                        Console.WriteLine(user.Value + " | ID: " + user.Key);
                    continue;
                }
                Networking.Rpc("WriteToChat", Name + ": " + input);
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Console.Write("\r" + new string(' ', Console.BufferWidth) + "\r");
                if(Networking.IsHost)
                    Console.WriteLine(Name + ": " + input);

            }
            
            Users.Clear();
            if (Networking.IsHost)
            {
                Console.WriteLine("Sending request to clients...");
                Menu();
                Networking.Rpc("Kick", "Host ended connection");
                host = null;
            }
            else
                client.Disconnect();
        }
        [RPC]
        public void WriteToChat(string input)
        {
            Console.WriteLine(input);
        }
        [RPC]
        public void AddToUsersList(ulong Id, string user)
        {
            Users[Id] = user;
            Networking.Rpc("Update", Users);
        }
        [RPC]
        public void DeleteFromUsersList(ulong Id)
        {
            Users.Remove(Id);
            Networking.Rpc("Update", Users);
        }
        // Updates list of users at all clients
        [RPC]
        public void Update(Dictionary<ulong, string> dic)
        {
            Users = dic;
        }
        [RPC]
        public void Kick(string reason)
        {
            Networking.Rpc("WriteToChat", Name + " was kicked! Reason " + reason);
            Console.WriteLine("---------Sesion ended---------");
            client.Disconnect();
        }
    }
}