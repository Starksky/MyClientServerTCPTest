using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Serialization.Extensions;
using UnityEngine;

namespace Client
{
    public class ClientListener
    {
        private Dictionary<int, Action<Socket, byte[]>> _events { get; }
        public bool Connected { get; private set; } = false;
        
        private Socket _socket;
        private Thread _threadReceive;
        
        public ClientListener(string ip = "127.0.0.1", int port = 8006)
        {
            _events = new Dictionary<int, Action<Socket, byte[]>>();
            
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                _socket.Connect(ipEndPoint);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
            finally
            {
                Connected = true;
                _threadReceive = new Thread(new ThreadStart(Receive));
                _threadReceive.Start();
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
                    if (_socket.Available == 0) continue;

                    List<byte> builder = new List<byte>();
                    
                    var buf = new byte[256];

                    do
                    {
                        _socket.Receive(buf, buf.Length, 0);
                        builder.AddRange(buf);
                    } while (_socket.Available > 0);

                    var data = builder.ToArray();
                    var msgid = (int)ObjectExtension.GetValue(typeof(int), data);
                    Debug.Log($"Client {msgid}");
                    
                    if(_events.ContainsKey(msgid))
                        Task.Run(() =>
                        {
                            _events[msgid]?.Invoke(_socket, data);
                        });
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
        
        public async void SendAsync(object data)
        {
            await Task.Run(() =>
            {
                _socket.Send(Serialization.BinarySerialization.Serialization(data));
            });
        }

        public void Close()
        {
            if (_threadReceive.IsAlive)
                _threadReceive.Abort();
            
            _socket.Close();
        }
    }
}