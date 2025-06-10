# BT2202A TAP Plugin Development 2025

### Overview

This project focuses on the development of a plugin for **Keysight’s BT2202A Charge-Discharge, Li-ion Cell Formation, and Test Solution**, using the **OpenTAP** framework. The plugin enables the design and execution of automated test sequences for energy storage cells, expanding the capabilities of the BT2202A for R&D and production testing.

The `BT2202A_2025` branch showcases an updated version of the plugin with advanced test logic and integration improvements, including JSON-based communication and parallel test execution support.

---

### 🎥 Video Demonstration


---

## 🚀 Features

### Basic Plugin (`main` branch)
- Auto-detection of BT2202A instrument within OpenTAP.
- Drag-and-drop test steps for:
  - Charge
  - Discharge
  - Measurement
- Basic Pass/Fail verdicts.
- Modular, open-source architecture for custom extensions.

### Advanced Plugin (`BT2202A_2025` branch)
- **Parallel test execution** using synchronized TAP instances.
- **JSON-based inter-process communication** for coordinating steps.
- Intelligent flags and logic for managing hardware limits.
- Workarounds for SCPI limitations in simultaneous tests.

---

## 💻 Requirements

### Software
- **Visual Studio**: Use the solution file:
