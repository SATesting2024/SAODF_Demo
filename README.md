# ODF TOOL APIs Automation

## Prerequisites

Before running the automation, ensure the following requirements are met:

-  **SSH Key Fingerprint** of the **Head Unit** (for QNX connection)
-  Active **Internet connectivity** on the Head Unit
- 'WinSCP.exe' must be available in the **remote setup**

---

## Description

This automation tool is designed to manage the **activation** and **deactivation** of any 'Gen20x.i3'
subsystems (internal, external, or OVAPI) via the backend V2 version.

### Key Features

- **GetAll() API**: Retrieves the current activation status of all subsystems.
- **Store Activation Status**: Saves the current `ActivationStatus` of the subsystems for later reference or comparison.
- **PutAll() API**: Updates the `ActivationStatus` for the subsystem based on user configurations.
- 
---

## Usage

To run this automation, ensure that:

1. You have SSH connectivity to the QNX-based head unit.
2. Your system has an active internet connection.

The automation flow is as follows:

1. **GetAll()** – Retrieve the current status of the subsystems.
2. **Store ActivationStatus** – Log the current `ActivationStatus` of each subsystem.
3. **PutAll()** – Update the subsystem's activation status to the desired values.
4. **Reboot()** - Reboot the bench via qnx
5. **GetAll()** – Retrieve the updated status of the subsystems.

---

