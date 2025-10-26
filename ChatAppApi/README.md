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
![Open Project Screenshot](images/open_project.png)

---

### 2️⃣ Configure the Database Connection
1. Open `appsettings.json`  
2. Update the **server name** in the connection string according to your local SQL Server instance.

Example:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ChatAppDb;Trusted_Connection=True;"
}
```
![AppSettings Screenshot](images/appsettings.png)

---

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
```
![Package Manager Screenshot](images/package_manager.png)

---

### 5️⃣ Apply Migrations to Create Tables
Run the following:
```bash
Add-Migration InitialCreate
Update-Database
```
This creates and establishes the database connection successfully.  
![Database Setup Screenshot](images/database_setup.png)

---

### 6️⃣ Build and Run the Server
In the project directory:
```bash
dotnet run
```
The server starts on **port 5086** by default.  
If needed, modify the port in `Program.cs`.  
![Server Running Screenshot](images/server_run.png)

---

### 7️⃣ Run the Client and Register Users
1. Open a new terminal and start the **client application**  
2. Enter a username when prompted  
3. Choose between **private** or **group** chat  
4. Register another user to enable chatting  

![Client App Screenshot](images/client_app.png)

---

## 🔐 Secure Communication Process

### 🔑 Step 1: Exchange Public Keys
Before sending encrypted messages, users must exchange public keys.

![Key Exchange Screenshot](images/key_exchange.png)

### 💬 Step 2: Start Chatting
After exchanging keys:
- Users can securely send **private messages**.  
- Group members see messages **in real time**.

![Group Chat Screenshot](images/group_chat.png)

---

## ✅ Example Workflow
When one user in a group chat sends a message, all other users receive it instantly and securely.

![Example Screenshot](images/example_chat.png)

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
 │    └── RSAHelper.cs
 └── README.md
```

---

## 🧠 Summary
This project demonstrates a **secure communication protocol** using:
- SignalR for real-time messaging  
- RSA encryption for key exchange and message security  
- SQL Server for data storage  

---

### 📸 Screenshots Placeholder Guide
Replace the placeholders under the `images/` folder with your screenshots:
```
images/
├── open_project.png
├── appsettings.png
├── package_manager.png
├── database_setup.png
├── server_run.png
├── client_app.png
├── key_exchange.png
├── group_chat.png
├── example_chat.png
```

---

📄 **End of README**
