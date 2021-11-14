using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serialization.Extensions;
using UnityEngine;

public sealed class ServerListener
    {
        public event Action<EndPoint, Socket> OnConnectedClient;

        private Dictionary<int, Action<Socket, byte[]>> _events;
        private Dictionary<EndPoint, Socket> _receivers;

        private Socket _socket;
        private Thread _threadReceiveConected;
        private Thread _threadReceive;
        
        public ServerListener(string ip = "127.0.0.1", int port = 8006)
        {
            _events = new Dictionary<int, Action<Socket, byte[]>>();
            _receivers = new Dictionary<EndPoint, Socket>();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _socket.Bind(ipEndPoint);
                _socket.Listen((int) SocketOptionName.MaxConnections);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
            finally
            {
                Created = true;
                _threadReceiveConected = new Thread(new ThreadStart(ReceiveConnected));
                _threadReceive = new Thread(new ThreadStart(Receive));
                _threadReceiveConected.Start();
                _threadReceive.Start();
            }
        }

        public bool Created { get; private set; } = false;
        
        /// <summary>
        /// Метод для прослушивания новых подключений
        /// </summary>
        private void ReceiveConnected()
        {
            while (true)
            {
                try
                {
                    Socket handler = _socket.Accept();

                    if (!_receivers.ContainsKey(handler.RemoteEndPoint))
                        _receivers.Add(handler.RemoteEndPoint, handler);

                    OnConnectedClient?.Invoke(handler.RemoteEndPoint, handler);
                }
                catch (Exception e)
                {
                    if(e.Message.Length > 0) Debug.Log(e.Message);
                }
            }
        }
        
        /// <summary>
        /// Метод для прослушивания
        /// </summary>
        private void Receive()
        {
            while (true)
            {
                try
                {
                    foreach (var client in _receivers.Values.ToArray())
                    {
                        if(client.Available == 0) continue;
                        
                        List<byte> builder = new List<byte>();
                        byte[] buf = new byte[256];

                        do
                        {
                            int size = client.Receive(buf);

                            if (size < 256)
                            {
                                byte[] value = new byte[size];
                                Array.Copy(buf, 0, value, 0, size);
                                builder.AddRange(value);
                            }
                            else
                                builder.AddRange(buf);
                            
                        } 
                        while (client.Available > 0);
                        
                        byte[] data = builder.ToArray();

                        var msgid = (int)ObjectExtension.GetValue(typeof(int), data);
                        Debug.Log($"Server {msgid}");
                        
                        if (_events.ContainsKey(msgid))
                            Task.Run(() =>
                            {
                                _events[msgid]?.Invoke(client, data);
                            });
                    }
                }
                catch (Exception e)
                {
                    if(e.Message.Length > 0) Debug.Log(e.Message);
                }
            }
        }

        public void AddEvent(int msgid, Action<Socket, byte[]> action)
        {
            if (!_events.ContainsKey(msgid)) _events.Add(msgid, action);
            else _events[msgid] += action;
        }
        
        public void RemoveEvent(int msgid, Action<Socket, byte[]> action)
        {
            if (_events.ContainsKey(msgid))
                _events[msgid] -= action;
        }
        
        public async void SendAsync(Socket client, object data)
        {
            await Task.Run(() =>
            {
                client.Send(Serialization.BinarySerialization.Serialization(data));
            });
        }

        public void Close(Socket client)
        {
            if (!_receivers.ContainsKey(client.RemoteEndPoint)) return;
            
            // закрываем сокет
            client.Close();

            _receivers.Remove(client.RemoteEndPoint);
        }
        
        public void CloseAll()
        {
            if (_threadReceiveConected.IsAlive)
                _threadReceiveConected.Abort();
            if (_threadReceive.IsAlive)
                _threadReceive.Abort();
            
            _socket.Close();

            foreach (var client in _receivers)
            {
                // закрываем сокет
                client.Value.Close();
            }
            _receivers.Clear();
        }
    }
