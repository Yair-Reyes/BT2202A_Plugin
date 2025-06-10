# BT2202A TAP Plugin for Keysight Instruments

![Keysight Logo](Plot_Data_/WindowsFormsApp1_testCSV/Keysight_logo.png)

## 📝 Overview

This OpenTAP plugin provides advanced automation for **Keysight's BT2202A Charge-Discharge System** - a specialized instrument for Li-ion cell formation and testing. The plugin transforms the BT2202A into a programmable testing platform with sophisticated sequence capabilities for battery research, development, and production.

## ⚡ Key Features

### Core Functionality
- **Complete Test Suite**: Comprehensive charge, discharge, and measurement operations with configurable parameters
- **Real-time Monitoring**: Live data visualization through the integrated plotting tool
- **Flexible Cell Management**: Control individual cells or cell groups with customizable channel assignments
- **Smart Test Logic**: Conditional testing with pass/fail verdicts based on voltage/current thresholds

### Advanced Capabilities (`BT2202A_2025` branch)
- **Parallel Test Execution**: Run multiple tests simultaneously with synchronized OpenTAP instances
- **Inter-Process Communication**: JSON-based coordination for distributed testing
- **Hardware Resource Management**: Intelligent allocation of instrument resources with safety limits
- **SCPI Command Optimization**: Workarounds for limitations in simultaneous operations

## 🔧 Components

### Test Steps
- **Charge**: Configure voltage, current, duration, and cell assignments
- **Discharge**: Set discharge parameters with customizable thresholds
- **Measurement**: Collect and analyze voltage and current data
- **Duration Test**: Create pass/fail conditions based on electrical parameters
- **Abort**: Safely terminate operations with proper shutdown procedures

### Data Visualization
- **Real-time Plotting**: Live visualization of voltage and current data
- **CSV Export**: Automated data logging for analysis and reporting
- **Test Reports**: Comprehensive result summaries with verdict information

### Communication Layer
- **SCPI Command Interface**: Direct instrument control through standard commands
- **JSON Configuration**: Flexible parameter management through external configuration files
- **Status Monitoring**: Real-time test status tracking across distributed processes

## 💻 Requirements

### Development Environment
- **Visual Studio** 2022 or later
- **.NET Framework** 4.7.2 or higher
- **OpenTAP** framework (latest version)

### Hardware Requirements
- **Keysight BT2202A** instrument with SCPI command support
- Network connectivity for TCP/IP communication with the instrument
- Sufficient system resources for real-time data processing and visualization

## 🚀 Getting Started

### Installation
1. Clone the repository
2. Open the solution file in Visual Studio
3. Build the solution to generate the plugin DLL
4. Install in your OpenTAP environment

### Configuration
1. Connect to your BT2202A instrument via TCP/IP
2. Configure the instrument address in the OpenTAP plugin settings
3. Create test sequences with the provided test steps

### Running Tests
1. Design test sequences using the OpenTAP GUI
2. Configure charge/discharge parameters and cell assignments
3. Add measurement and condition steps
4. Execute the sequence and monitor results in real-time

## 📊 Data Analysis

The plugin includes a dedicated data visualization tool that provides:

1. **Real-time plots** of voltage and current data
2. **CSV export** for offline analysis
3. **Cell comparison** across multiple channels
4. **Test report generation** with verdict statistics

## 🔌 API Reference

The plugin exposes a comprehensive API for programmatic control:

- **ScpiInstrument**: Base communication interface
- **Charge/Discharge**: Control functions for energy transfer operations
- **Measurement**: Data acquisition and analysis
- **JSON Manager**: Configuration and inter-process communication
- **Status Monitoring**: Test progression and verdict tracking

## 👥 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch
3. Implement your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the Apache License 2.0 - see the LICENSE file for details.

---

*Developed by the BT2202A Plugin Development Team © 2025*
