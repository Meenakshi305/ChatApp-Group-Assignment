using ChatAppApi.Data;
using ChatAppApi.Model;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChatAppApi.Hubs
{
    public class ChatWebSocketHandler
    {
        private readonly ChatDbContext _context;
        private static readonly Dictionary<WebSocket, UserInfo> ConnectedUsers = new();
        private static readonly Dictionary<string, HashSet<WebSocket>> Groups = new();

        public ChatWebSocketHandler(ChatDbContext context) => _context = context;

        public async Task HandleAsync(WebSocket socket)
        {
            var buffer = new byte[8192];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        ConnectedUsers.Remove(socket);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                        return;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessage(socket, json);
                }
            }
            catch { ConnectedUsers.Remove(socket); }
        }

        private async Task ProcessMessage(WebSocket socket, string json)
        {
            var data = JsonDocument.Parse(json).RootElement;
            var action = data.GetProperty("action").GetString();

            switch (action)
            {
                case "register":
                    await RegisterUser(socket, data.GetProperty("username").GetString()!);
                    break;
                case "privateMessage":
                    await SendPrivateMessage(socket, data.GetProperty("to").GetString()!, data.GetProperty("message").GetString()!);
                    break;
                case "privateFile":
                    await SendPrivateFile(socket, data.GetProperty("to").GetString()!, data.GetProperty("fileName").GetString()!, data.GetProperty("base64").GetString()!);
                    break;
                case "joinGroup":
                    await JoinGroup(socket, data.GetProperty("group").GetString()!);
                    break;
                case "groupMessage":
                    await SendGroupMessage(socket, data.GetProperty("group").GetString()!, data.GetProperty("message").GetString()!);
                    break;
                case "groupFile":
                    await SendGroupFile(socket, data.GetProperty("group").GetString()!, data.GetProperty("fileName").GetString()!, data.GetProperty("base64").GetString()!);
                    break;
                case "listOnlineUsers":
                    await SendOnlineUsers(socket);
                    break;
            }
        }

        private async Task RegisterUser(WebSocket socket, string username)
        {
            var rsa = RSA.Create(4096);
            ConnectedUsers[socket] = new UserInfo
            {
                Username = username,
                Socket = socket,
                PublicKey = rsa,
                PrivateKey = rsa
            };

            if (await _context.Users.FirstOrDefaultAsync(u => u.Username == username) == null)
            {
                _context.Users.Add(new User
                {
                    Username = username,
                    PublicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey())
                });
                await _context.SaveChangesAsync();
            }

            await SendJson(socket, new { type = "registered", username, publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey()) });
        }

        private async Task SendPrivateMessage(WebSocket fromSocket, string toUsername, string message)
        {
            if (!ConnectedUsers.TryGetValue(fromSocket, out var sender)) return;
            var receiver = ConnectedUsers.Values.FirstOrDefault(u => u.Username == toUsername);
            if (receiver == null)
            {
                await SendJson(fromSocket, new { type = "error", text = "User not found" });
                return;
            }

            string encrypted = EncryptMessage(message, receiver.PublicKey);
            string signature = SignMessage(message, sender.PrivateKey);

            await SendJson(fromSocket, new { type = "private", to = toUsername, text = message, signature });

            if (receiver.Socket.State == WebSocketState.Open)
            {
                string decrypted = DecryptMessage(encrypted, receiver.PrivateKey);
                await SendJson(receiver.Socket, new { type = "private", from = sender.Username, text = decrypted, signature });
            }
        }

        private async Task SendPrivateFile(WebSocket fromSocket, string toUsername, string fileName, string base64)
        {
            if (!ConnectedUsers.TryGetValue(fromSocket, out var sender)) return;
            var receiver = ConnectedUsers.Values.FirstOrDefault(u => u.Username == toUsername);
            if (receiver == null)
            {
                await SendJson(fromSocket, new { type = "error", text = "User not found" });
                return;
            }

            string signature = SignMessage(base64, sender.PrivateKey);

            await SendJson(fromSocket, new { type = "privateFile", to = toUsername, fileName, base64, signature });

            if (receiver.Socket.State == WebSocketState.Open)
            {
                byte[] fileBytes = Convert.FromBase64String(base64);
                string encryptedBase64 = Convert.ToBase64String(receiver.PublicKey.Encrypt(fileBytes, RSAEncryptionPadding.OaepSHA256));
                byte[] decryptedBytes = receiver.PrivateKey.Decrypt(Convert.FromBase64String(encryptedBase64), RSAEncryptionPadding.OaepSHA256);
                string decryptedBase64 = Convert.ToBase64String(decryptedBytes);

                await SendJson(receiver.Socket, new { type = "privateFile", from = sender.Username, fileName, base64 = decryptedBase64, signature });
            }

            Directory.CreateDirectory("PrivateUploads");
            await File.WriteAllBytesAsync(Path.Combine("PrivateUploads", fileName), Convert.FromBase64String(base64));
        }

        private async Task JoinGroup(WebSocket socket, string group)
        {
            if (!Groups.ContainsKey(group)) Groups[group] = new HashSet<WebSocket>();
            Groups[group].Add(socket);
            await SendJson(socket, new { type = "joinedGroup", group });
        }

        private async Task SendGroupMessage(WebSocket socket, string group, string message)
        {
            if (!ConnectedUsers.TryGetValue(socket, out var sender)) return;
            if (!Groups.TryGetValue(group, out var members) || !members.Contains(socket))
            {
                await SendJson(socket, new { type = "error", text = $"You are not a member of group '{group}'" });
                return;
            }

            string signature = SignMessage(message, sender.PrivateKey);

            foreach (var memberSocket in members)
            {
                if (memberSocket.State == WebSocketState.Open && ConnectedUsers.TryGetValue(memberSocket, out var member))
                {
                    string encrypted = Convert.ToBase64String(member.PublicKey.Encrypt(Encoding.UTF8.GetBytes(message), RSAEncryptionPadding.OaepSHA256));
                    string decrypted = Encoding.UTF8.GetString(member.PrivateKey.Decrypt(Convert.FromBase64String(encrypted), RSAEncryptionPadding.OaepSHA256));

                    await SendJson(memberSocket, new { type = "groupMessage", group, from = sender.Username, text = decrypted, signature });
                }
            }
        }

        private async Task SendGroupFile(WebSocket socket, string group, string fileName, string base64)
        {
            if (!ConnectedUsers.TryGetValue(socket, out var sender)) return;
            if (!Groups.TryGetValue(group, out var members) || !members.Contains(socket))
            {
                await SendJson(socket, new { type = "error", text = $"You are not a member of group '{group}'" });
                return;
            }

            string signature = SignMessage(base64, sender.PrivateKey);

            foreach (var memberSocket in members)
            {
                if (memberSocket.State == WebSocketState.Open && ConnectedUsers.TryGetValue(memberSocket, out var member))
                {
                    byte[] fileBytes = Convert.FromBase64String(base64);
                    string encryptedBase64 = Convert.ToBase64String(member.PublicKey.Encrypt(fileBytes, RSAEncryptionPadding.OaepSHA256));
                    byte[] decryptedBytes = member.PrivateKey.Decrypt(Convert.FromBase64String(encryptedBase64), RSAEncryptionPadding.OaepSHA256);
                    string decryptedBase64 = Convert.ToBase64String(decryptedBytes);

                    await SendJson(memberSocket, new { type = "groupFile", group, from = sender.Username, fileName, base64 = decryptedBase64, signature });
                }
            }

            Directory.CreateDirectory("GroupUploads");
            await File.WriteAllBytesAsync(Path.Combine("GroupUploads", fileName), Convert.FromBase64String(base64));
        }

        private async Task SendOnlineUsers(WebSocket socket)
        {
            var onlineUsers = ConnectedUsers.Values.Select(u => u.Username).OrderBy(u => u).ToList();
            await SendJson(socket, new { type = "onlineUsers", users = onlineUsers });
        }

        private static string EncryptMessage(string message, RSA publicKey) =>
            Convert.ToBase64String(publicKey.Encrypt(Encoding.UTF8.GetBytes(message), RSAEncryptionPadding.OaepSHA256));

        private static string DecryptMessage(string ciphertext, RSA privateKey) =>
            Encoding.UTF8.GetString(privateKey.Decrypt(Convert.FromBase64String(ciphertext), RSAEncryptionPadding.OaepSHA256));

        private static string SignMessage(string message, RSA privateKey) =>
            Convert.ToBase64String(privateKey.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));

        private static async Task SendJson(WebSocket socket, object obj)
        {
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private class UserInfo
        {
            public string Username { get; set; }
            public WebSocket Socket { get; set; }
            public RSA PublicKey { get; set; }
            public RSA PrivateKey { get; set; }
        }
    }
}
