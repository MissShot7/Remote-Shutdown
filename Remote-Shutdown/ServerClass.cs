using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Xml;
using System.Windows.Threading;
using System.Diagnostics;

namespace Remote_Shutdown
{
    public static class ServerClass
    {
        private static string serverIP;
        private static bool running = false;
        private static TcpListener tcpListener;
        public static string Password = "SafePassword";
        public static int TimeOutTime = 5;
        private static int ShutdownTime = 10;

        static List<(string IP, DateTime Timestamp)> TimeoutIPs = new List<(string, DateTime)>();
        static List<string> PermanentlyBanedIps = new List<string>();
        public static void NCL(string txt) //New console log
        {
            MainWindow.NCL(txt);
        }
        public static bool StartServer()
        {
            serverIP = GetLocalIPAddress(); //lokální ip
            if (serverIP == "Connecting_error") { NCL("Connection error, ensure your wifi is on"); return false; } //chyba s připojením
            if (running == true) { return false; }

            //start
            running = true;

            Thread serverThread = new Thread(RunServer);
            serverThread.Start();
            return true;
        }
        public static void StopServer()
        {
            running = false;
            tcpListener?.Stop();
            MainWindow.Instance.OnOffButton.Content = "Off";
            NCL("Server Stopped");
        }
        private static void RunServer()
        {
            int port = 8111;
            tcpListener = new TcpListener(IPAddress.Parse(serverIP), port);
            tcpListener.Start();

            NCL($"Server started on {serverIP}:{port}");


            while (running)
            {

                if (tcpListener.Pending())
                {
                    // Accept a new connection
                    TcpClient client = tcpListener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleRequest(client));
                    clientThread.Start();
                }
                else
                {
                    Thread.Sleep(100); // Prevent CPU spinning
                }
            }
        }
        private static void HandleRequest(TcpClient client)
        {
            RemoveOldEntriesFromTimeOutList();
            try
            {
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream))
                using (StreamWriter writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    string request = reader.ReadLine();
                    IPEndPoint remoteIpEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                    string ClientAdress = (client.Client.RemoteEndPoint as IPEndPoint).Address.ToString();
                    if (IsIPBaned(ClientAdress)) { NCL($"permanently banned ip {ClientAdress} tried to access"); return; } // IP is permanently banned
                    if (IsIPInList(ClientAdress)) { NCL($"Temporary blacklisted ip {ClientAdress} tried to access"); return; } // IP is temporary blacklisted
                    NCL($"Request: {request} from {ClientAdress}");

                    if (string.IsNullOrEmpty(request)) return;

                    string[] tokens = request.Split(' ');
                    if (tokens.Length < 2) return;

                    string path = tokens[1];

                    if (path.StartsWith("/RemoteShutdown", StringComparison.OrdinalIgnoreCase))
                    {
                        var queryParamsIndex = path.IndexOf("?password=");
                        if (queryParamsIndex != -1)
                        {
                            // Get the index where the password starts
                            int passwordStartIndex = queryParamsIndex + 10;
                            // Find the index of the next '&' if present
                            int nextParamIndex = path.IndexOf("&", passwordStartIndex);
                            // If '&' is found, extract the password substring up to '&', else get the full password string
                            string providedPassword = nextParamIndex != -1
                                ? path.Substring(passwordStartIndex, nextParamIndex - passwordStartIndex)
                                : path.Substring(passwordStartIndex);
                            // Now, extract the next parameter if present (after the '&')
                            string nextParam = nextParamIndex != -1
                                ? path.Substring(nextParamIndex + 1)  // Extract everything after the '&'
                                : string.Empty;  // No next parameter if no '&' found
                            
                            if (providedPassword == Password)
                            {
                                writer.WriteLine("HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n");//header

                                if (nextParam == "yes")
                                {
                                    writer.WriteLine($"<html><body><h1>Remote Shutdow</h1><p>Correct password and shutting down in {ShutdownTime} seconds</p></br></body><title>Remote Shutdown</title></html>");
                                    //shutdown
                                    var psi = new ProcessStartInfo("shutdown", $"/s /f /t {ShutdownTime}");
                                    psi.CreateNoWindow = true;
                                    psi.UseShellExecute = false;
                                    Process.Start(psi);
                                    NCL($"{ClientAdress} - shutting down");
                                    return;
                                }
                                NCL($"{ClientAdress} - correct password");
                                //StopServer();

                                writer.WriteLine(@"
                                                    <html>
                                                    <head>
                                                        <title>Remote Shutdown</title>
                                                    </head>
                                                    <body>
                                                        <h1>Remote Shutdown</h1>
                                                        <p>Correct password</p>
                                                        <br/>
                                                        <form id=""shutdownForm"" action="""">
                                                        <input type=""submit"" value=""Shutdown"" />
                                                    </form>

                                                    <script>
                                                        document.getElementById('shutdownForm').onsubmit = function(e) {
                                                            e.preventDefault(); // Prevent the form from submitting immediately
                                                            window.location.href = window.location.href + '&yes'; // Add ?yes to the current URL
                                                        };
                                                    </script>
                                                    </body>
                                                    </html>
                                                ");
                            }
                            else
                            {
                                NCL($"{ClientAdress} - wrong password ({providedPassword})");
                                writer.WriteLine("HTTP/1.1 403 Forbidden\r\nContent-Type: text/plain\r\n\r\nIncorrect password\n5 second ban");
                                //5 seconds timeout for ip
                                AddIPToTimeoutList(ClientAdress);
                            }
                        }
                        else //no ?password
                        {
                            writer.WriteLine("HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain\r\n\r\nPassword required");
                        }
                    }
                    else
                    {
                        writer.WriteLine("HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nServer on");
                    }
                }
            }
            catch (Exception ex)
            {
                NCL("Error handling request: " + ex.Message);
            }
            finally
            {
                client.Close();
            }
        }
        private static void AddIPToTimeoutList(string ip)
        {
            TimeoutIPs.Add((ip, DateTime.Now));
        }
        static void RemoveOldEntriesFromTimeOutList()
        {
            DateTime now = DateTime.Now;
            TimeoutIPs = TimeoutIPs.Where(entry => (now - entry.Timestamp).TotalSeconds <= TimeOutTime).ToList();
        }
        static bool IsIPInList(string ip) //temporary
        {
            return TimeoutIPs.Any(entry => entry.IP == ip);
        }
        static bool IsIPBaned(string ip) //permament
        {
            return PermanentlyBanedIps.Any(entry => entry == ip);
        }




        public static string GetLocalIPAddress()
        {
            //jestli je připojen k internetu


            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                try
                {
                    socket.Connect("8.8.8.8", 65530);
                }
                catch { return "Connecting_error"; }



                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }
    }
}
