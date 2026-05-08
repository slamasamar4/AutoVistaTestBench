# 🚗 AutoVista Test Bench

**Industrial Automotive Test System Simulation built with C# / .NET 8 and WPF (MVVM)**

---

## 📌 Overview

AutoVista Test Bench is a desktop application that simulates a real-world automotive test system used in production and R&D environments.

The system emulates communication between electronic control units (ECUs), sensors, and PC-based applications, enabling real-time monitoring, data acquisition, and anomaly detection.

This project is designed to reflect **industry-level architecture and practices**, similar to software used in automotive testing environments.

---

## 🎯 Objectives

* Simulate automotive hardware systems (sensors, ECU modules, CAN bus)
* Process real-time data streams using multithreading
* Provide a monitoring dashboard via WPF UI
* Apply clean architecture and separation of concerns
* Demonstrate production-level C#/.NET development skills

---

## 🏗️ Architecture

The project follows a **layered clean architecture**:

```
AutoVistaTestBench/
│
├── Core/           → Domain models, enums, interfaces
├── Simulator/      → Hardware & signal simulation
├── Services/       → Business logic & system services
├── UI (WPF)/       → Views, ViewModels (MVVM)
├── Tests/          → Unit & integration tests
```

### 🔄 Data Flow

```
Simulator → Services → ViewModels → UI
                ↓
             Logging / AI Analysis
```

---

## ⚙️ Technologies

* **C# / .NET 8**
* **WPF (MVVM Pattern)**
* Multithreading / Async Programming
* Dependency Injection
* Unit Testing (xUnit)
* Logging & Monitoring
* Git / Version Control

---

## 🔌 Key Features

### 🧪 Hardware Simulation

* Temperature & voltage sensors
* ECU module behavior
* CAN bus frame generation
* Fault injection system

### 📊 Real-Time Monitoring

* Live data updates
* Channel status tracking
* Dynamic UI binding (MVVM)

### 📄 Logging & Analysis

* Structured logging system
* Log severity classification
* (Optional) AI-based anomaly detection

---

## 🖥️ UI Preview (WPF)

* Dashboard view (system overview)
* Channel monitor (real-time signals)
* Log analyzer (system events & anomalies)

---

## 🚀 Getting Started

### 🔧 Requirements

* Windows OS (WPF required)
* .NET 8 SDK
* Visual Studio (recommended)

---

### ▶️ Run the Project

1. Clone the repository:

```bash
git clone https://github.com/your-username/AutoVistaTestBench.git
cd AutoVistaTestBench
```

2. Open the solution:

```
AutoVistaTestBench.sln
```

3. Restore packages & build:

* In Visual Studio:
  `Ctrl + Shift + B`

4. Run the application:

* Press `F5`

---

## 🧪 Testing

Run tests using:

```bash
dotnet test
```

Includes:

* Core model tests
* Service layer tests

---

## 📦 Project Structure (Detailed)

* **Core** → Business entities & contracts
* **Simulator** → Mock hardware environment
* **Services** → Data acquisition & processing
* **UI** → WPF + MVVM implementation
* **Tests** → Validation of logic & behavior

---

## 🧠 Learning Highlights

This project demonstrates:

* Designing modular systems (Clean Architecture)
* Applying MVVM in WPF applications
* Handling real-time data with multithreading
* Simulating hardware-software interaction
* Writing testable and maintainable C# code

---

## ⚠️ Notes

* WPF UI requires **Windows OS**
* Core & Services layers can run cross-platform


## ⭐ Why this project?

This project was built to bridge the gap between web development experience and industrial software engineering, targeting roles in automotive R&D environments.

