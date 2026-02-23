# CrossPlatformDataTransfer

## Overview

CrossPlatformDataTransfer is a robust, commercial-grade desktop application designed for secure and efficient data transfer between a PC and Android devices. Built with .NET 8 and adhering strictly to Clean Architecture principles, this project emphasizes modularity, testability, and security. It leverages a hybrid agent-based approach for high-speed, reliable data migration, overcoming the limitations of traditional ADB shell commands.

## Architecture

The solution is structured into several layers, following the Clean Architecture paradigm:

1.  **CrossPlatformDataTransfer.Core:**
    *   Contains domain models, enums, and interfaces. This layer is the heart of the application and remains independent of any external concerns.
    *   **Key Components:** `Device`, `TransferSession`, `TransferItem`, `ClipboardData`, `IAdbService`, `IAgentCommunicationService`, `IEncryptionService`, `IKeyManagementService`, `IPairingService`, `IHashService`, `IDeviceDiscoveryService`, `ITransferService`, `IDeviceRepository`, `ITransferRepository`.

2.  **CrossPlatformDataTransfer.Application:**
    *   Implements use cases that orchestrate the flow of data between the UI/CLI and the domain/infrastructure layers.
    *   **Key Components:** `DeviceDiscoveryUseCase`, `SecureTransferUseCase`, `SmsTransferUseCase`, `AgentBasedTransferUseCase`, Data Transfer Objects (DTOs).

3.  **CrossPlatformDataTransfer.Infrastructure:**
    *   Provides concrete implementations for interfaces defined in the Core layer, focusing on external concerns like device communication (ADB), data persistence (mock repositories), and agent interaction.
    *   **Key Components:** `AdbService`, `AdbDeviceDiscoveryService`, `AndroidAgentClient`, `MockDeviceRepository`, `MockTransferService`, `SmsNormalizationService`.

4.  **CrossPlatformDataTransfer.Infrastructure.Security:**
    *   Dedicated to implementing robust security features, including end-to-end encryption and key management.
    *   **Key Components:** `AesGcmEncryptionService` (AES-256-GCM), `KeyManagementService`, `Sha256HashService`, `DiffieHellmanKeyExchangeService`, `SecurePairingService`.

5.  **CrossPlatformDataTransfer.UI:**
    *   (WPF Application - Windows-specific) Provides a modern, responsive graphical user interface using the MVVM pattern. This project is designed for Windows environments.
    *   **Key Components:** `MainViewModel`, `MainWindow.xaml`.

6.  **CrossPlatformDataTransfer.CLI:**
    *   A Console Application for testing and interacting with the application's core functionalities in a command-line environment. Useful for development and automated testing.
    *   **Key Components:** `Program.cs` for DI setup and use case execution.

7.  **CrossPlatformDataTransfer.Startup:**
    *   A dedicated project to centralize Dependency Injection (DI) configuration, making it easy to register all services from different layers.

## Android Agent

The project utilizes an external **Android Data Agent** (Kotlin-based application) that runs on the Android device. This agent communicates with the PC application via ADB port forwarding and TCP sockets, enabling:

*   **Permission Handling:** Requests necessary permissions (e.g., SMS, Contacts) directly from the user on the device.
*   **Efficient Data Access:** Accesses device data (e.g., SMS database) through Android's Content Providers, avoiding root requirements in many cases.
*   **Secure Communication:** Supports AES-256-GCM encrypted data streaming.

## Getting Started

### Prerequisites

*   .NET 8 SDK (for building the C# solution)
*   Android SDK with ADB (for device communication)
*   A compatible Android device with the **Android Data Agent** application installed.

### Building the Solution

1.  **Clone the repository:**
    ```bash
    git clone <repository_url>
    cd CrossPlatformDataTransfer
    ```
2.  **Restore dependencies and build:**
    ```bash
    dotnet build
    ```
    *Note: The `CrossPlatformDataTransfer.UI` project may show build errors on non-Windows environments due to WPF-specific dependencies. This is expected and does not affect the core logic or the CLI tool.*

### Running the CLI Tool

To test the core functionalities and agent communication:

1.  **Ensure an Android device is connected via USB and ADB is authorized.**
2.  **Install the Android Data Agent APK on the device.**
3.  **Run the CLI project:**
    ```bash
    dotnet run --project CrossPlatformDataTransfer.CLI
    ```
    The CLI tool will demonstrate device discovery and initiate a mock data transfer flow using the agent protocol.

### Running the WPF UI (Windows Only)

1.  **Open the solution in Visual Studio (or Rider) on a Windows machine.**
2.  **Set `CrossPlatformDataTransfer.UI` as the startup project.**
3.  **Run the application.**

## Deployment

For deployment, compile the desired projects (e.g., `CrossPlatformDataTransfer.UI` for the desktop application, `CrossPlatformDataTransfer.CLI` for command-line utilities) and distribute the resulting executables along with their dependencies.

## Future Enhancements

*   Full implementation of `DiffieHellmanKeyExchangeService` and `SecurePairingService`.
*   Support for other data types (Contacts, Call Logs, Photos).
*   Comprehensive error handling and logging.
*   Unit and integration tests for all layers.
*   User authentication and authorization.

---

**Author:** Manus AI
**Date:** February 19, 2026
