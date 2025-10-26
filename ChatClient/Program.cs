using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;

class Program
{
    private static ClientWebSocket socket = new();
    private static string username = "";
    private static RSA clientPrivateKey;
    private static RSA serverPublicKey;
    private static readonly HashSet<string> JoinedGroups = new();
    private static readonly Dictionary<string, RSA> UserPublicKeys = new();


    static async Task Main()
    {
        Console.Write("Enter your username: ");
        username = Console.ReadLine()!.Trim();

        await socket.ConnectAsync(new Uri("ws://localhost:5253/ws"), CancellationToken.None);
        Console.WriteLine("Connected to server.");

        // Register
        await SendJson(new { action = "register", username });

        _ = Task.Run(ListenAsync);

        Console.WriteLine("\nCommands:");
        Console.WriteLine("private <to> <message>");
        Console.WriteLine("privateFile <to> <filePath>");
        Console.WriteLine("join <groupName>");
        Console.WriteLine("groupmsg <groupName> <message>");
        Console.WriteLine("groupfile <groupName> <filePath>");
        Console.WriteLine("online");
        Console.WriteLine("exit\n");

        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input == null) continue;
            if (input.Trim().ToLower() == "exit") break;

            var parts = input.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1)
            {
                Console.WriteLine(" Invalid command.");
                continue;
            }

            var cmd = parts[0].ToLower();
            switch (cmd)
            {
                case "private":
                    if (parts.Length < 3) { Console.WriteLine("Usage: private <username> <message>"); continue; }
                    await SendJson(new { action = "privateMessage", to = parts[1], message = parts[2] });
                    break;

                case "privatefile":
                    if (parts.Length < 3) { Console.WriteLine("Usage: privateFile <username> <filePath>"); continue; }
                    await SendFile(parts[1], parts[2], "privateFile");
                    break;

                case "join":
                    await SendJson(new { action = "joinGroup", group = parts[1] });
                    JoinedGroups.Add(parts[1]);
                    break;

                case "groupmsg":
                    if (parts.Length < 3) { Console.WriteLine("Usage: groupmsg <group> <message>"); continue; }
                    if (!JoinedGroups.Contains(parts[1])) { Console.WriteLine($"❌ You are not in '{parts[1]}'"); continue; }
                    await SendJson(new { action = "groupMessage", group = parts[1], message = parts[2] });
                    break;

                case "groupfile":
                    if (parts.Length < 3) { Console.WriteLine("Usage: groupfile <group> <filePath>"); continue; }
                    if (!JoinedGroups.Contains(parts[1])) { Console.WriteLine($"❌ You are not in '{parts[1]}'"); continue; }
                    await SendFile(parts[1], parts[2], "groupFile");
                    break;

                case "online":
                    await SendJson(new { action = "listOnlineUsers" });
                    break;

                default:
                    Console.WriteLine(" Unknown command.");
                    break;
            }
        }

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closed", CancellationToken.None);
        Console.WriteLine("Disconnected.");
    }

    private static async Task SendFile(string target, string filePath, string action)
    {
        if (!File.Exists(filePath)) { Console.WriteLine(" File not found."); return; }

        string base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(filePath));
        await SendJson(new
        {
            action,
            to = action == "privateFile" ? target : null,
            group = action == "groupFile" ? target : null,
            fileName = Path.GetFileName(filePath),
            base64
        });

        Console.WriteLine($"📤 Sent file '{filePath}' via {action}");
    }

    private static async Task SendJson(object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static async Task ListenAsync()
    {
        var buffer = new byte[8192];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close) { Console.WriteLine("Server closed connection."); break; }

            string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                var doc = JsonDocument.Parse(json).RootElement;
                string type = doc.GetProperty("type").GetString() ?? "";

                switch (type)
                {
                    case "registered":
                        clientPrivateKey = RSA.Create(4096); // Each client creates its own key

                        // Store server's public key if sent
                        if (doc.TryGetProperty("publicKey", out var pubKeyProp))
                        {
                            byte[] pubKeyBytes = Convert.FromBase64String(pubKeyProp.GetString()!);
                            serverPublicKey = RSA.Create();
                            serverPublicKey.ImportRSAPublicKey(pubKeyBytes, out _);

                            if (doc.TryGetProperty("username", out var usernameProp))
                            {
                                string user = usernameProp.GetString()!;
                                RSA userRsa = RSA.Create();
                                userRsa.ImportRSAPublicKey(pubKeyBytes, out _);
                                UserPublicKeys[user] = userRsa;
                            }
                        }

                        Console.WriteLine($"Registered as {username}");
                        break;

                    case "private":
                        VerifyAndDisplay(doc, false);
                        break;

                    case "privateFile":
                        VerifyAndSaveFile(doc, false);
                        break;

                    case "groupMessage":
                        if (JoinedGroups.Contains(doc.GetProperty("group").GetString())) VerifyAndDisplay(doc, true);
                        break;

                    case "groupFile":
                        if (JoinedGroups.Contains(doc.GetProperty("group").GetString())) VerifyAndSaveFile(doc, true);
                        break;

                    case "joinedGroup":
                        var grp = doc.GetProperty("group").GetString();
                        JoinedGroups.Add(grp);
                        Console.WriteLine($"\n Joined group '{grp}'");
                        break;

                    case "onlineUsers":
                        Console.WriteLine("\n Online users: " + string.Join(", ", doc.GetProperty("users").EnumerateArray().Select(u => u.GetString())));
                        break;

                    case "error":
                        Console.WriteLine($" Error: {doc.GetProperty("text").GetString()}");
                        break;

                    default:
                        Console.WriteLine($"[Server]: {json}");
                        break;
                }
            }
            catch (Exception ex) { Console.WriteLine($"Raw: {json}"); }
        }
    }

    private static void VerifyAndDisplay(JsonElement doc, bool isGroup)
    {
        string text = doc.GetProperty("text").GetString() ?? "";
        string from = doc.GetProperty("from").GetString() ?? "?";
        string signature = doc.GetProperty("signature").GetString() ?? "";

        // Get sender's public key
        UserPublicKeys.TryGetValue(from, out RSA senderPublicKey);
        bool verified = senderPublicKey != null && VerifySignature(text, signature, senderPublicKey);




        if (isGroup)
        {
            string group = doc.GetProperty("group").GetString() ?? "?";
            Console.WriteLine($"\n [{group}] {from}: {text}");
        }
        else
        {
            Console.WriteLine($"\n Private from {from}: {text}");
        }
    }

    private static void VerifyAndSaveFile(JsonElement doc, bool isGroup)
    {
        string from = doc.GetProperty("from").GetString();
        string fileName = doc.GetProperty("fileName").GetString();
        string base64 = doc.GetProperty("base64").GetString();
        string signature = doc.GetProperty("signature").GetString();
        string folder = isGroup ? "GroupDownloads" : "PrivateDownloads";

        UserPublicKeys.TryGetValue(from, out RSA senderPublicKey);
        bool verified = senderPublicKey != null && VerifySignature(base64, signature, senderPublicKey);

        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName);
        File.WriteAllBytes(path, Convert.FromBase64String(base64));

        Console.WriteLine($"\n {(isGroup ? "Group" : "Private")} file '{fileName}' received from {from}");
    }

    private static bool VerifySignature(string data, string signatureBase64, RSA publicKey)
    {
        try
        {
            byte[] sigBytes = Convert.FromBase64String(signatureBase64);
            return publicKey.VerifyData(Encoding.UTF8.GetBytes(data), sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        }
        catch { return false; }
    }

}
