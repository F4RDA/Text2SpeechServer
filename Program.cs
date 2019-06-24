using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Speech.Text;
using Microsoft.Speech.Synthesis;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using IniParser;
using IniParser.Model;

namespace speech
{
    class Program
    {
        
        public static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            Console.WriteLine("Starting...");
            var server = new TcpListener(IPAddress.Parse("0.0.0.0"), 56000);
            server.Start();
            Console.WriteLine("Started.");
            RWControl.writeTofile(DateTime.Now + " - Application is started");
            while (true)
            {
                var client = await server.AcceptTcpClientAsync().ConfigureAwait(false);
                var cw = new ClientWorking(client, true);
                string clientIPaddress = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                if (clientControl.IsThereAccess(clientIPaddress))
                {
                    Task.Run((Func<Task>)cw.DoClientAsync);
                }
                else
                {
                    RWControl.writeTofile(DateTime.Now + "[" + clientIPaddress + "]" + " - " + "Connection rejected!");
                }
            }
        }
    }
    class clientControl
    {
        public static bool IsThereAccess(string _ipaddress)
        {
            bool status = false;
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("Configuration.ini");
            string[] allowedIPAddress = data["client"]["allowedIPAddress"].Split(';');
            if (allowedIPAddress.Contains(_ipaddress))
            {
                status = true;
            }
            else
            {
                status = false;
            }
            return status;
        }
    }

    class RWControl
    {
        public static void writeTofile(string _text)
        {
            string logpath = AppDomain.CurrentDomain.BaseDirectory + "\\logs";
            if (!Directory.Exists(logpath))
            {
                Directory.CreateDirectory(logpath);
            }
            string logfilepath = AppDomain.CurrentDomain.BaseDirectory + "\\logs\\SpeechLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(logfilepath))
            {
                using (StreamWriter sw = File.CreateText(logfilepath))
                {
                    sw.WriteLine(_text);
                }
            }
            else
            {
                using (FileStream fs = new FileStream(logfilepath, FileMode.Append, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(_text);
                }
            }
        }
    }
    class ClientWorking
    {
        TcpClient _client;
        bool _ownsClient;
        string _clientIPaddress;

        public ClientWorking(TcpClient client, bool ownsClient)
        {
            _client = client;
            _ownsClient = ownsClient;
            _clientIPaddress = ((IPEndPoint)_client.Client.RemoteEndPoint).Address.ToString();

        }

        private async Task konus(string _gelenText)
        {
            SpeechSynthesizer synth = new SpeechSynthesizer();
            synth.SetOutputToDefaultAudioDevice();
            synth.Speak(_gelenText);
            RWControl.writeTofile(DateTime.Now + " - " + "[" + _clientIPaddress + "]" + _gelenText);

        }

        public async Task DoClientAsync()
        {
                try
                {
                    using (var stream = _client.GetStream())
                    {
                        using (var sr = new StreamReader(stream))
                        using (var sw = new StreamWriter(stream))
                        {
                            await sw.WriteLineAsync("Connection accepted!").ConfigureAwait(false);

                            RWControl.writeTofile(DateTime.Now + " - " + "[" + _clientIPaddress + "]" + " - " + "Connection accepted!");
                            await sw.FlushAsync().ConfigureAwait(false);
                            var data = default(string);
                            while (!((data = await sr.ReadLineAsync().ConfigureAwait(false)).Equals("exit", StringComparison.OrdinalIgnoreCase)))
                            {
                                await sw.WriteLineAsync(data).ConfigureAwait(false);
                                await konus(data).ConfigureAwait(false);
                                await sw.FlushAsync().ConfigureAwait(false);
                            }
                        }

                    }
                }
                finally
                {
                    if (_ownsClient && _client != null)
                    {
                        (_client as IDisposable).Dispose();
                        _client = null;
                        RWControl.writeTofile(DateTime.Now + " - " + "[" + _clientIPaddress + "]" + " - " + "Connection disconnected!");
                    }
                }
        }
    }

   
}
