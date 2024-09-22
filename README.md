# Unity TCP
async/await TCP socket implementation for Unity3d.

# How to use

## Installation
In Package manager click plus button and click "add package from git URL". Then enter follow link:
`https://github.com/iAcolyte/UniTCP.git?path=Assets`

## As a server
1. Attatch `UnityTCP.cs` to arbitary GameObject
2. Change socket type to `Server`
3. Set port number you want to open
4. Set some callback events
    - `OnMessage` returns received string message
    - `OnEstablished` called when connection is established and returns connected TcpClient
    - `OnDisconnected` called when connection is lost and returns disconnected client's Endpoint

## As a client
1. Attatch `UnityTCP.cs` to arbitary GameObject
2. Change socket type to `Client`
3. Set host address and port number you want to access
4. Set `OnMessage` callback event
    - this will be called when a server send message to you


# Sample
Sample scene is included in `Assets/UnityTCP/Sample.unity`