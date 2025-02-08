# Remote Shutdown

Remote Shutdown is a simple Windows application that allows you to remotely shut down a PC over the local network. The server listens for HTTP requests from clients, accepts a password, and can trigger a system shutdown.

## Features

- **Start/Stop Server**: Allows you to control the server to start or stop listening for shutdown requests.
- **Secure Password Authentication**: The server verifies the password before allowing shutdown commands.
- **Remote Shutdown**: When triggered, the server will initiate a system shutdown after a set time.
- **Temporary and Permanent IP Blacklist**: IP addresses can be blacklisted temporarily or permanently in case of suspicious activity.
- **Log File**: All actions are logged to a text file and can be viewed in the application.

## Requirements

- **.NET Framework**: This application requires .NET Framework 4.5 or later.
- **Windows OS**: Windows OS is required for running the application, as it uses Windows-specific features like `cmd.exe /c /shutdown`.

## Installation

1. Clone or download the repository to your local machine.
2. Open the solution in Visual Studio.
3. Build the project.
4. Run the application.

## Usage

### Start the Server

1. Launch the application.
2. Click the **"On"** button to start the server. The server will listen for incoming requests.
3. The application will display logs in the console window.

### Trigger Remote Shutdown

To trigger a remote shutdown:

1. Send a request to the server using a browser or HTTP client: `http://<server-ip>:8111/RemoteShutdown?password=<password>`
Replace `<server-ip>` with the local IP address of the machine running the server and `<password>` with the password set in the `ServerClass.Password` variable.

2. If the password is correct, the server will display a confirmation message and the user will see a shutdown option.

3. Clicking the **Shutdown** button will initiate a system shutdown after the configured shutdown time (default: 10 seconds).

### IP Blacklist

- **Temporary Blacklist**: If an incorrect password is entered, the IP is temporarily added to the blacklist for 5 seconds.
- **Permanent Blacklist**: Certain IPs can be permanently banned from accessing the server.

### Stopping the Server

Click the **"Off"** button to stop the server. This will stop accepting any more shutdown requests.

## Configuration

- **Password**: The default password is `SafePassword`. You can change this in the `ServerClass.Password` field.
- **Timeout Time**: The default timeout for temporarily blacklisting an IP is 5 seconds, controlled by the `TimeOutTime` variable.
- **Shutdown Time**: The default time before the system shuts down is 10 seconds. You can change this in the `ShutdownTime` variable.

## Logging

All server actions and requests are logged in `Log.txt`. The logs are also shown in the application's console window.

## Troubleshooting

- **"Connecting error"**: This error appears if the server can't get the local IP address. Ensure that your Wi-Fi or network connection is active.
- **Firewall Issues**: Make sure that the firewall allows connections on port `8111`.


