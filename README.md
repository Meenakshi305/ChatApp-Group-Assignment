# 🛡️ Advanced Secure Protocol Design, Implementation, and Review

### 👥 Group Name: Group 108 (Project Group 4)

**Members:**
- Meenakshi S Reddy (A1987136)  
- Gaddam Pranavi Reddy (A1994118)  
- Anil Goud Sunkari (A1985082)  
- Gopi Krishna Ravula (A1942594)

---

## ⚙️ Prerequisites
Ensure the following tools are installed on your system:
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

---

## 🚀 Setup Process

### 1️⃣ Open the Project
Navigate to your project folder and open:
```
ChatAppApi.csproj
```

---

### 2️⃣ Configure the Database Connection
1. Open `appsettings.json`  
2. Update the **server name** in the connection string according to your local SQL Server instance.

Example:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ChatAppDb;Trusted_Connection=True;"
}


### 3️⃣ Create and Connect to the Database
1. Launch **SSMS**  
2. Connect to your SQL Server instance  
3. (Optional) Delete any existing **Migrations** folder  

---

### 4️⃣ Install Required Packages
In the Visual Studio **Package Manager Console**, run:
```bash
Install-Package Microsoft.EntityFrameworkCore.SqlServer
Install-Package Microsoft.EntityFrameworkCore.Design


### 5️⃣ Apply Migrations to Create Tables
Run the following:
```bash
Add-Migration InitialCreate
Update-Database
```
This creates and establishes the database connection successfully.  


---

### 6️⃣ Build and Run the Server
In the project directory:
```bash
dotnet run
```
The server starts on **port 5086** by default.  
If needed, modify the port in `Program.cs`.  


---

### 7️⃣ Run the Client and Register Users
1. Open a new terminal and start the **client application**  
2. Enter a username when prompted  
3. Choose between **private** or **group** chat  
4. Register another user to enable chatting  


---

## 🔐 Secure Communication Process
Commands:
private <to> <message>
privateFile <to> <filePath>
join <groupName>
groupmsg <groupName> <message>
groupfile <groupName> <filePath>
online
exit
Command	Description
private <to> <message>	Send a private encrypted message to a specific user
privateFile <to> <filePath>	Send an encrypted file privately to a user
join <groupName>	Join or create a secure group chat
groupmsg <groupName> <message>	Send an encrypted message to a group
groupfile <groupName> <filePath>	Send an encrypted file to a group
online	Display all users currently online
exit	Disconnect from the server

---

## 📂 Folder Structure (Example)
```
ChatApp/
 ├── ChatAppApi/
 │    ├── appsettings.json
 │    ├── Program.cs
 │    ├── Hubs/
 │    └── Models/
 ├── ChatClient/
 │    ├── Program.cs
 │   
 └── README.md
```

---

## 🧠 Summary
This project demonstrates a **secure communication protocol** using:
- SignalR for real-time messaging  
- RSA encryption for key exchange and message security  
- SQL Server for data storage  

---
Also added documentation with setup images for easier understanding.
📄 **End of README**
