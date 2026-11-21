# CrossPlatformDataTransfer

> **Current status (2026-11-22):** This is an experimental prototype, not a commercial migration product. The Android Agent TLS build was installed on the owner's Samsung SM-A135F (Android API 34), but the latest TLS pairing attempt did not complete because the Android foreground agent service was stopped. The TLS runtime is therefore **not verified**. The application does not transfer files or personal data, and no backup/restore feature is enabled.

## Overview

CrossPlatformDataTransfer is an experimental .NET 8/WPF project intended to grow into a Windows tool for Android data migration. It is not yet a working migration product. Current work focuses on making the app honest about its capabilities, establishing a small Android connection prototype, and validating a safe transfer design before any personal data is handled.

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
    *   Contains cryptographic building blocks (AES-GCM, hashing, key exchange, and pairing prototypes). These components are not yet integrated into a verified end-to-end file-transfer protocol.
    *   **Key Components:** `AesGcmEncryptionService`, `KeyManagementService`, `Sha256HashService`, `DiffieHellmanKeyExchangeService`, `SecurePairingService`.

5.  **CrossPlatformDataTransfer.UI:**
    *   (WPF Application - Windows-specific) Provides a modern, responsive graphical user interface using the MVVM pattern. This project is designed for Windows environments.
    *   **Key Components:** `MainViewModel`, `MainWindow.xaml`.

6.  **CrossPlatformDataTransfer.CLI:**
    *   The former plaintext TCP listener is disabled. The CLI currently exits with a notice and is not a supported migration path.

7.  **CrossPlatformDataTransfer.Startup:**
    *   A dedicated project to centralize Dependency Injection (DI) configuration, making it easy to register all services from different layers.

## Android Agent

The `android-agent` folder contains a Kotlin prototype. It starts a visible foreground service, listens on device loopback port 4711, and supports only `PAIR`, `PING`, `HANDSHAKE`, and `GET_CAPABILITIES`. The source includes a TLS session and certificate fingerprint comparison, but that updated TLS session has not passed the on-device acceptance test. Advertised read/write capabilities remain empty. The agent does **not** access files, photos, contacts, SMS, or other phone data.

## Getting Started

### Prerequisites

*   .NET 8 SDK (for building the C# solution)
*   Android Studio with Android SDK Platform 36 and JDK 17 (to build the Kotlin agent)
*   ADB (Android SDK Platform-Tools) and an Android device for integration testing

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

### Android Agent status

Open `android-agent` in Android Studio and build the APK. Follow `android-agent/README.md` for the pairing sequence. The current TLS build is installed on the owner's A13, but the TLS runtime acceptance test is still pending. Building or installing this scaffold does not enable data transfers.

### CLI status

The former plaintext TCP listener was removed. The CLI currently exits with a message directing users to the WPF preview; it does not pair devices or perform a migration.

### Running the WPF UI (Windows Only)

1.  **Open the solution in Visual Studio (or Rider) on a Windows machine.**
2.  **Set `CrossPlatformDataTransfer.UI` as the startup project.**
3.  **Run the application.**

## Deployment

Do not distribute the current Debug APK or Windows preview as a production release. Production packaging, code signing, update verification, compatibility testing, and user documentation are not complete.

## Future Enhancements

*   Verify TLS pairing and fail-closed behavior on the authorized A13 device.
*   Implement user-selected file transfer in both directions over a versioned, authenticated protocol.
*   Build backup/restore and official Android data modules only where platform APIs allow them.
*   Complete security, reliability, compatibility, signing, packaging, and commercial release review.

---

**Author:** Manus AI
**Date:** February 19, 2026
