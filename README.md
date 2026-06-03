# Baykar Case Study

## Project Overview

Baykar Case Study is a .NET 10 WPF solution built with C# and MVVM-style structure.

The solution contains:

- `Baykar.UserInterface`: operator/user interface for monitoring received communication packets, sending command/setting packets, logging received data, playing logs, and converting logs to MATLAB files.
- `Baykar.SimulationInterface`: simulated avionics/embedded unit that sends communication packets and receives commands/settings.
- `Baykar.Shared`: shared protocol enums, packet payload models, CRC calculation, packet building, packet validation, byte-by-byte packet parsing, UDP communication, logging, and MATLAB conversion services.

## How To Build

From the solution root:

```powershell
dotnet build .\BaykarCaseStudy.sln
```

## How To Run UserInterface

From the solution root:

```powershell
dotnet run --project .\Baykar.UserInterface\Baykar.UserInterface.csproj
```

In the UserInterface window, click `Start Listener` before starting the simulation.

## How To Run SimulationInterface

From the solution root:

```powershell
dotnet run --project .\Baykar.SimulationInterface\Baykar.SimulationInterface.csproj
```

In the SimulationInterface window, click `Start Simulation` to begin UDP transmission.

## Correct Startup Order

1. Start `Baykar.UserInterface`.
2. Click `Start Listener`.
3. Start `Baykar.SimulationInterface`.
4. Click `Start Simulation`.

This order ensures the operator UI is already listening before the simulation starts sending packets.

## UDP Ports

- SimulationInterface local port: `5000`
- UserInterface local port: `5001`
- SimulationInterface sends to UserInterface: `127.0.0.1:5001`
- UserInterface sends to SimulationInterface: `127.0.0.1:5000`

## Packet Format

Packet bytes are built in this order:

```text
Sync1, Sync2, PacketId, PayloadLength, Payload, CRC
```

Protocol constants:

- `Sync1 = 169`
- `Sync2 = 233`

Packet serialization uses Little Endian for numeric payload values.

## CRC Algorithm

CRC uses CRC-8/ATM:

- Polynomial: `0x07`
- Initial value: `0x00`
- Final XOR: `0x00`

CRC is calculated over all packet bytes except the final CRC byte.

## Log Folder Path

Communication logs are saved under the UserInterface application base directory:

```text
Release/Log Kayıtları
```

For a Debug run, this is typically:

```text
Baykar.UserInterface/bin/Debug/net10.0-windows/Release/Log Kayıtları
```

## MATLAB Conversion Folder Path

Converted MATLAB files are saved under the UserInterface application base directory:

```text
Release/MATLAB Dönüsümleri
```

For a Debug run, this is typically:

```text
Baykar.UserInterface/bin/Debug/net10.0-windows/Release/MATLAB Dönüsümleri
```

## Known Note

`CommunicationPacket1` `Data5` is `INT16` in the PDF, but the PDF sample value `72635` is outside the valid `Int16` range. The implementation uses `0` for the sample value.
