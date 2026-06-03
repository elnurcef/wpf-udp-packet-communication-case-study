# WPF UDP Communication Simulator

A WPF-based desktop communication simulator developed as a technical case study.
The solution contains two separate WPF applications that communicate with each other over UDP using a custom binary packet protocol.

## Project Overview

This project simulates communication between:

1. **User Interface**

   * Operator-side WPF application
   * Receives communication packets
   * Displays packet data
   * Sends command and setting packets
   * Receives feedback packets
   * Logs incoming communication data
   * Plays saved logs
   * Converts log files to MATLAB `.mat` format

2. **Simulation Interface**

   * Simulated avionics/embedded unit
   * Sends communication packets at 5 Hz
   * Receives command and setting packets
   * Sends feedback packets back to the user interface

3. **Shared Library**

   * Shared packet models
   * Enums
   * CRC calculation
   * Packet building
   * Packet validation
   * Byte-by-byte packet capture state machine
   * UDP communication service
   * Logging and MATLAB conversion services

## Solution Structure

```text
wpf-udp-communication-simulator/
│
├── Baykar.UserInterface/
│   └── WPF operator/user interface
│
├── Baykar.SimulationInterface/
│   └── WPF simulated unit interface
│
├── Baykar.Shared/
│   └── Shared protocol, packet, CRC, UDP, logging and MATLAB logic
│
├── BaykarCaseStudy.sln
├── PROJECT_RULES.md
└── README.md
```

## Technologies

* C#
* .NET 8
* WPF
* MVVM-style structure
* UDP communication
* Custom binary packet protocol
* CRC-8/ATM
* Little Endian serialization
* CSV logging
* MATLAB `.mat` conversion

## Packet Format

All packets use the following binary format:

```text
Sync1 | Sync2 | PacketId | PayloadLength | Payload | CRC
```

| Field         | Description                 |
| ------------- | --------------------------- |
| Sync1         | First synchronization byte  |
| Sync2         | Second synchronization byte |
| PacketId      | Packet type identifier      |
| PayloadLength | Payload size in bytes       |
| Payload       | Packet-specific binary data |
| CRC           | CRC-8 checksum              |

Synchronization bytes:

```text
Sync1 = 169
Sync2 = 233
```

## Packet Types

| Packet Type          | ID | Direction                           |
| -------------------- | -: | ----------------------------------- |
| CommunicationPacket1 |  3 | SimulationInterface → UserInterface |
| CommunicationPacket2 |  4 | SimulationInterface → UserInterface |
| SettingPacket        |  5 | UserInterface → SimulationInterface |
| CommandPacket        |  6 | UserInterface → SimulationInterface |
| FeedbackPacket       |  7 | SimulationInterface → UserInterface |

## CRC Algorithm

The project uses:

```text
CRC-8/ATM
Polynomial: 0x07
Initial value: 0x00
Final XOR: 0x00
```

CRC is calculated over:

```text
Sync1 + Sync2 + PacketId + PayloadLength + Payload
```

The CRC byte itself is not included in the CRC calculation.

## UDP Ports

The applications communicate over localhost using these ports:

| Application         | Local Port | Remote Target  |
| ------------------- | ---------: | -------------- |
| SimulationInterface |       5000 | 127.0.0.1:5001 |
| UserInterface       |       5001 | 127.0.0.1:5000 |

## How to Build

Run this command from the solution root:

```bash
dotnet build
```

## How to Run

Open two terminals from the solution root.

### Terminal 1 — Run User Interface

```bash
dotnet run --project .\Baykar.UserInterface\Baykar.UserInterface.csproj
```

### Terminal 2 — Run Simulation Interface

```bash
dotnet run --project .\Baykar.SimulationInterface\Baykar.SimulationInterface.csproj
```

## Recommended Startup Order

1. Start `Baykar.UserInterface`
2. Click **Start Listener**
3. Start `Baykar.SimulationInterface`
4. Click **Start Simulation**
5. Verify that CommunicationPacket1 and CommunicationPacket2 are received
6. Send command or setting packets from the User Interface
7. Verify that feedback is received

## Main Features

### User Interface

* Starts and stops UDP listener
* Receives CommunicationPacket1 and CommunicationPacket2
* Displays received packet data
* Shows valid and invalid packet counters
* Sends CommandPacket
* Sends SettingPacket
* Receives FeedbackPacket
* Logs received communication packets
* Plays saved log files
* Converts saved logs to MATLAB `.mat` files

### Simulation Interface

* Starts and stops UDP listener
* Sends CommunicationPacket1 and CommunicationPacket2 at 5 Hz
* Receives CommandPacket
* Receives SettingPacket
* Sends FeedbackPacket
* Displays sent packet counters
* Displays last received command and setting values

## Logging

CommunicationPacket1 and CommunicationPacket2 data can be logged from the User Interface.

Log files are saved under:

```text
Release/Log Kayıtları
```

The log format is CSV.

## Log Playback

Saved CSV logs can be replayed from the User Interface.

During playback, packet values are shown again in the UI without requiring active UDP communication.

## MATLAB Conversion

Saved CSV logs can be converted to MATLAB `.mat` files.

MATLAB files are saved under:

```text
Release/MATLAB Dönüsümleri
```

The generated `.mat` file contains packet-based variables for CommunicationPacket1 and CommunicationPacket2 data.

## Important Implementation Notes

* Packet serialization uses Little Endian byte order.
* Packet parsing is implemented with a byte-by-byte state machine.
* UserInterface does not use Timer.
* Periodic transmission is implemented with an async loop and `Task.Delay`.
* Background operations use `Task`, `CancellationToken`, and event-based communication.
* UI updates from background receive loops are marshalled safely through Dispatcher.
* The project avoids manual thread creation where possible.

## Known Note

CommunicationPacket1 includes a `Data5` field defined as `INT16`.

The sample document value for this field is `72635`, which is outside the valid `Int16` range.
Because of this inconsistency, the implementation uses `0` as the sample value for `Data5`.

## Suggested Manual Test Flow

1. Build the solution
2. Run UserInterface
3. Run SimulationInterface
4. Start listener in UserInterface
5. Start simulation in SimulationInterface
6. Confirm that CommunicationPacket1 and CommunicationPacket2 are received
7. Send Command 1 from UserInterface
8. Confirm that SimulationInterface receives the command
9. Confirm that feedback is returned to UserInterface
10. Send Setting 1 from UserInterface
11. Confirm that SimulationInterface receives the setting and value
12. Confirm that feedback is returned to UserInterface
13. Start logging
14. Stop logging after a few seconds
15. Play the saved log
16. Convert the saved log to MATLAB format
17. Stop simulation and listener without application crash

## Repository Note

Generated runtime files such as logs, MATLAB output files, `bin`, and `obj` folders should not be committed to the repository.
