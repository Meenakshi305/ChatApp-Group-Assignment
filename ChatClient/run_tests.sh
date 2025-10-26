#!/bin/bash

# === CONFIGURATION ===
SERVER_PROJECT="/c/Users/meena/Downloads/ChatAppApi (2)/ChatApp/ChatAppApi/ChatAppApi"
CLIENT_SOURCE="/c/Users/meena/Downloads/ChatAppApi (2)/ChatApp/ChatClient"
CLIENT_EXE="/c/Users/meena/Downloads/ChatAppApi (2)/ChatApp/ChatClient"

# === STEP 1: Compile client ===
echo "Compiling ChatClient..."
dotnet publish "$CLIENT_SOURCE" -c Release -o "$(dirname "$CLIENT_EXE")" || { echo "Compilation failed"; exit 1; }

# === STEP 2: Start server in background ===
echo "Starting server..."
dotnet run --project "$SERVER_PROJECT" &
SERVER_PID=$!
sleep 3  # wait for server to start

# === STEP 3: Start client 1 ===
echo "Starting client 1..."
"$CLIENT_EXE" <<EOF &
TestUser1
online
exit
EOF
CLIENT1_PID=$!

# === STEP 4: Start client 2 ===
echo "Starting client 2..."
"$CLIENT_EXE" <<EOF &
TestUser2
online
exit
EOF
CLIENT2_PID=$!

# === STEP 5: Wait for clients to finish ===
wait $CLIENT1_PID
wait $CLIENT2_PID

# === STEP 6: Stop server ===
kill $SERVER_PID

echo "All tests completed."
