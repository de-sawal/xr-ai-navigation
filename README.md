# XR AI Assistant Navigation

Welcome to **XR AI Assistant Navigation**—an experimental XR application designed to support research on navigation strategies in extended reality environments. This project guides participants through a calibrated space using an AI assistant that supports multiple navigation modes (Teleport, Walking, and Shape). The experiment records data (such as trial completion times and accuracy) for further analysis.

> **Note:** This project is not intended to be a consumer application but rather a research tool for controlled experiments.

---

## Table of Contents

1. [Overview](#overview)
2. [Features](#features)
3. [Prerequisites](#prerequisites)
4. [Installation and Setup](#installation-and-setup)
   - [Cloning the Repository](#cloning-the-repository)
   - [Opening the Project in Unity](#opening-the-project-in-unity)
   - [Importing Required Packages](#importing-required-packages)
   - [Configuring XR Settings](#configuring-xr-settings)
5. [Project File Structure](#project-file-structure)
6. [Usage and Experiment Flow](#usage-and-experiment-flow)
7. [Building and Deployment Instructions](#building-and-deployment-instructions)
   - [For Mobile XR Devices](#for-mobile-xr-devices)
   - [For Desktop XR](#for-desktop-xr)
8. [Troubleshooting](#troubleshooting)
9. [License](#license)

---

## 1. Overview

**XR AI Assistant Navigation** is a Unity-based experimental application that:
- **Calibrates** the physical environment using XR input.
- **Dynamically places** study objects on calibrated surfaces.
- Offers three navigation modes (Teleport, Walking, and Shape) managed by an AI Assistant.
- Features an interactive, auto-generated UI for experiment control.
- Logs experimental trial data (in JSON and CSV formats) for subsequent analysis.

---

## 2. Features

- **Room Calibration:** Automatically scans the environment via XR inputs and raycasting.
- **Object Placement:** Dynamically positions study objects across valid surfaces.
- **AI Assistant Navigation:** Guides participants through the experiment using different navigation modes.
- **Interactive UI:** Provides a main menu, calibration feedback, trial instructions, and a questionnaire phase.
- **Data Logging:** Records detailed trial data for research purposes.

---

## 3. Prerequisites

Before setting up the project, ensure you have:

- **Unity:** Version 2021 LTS or later is recommended.
- **XR Plugin Management:** Configured for your target XR platform.
- **Optional:** Oculus Integration package (if you plan to deploy on Oculus devices).
- **Build Support:** Android Build Support (for mobile XR devices) or PC build support.

---

## 4. Installation and Setup

### Cloning the Repository

Clone the repository to your local machine using the following commands:

```bash
git clone https://github.com/yourusername/XRAIAssistantNavigation.git
cd XRAIAssistantNavigation
