# Project Rules

- Language: C#
- Framework: .NET 10
- UI Framework: WPF
- Architecture: MVVM
- Naming language: English
- Communication protocol: UDP will be used
- Packet format: Sync1, Sync2, PacketId, PayloadLength, Payload, CRC
- Sync1 = 169
- Sync2 = 233
- CRC algorithm: CRC-8/ATM
- Packet serialization must use Little Endian
- The user interface must not use Timer
- Background work must be implemented with Task, CancellationToken, and events
- UI updates must be done safely through Dispatcher
- More than 3 manually created threads must not be used
- Packet parsing must be implemented with a byte-by-byte packet state machine
