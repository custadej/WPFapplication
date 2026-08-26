# Messaging WPF App

Desktop LAN messaging app built for school as an introduction to C#, .NET, WPF, and XAML. Messenger-style UI with real-time chat over a local network using raw TCP sockets.

## What it does

Two instances of the app connect directly over LAN — one runs as a server, the other connects as a client. No backend or internet required, just a local IP.

- **Real-time messaging** — TCP connection via `TcpListener` / `TcpClient`, messages sent over `NetworkStream`
- **Contacts** — add and manage contacts, each with their own conversation history
- **Chat history** — conversations persist locally in an XML file (`chat-data.xml`)
- **User profiles** — set your name, status (Online, Away, etc.) and avatar with built-in image cropping
- **Custom themes** — pick accent colors and adjust the UI appearance
- **Emoji support** — emoji picker in the message composer
- **Auto-save** — chat data saved automatically in the background with a DispatcherTimer

## Stack

- C# / .NET
- WPF (Windows Presentation Foundation)
- XAML for UI layout and data binding
- INotifyPropertyChanged + ObservableCollection for reactive UI
- XML serialization for data persistence
- Raw TCP sockets for networking (no SignalR or third-party libs)

## Run

Open `TakojsnjeSporocanje/TakojsnjeSporocanje.slnx` in Visual Studio and hit Run.

One instance starts as **server** (listens on a port), the other connects as **client** using the server's local IP. Both modes are selectable from the network settings window.
