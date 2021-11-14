using System;
using System.Collections.Generic;
using System.Net.Sockets;
using Client;
using Serialization.Extensions;
using UnityEngine;

namespace DefaultNamespace
{
    public sealed class ClientServer : MonoBehaviour
    {
        private ServerListener _server;
        private ClientListener _client;

        private void Awake()
        {
            _server = new ServerListener();
            _client = new ClientListener();
            
            _server.AddEvent(1000, OnAuthServer);
            _client.AddEvent(1000, OnAuthClient);

            var data = new Dictionary<string, object>();
            data.Add("login", "test@mail.ru");
            data.Add("password", "123456");
            
            if(_client.Connected)
                _client.SendAsync(new Pack(1000, data));
        }
        
        private void OnAuthServer(Socket socket, byte[] data)
        {
            var pack = Serialization.BinarySerialization.Deserialization<Pack>(data);
            
            var serData = pack.GetData<Dictionary<string, object>>();
            foreach (var item in serData)
                Debug.Log($"{item.Key} = {item.Value.GetValue<string>()}");
            
            var answer = new Dictionary<string, object>();
            answer.Add("success", true);
            _server.SendAsync(socket, new Pack(1000, answer));
        }
        
        private void OnAuthClient(Socket socket, byte[] data)
        {
            var pack = Serialization.BinarySerialization.Deserialization<Pack>(data);
            var serData = pack.GetData<Dictionary<string, object>>();
            foreach (var item in serData)
                Debug.Log($"{item.Key} = {item.Value.GetValue<bool>()}");
        }
    }
}