using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using SuperSimpleTcp;
using WatsonWebsocket;


namespace kiss_ws_bridge
{
    class Program
    {
        static int state = 0;
        static SimpleTcpClient kissTCPClient;
        static WatsonWsServer wsServer;

        static void printHelp()
        {
            Console.WriteLine("Usage: kiss-ws-bridge <kiss host> <kiss port>");
            Console.WriteLine("");
            Console.WriteLine("Example: kiss-ws-bridge 127.0.0.1 8001");
            Console.WriteLine("");
        }

        static void Main(string[] args)
        {
            // require two parameters
            if (args.Length != 2)
            {
                printHelp();
                return;
            }

            string kiss_host = args[0];
            int kiss_port = Int32.Parse(args[1]);

            while (true)
            {
                switch (state)
                {
                    case 0: // connect to kiss server
                        Console.WriteLine("Connecting to KISS - " + kiss_host + ":" + kiss_port.ToString());

                        kissTCPClient = new SimpleTcpClient(kiss_host, kiss_port);
                        kissTCPClient.Events.Connected += Events_Connected;
                        kissTCPClient.Events.Disconnected += Events_Disconnected;
                        kissTCPClient.Events.DataReceived += Events_DataReceived;

                        try
                        {
                            kissTCPClient.Connect();
                        }
                        catch (Exception Ex)
                        {
                            Console.WriteLine("Failed: " + Ex.ToString());
                            state = 0;
                        }

                        break;

                    case 1: // create websocket server
                        int wsport = 52500;

                        Console.WriteLine("Starting Websocket Server : " + wsport.ToString());

                        if (wsServer == null)
                        {
                            wsServer = new WatsonWsServer("127.0.0.1", 52500, false);
                            wsServer.ClientConnected += WsServer_ClientConnected;
                            wsServer.ClientDisconnected += WsServer_ClientDisconnected;

                            try
                            {
                                wsServer.Start();
                                Console.WriteLine("WS Started");
                            }
                            catch (Exception Ex)
                            {
                                Console.WriteLine("Failed: " + Ex.ToString());
                                state = 1;
                            }
                       }

                        state = 2;

                        break;

                    case 2: //nothing to do, events will handle it all
                        break;
                }
            }

        }

        private static void WsServer_ClientDisconnected(object sender, DisconnectionEventArgs e)
        {
            Console.WriteLine("WS Client Discconnected");
        }

        private static void WsServer_ClientConnected(object sender, WatsonWebsocket.ConnectionEventArgs e)
        {
            Console.WriteLine("WS Client Connected");
        }

        static async void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine("KISS Packet Received: " + e.Data.Count);
            Console.WriteLine("Array Size: " + e.Data.Array.Length);

            byte[] buffer = new byte[e.Data.Count];

            string kissPacket = "";
            for (int c = 0; c < e.Data.Count; c++)
            {
                kissPacket += string.Format("{0:X}", e.Data.Array[c]).PadLeft(2, '0');
                buffer[c] = e.Data.Array[c];
            }

            Console.WriteLine(kissPacket);

            if (wsServer != null)
            {
                Console.WriteLine("WS Clients: " + wsServer.ListClients().Count());

                foreach (var client in wsServer.ListClients())
                {
                    await wsServer.SendAsync(client.Guid, buffer);
                }
            }
            else
            {
                Console.WriteLine("Ws Server null ?");
            }
        }

        static void Events_Disconnected(object sender, SuperSimpleTcp.ConnectionEventArgs e)
        {
            // kiss disconnected            
            Console.WriteLine("KISS Disconnected");
            state = 0;
        }

        static void Events_Connected(object sender, SuperSimpleTcp.ConnectionEventArgs e)
        {
            // kiss connected
            Console.WriteLine("KISS Connected");
            state = 1;
        }
    }
}
