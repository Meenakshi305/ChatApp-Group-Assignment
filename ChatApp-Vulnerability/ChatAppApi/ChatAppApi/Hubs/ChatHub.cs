//using ChatAppApi.Data;
//using ChatAppApi.Model;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using System.Security.Cryptography;
//using System.Text;

//namespace ChatAppApi.Hubs
//{
//    public class ChatHub : Hub
//    {
//        private readonly ChatDbContext _context;
//        private static Dictionary<string, string> ConnectedUsers = new(); // ConnectionId -> Username

//        public ChatHub(ChatDbContext context)
//        {
//            _context = context;
//        }

//        public override async Task OnDisconnectedAsync(Exception? exception)
//        {
//            ConnectedUsers.Remove(Context.ConnectionId);
//            await base.OnDisconnectedAsync(exception);
//        }

//        // ---------------- REGISTER ----------------
//        public async Task RegisterUser(string username)
//        {
//            ConnectedUsers[Context.ConnectionId] = username;

//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
//            string publicKey, privateKey;

//            if (user == null)
//            {
//                publicKey = GenerateRsaKeys(out privateKey);
//                user = new User
//                {
//                    Username = username,
//                    PublicKey = publicKey,
//                    PrivateKey = privateKey
//                };

//                await _context.Users.AddAsync(user);
//                await _context.SaveChangesAsync();
//            }
//            else
//            {
//                publicKey = user.PublicKey!;
//                privateKey = user.PrivateKey!;
//            }

//            await Clients.Caller.SendAsync("UserRegistered", username, publicKey, privateKey);
//        }

//        // ---------------- PRIVATE MESSAGE ----------------
//        public async Task SendPrivateMessage(string toUsername, string message)
//        {
//            if (!ConnectedUsers.TryGetValue(Context.ConnectionId, out var fromUsername)) return;

//            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == fromUsername);
//            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.Username == toUsername);

//            if (sender == null || receiver == null) return;

//            string encryptedMessage = EncryptMessage(message, receiver.PublicKey);

//            var msg = new Message
//            {
//                SenderId = sender.Id,
//                ReceiverId = receiver.Id,
//                MessageText = encryptedMessage,
//                SentAt = DateTime.Now
//            };

//            _context.Messages.Add(msg);
//            await _context.SaveChangesAsync();

//            await Clients.Caller.SendAsync("ReceivePrivateMessage", toUsername, encryptedMessage);
//            foreach (var connId in ConnectedUsers.Where(x => x.Value == toUsername).Select(x => x.Key))
//            {
//                await Clients.Client(connId).SendAsync("ReceivePrivateMessage", fromUsername, encryptedMessage);
//            }
//        }

//        // ---------------- DECRYPT (for browser) ----------------
//        public async Task<string> DecryptMessageForUser(string encryptedText)
//        {
//            if (!ConnectedUsers.TryGetValue(Context.ConnectionId, out var username))
//                return "❌ User not found";

//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
//            if (user == null || user.PrivateKey == null)
//                return "❌ No private key found";

//            try
//            {
//                using var rsa = RSA.Create();
//                rsa.ImportRSAPrivateKey(Convert.FromBase64String(user.PrivateKey), out _);
//                var decryptedBytes = rsa.Decrypt(Convert.FromBase64String(encryptedText), RSAEncryptionPadding.Pkcs1);
//                return Encoding.UTF8.GetString(decryptedBytes);
//            }
//            catch (Exception ex)
//            {
//                return $"❌ Decryption failed: {ex.Message}";
//            }
//        }

//        // ---------------- GROUP CHAT ----------------
//        public async Task JoinGroup(string groupName)
//        {
//            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

//            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == groupName);
//            if (group == null)
//            {
//                group = new Group { GroupName = groupName };
//                await _context.Groups.AddAsync(group);
//                await _context.SaveChangesAsync();
//            }

//            await Clients.Group(groupName).SendAsync("GroupJoined", ConnectedUsers[Context.ConnectionId], groupName);
//        }

//        public async Task SendGroupMessage(string groupName, string message)
//        {
//            var fromUsername = ConnectedUsers[Context.ConnectionId];
//            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == fromUsername);
//            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == groupName);

//            if (sender == null || group == null) return;

//            var msg = new Message
//            {
//                SenderId = sender.Id,
//                GroupId = group.Id,
//                MessageText = message,
//                SentAt = DateTime.Now
//            };

//            _context.Messages.Add(msg);
//            await _context.SaveChangesAsync();

//            await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", fromUsername, message);
//        }

//        // ---------------- FILE UPLOAD ----------------
//        public async Task UploadFile(string groupName, string fileName, string base64Data)
//        {
//            var fromUsername = ConnectedUsers[Context.ConnectionId];

//            Directory.CreateDirectory("Uploads");
//            string filePath = Path.Combine("Uploads", fileName);
//            await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(base64Data));

//            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == fromUsername);
//            var group = await _context.Groups.FirstOrDefaultAsync(g => g.GroupName == groupName);

//            if (sender != null && group != null)
//            {
//                var msg = new Message
//                {
//                    SenderId = sender.Id,
//                    GroupId = group.Id,
//                    MessageText = "[FILE]",
//                    FileName = fileName,
//                    IsFile = true,
//                    SentAt = DateTime.Now
//                };
//                _context.Messages.Add(msg);
//                await _context.SaveChangesAsync();
//            }

//            await Clients.Group(groupName).SendAsync("ReceiveFile", fromUsername, fileName);
//        }

//        // ---------------- RSA HELPERS ----------------
//        private string GenerateRsaKeys(out string privateKey)
//        {
//            using var rsa = RSA.Create(2048);
//            privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
//            return Convert.ToBase64String(rsa.ExportRSAPublicKey());
//        }

//        private string EncryptMessage(string plainText, string publicKey)
//        {
//            using var rsa = RSA.Create();
//            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
//            var encrypted = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.Pkcs1);
//            return Convert.ToBase64String(encrypted);
//        }
//    }
//}
