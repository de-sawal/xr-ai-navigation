# XR AI Assistant Navigation – Experiment Deployment Guide

Welcome to the XR AI Assistant Navigation experiment! This document explains, in detail, how to set up the Unity project from scratch, integrate all the provided scripts, configure the scene, and deploy the experiment. This guide also covers GitHub repository setup and instructions for version control and collaboration. The goal is to help you deploy an XR app (used solely for experimental purposes) that demonstrates an AI assistant guiding participants through an immersive navigation task.

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [GitHub Repository Setup](#github-repository-setup)
4. [Unity Project Setup](#unity-project-setup)
   - [Creating a New Unity Project](#creating-a-new-unity-project)
   - [Installing XR Packages](#installing-xr-packages)
   - [Organizing Project Folders and Scripts](#organizing-project-folders-and-scripts)
   - [Scene Setup and Manager Configuration](#scene-setup-and-manager-configuration)
5. [Configuring Components and UI](#configuring-components-and-ui)
6. [Building and Deploying the Experiment](#building-and-deploying-the-experiment)
7. [Data Logging and Retrieval](#data-logging-and-retrieval)
8. [Troubleshooting and Next Steps](#troubleshooting-and-next-steps)

---

## Overview

The XR AI Assistant Navigation experiment is designed to test how participants interact with an AI assistant that guides them through an XR environment. The experiment uses several custom scripts:

- **ExperimentController**: Manages the experiment flow (main menu, calibration, object placement, trials, and questionnaire).
- **RoomCalibrationManager**: Scans and calibrates the physical environment.
- **ObjectPlacementManager**: Dynamically spawns study objects on detected surfaces.
- **InteractionManager**: Handles XR pointer input and object selection.
- **DataLogger**: Logs participant data (trial performance, navigation style, etc.).
- **AIAssistant**: Provides guidance (via teleportation, walking, or shape-based navigation) to a target object.
- **ExperimentUISetup**: Auto-generates the necessary UI panels (Main Menu, Calibration, Practice, Questionnaire).

This guide walks you through setting up a new Unity project with these scripts, configuring the necessary XR components, and deploying the experiment for data collection.

---

## Prerequisites

Before you begin, ensure you have:

- A recent version of **Unity Hub** installed.
- Unity Editor (preferably an LTS version, e.g., 2022 LTS or later).
- XR/VR device support (for example, using Oculus Integration for Quest or another XR device).  
- Android Build Support installed (if deploying to a standalone XR headset).
- A GitHub account and Git installed on your development machine.

---

## GitHub Repository Setup

This project is hosted on GitHub. Follow these steps to clone and set up the repository:

1. **Clone the Repository**  
   Open your terminal (or Git Bash) and navigate to your desired directory, then run:
```
   git clone https://github.com/de-sawal/xr-ai-navigation.git
```
2. **Navigate to the Project Folder**
```  
   cd xr-ai-navigation
```

4. **Set Up the Repository Locally**  
   - Open the project folder in Unity Hub.
   - In Unity, open the project so that the editor loads all assets and scripts.
   - (Optional) Create a new branch for your local development work:

     git checkout -b feature/setup

5. **Commit and Push Changes**  
   - As you make modifications, commit your changes with meaningful messages.
   - To push your changes to GitHub, run:

     git add .
     git commit -m "Describe your changes here"
     git push origin feature/setup

   Continue using Git to manage versions and collaborate with your team.

---

## Unity Project Setup

### Creating a New Unity Project

1. Open **Unity Hub**.
2. Click on **New Project**.
3. Select the **3D (or 3D Core)** template.
4. Name your project (e.g., "xr-ai-navigation").
5. Choose a project location.
6. Click **Create** and wait for the project to load.

### Installing XR Packages

Since this project targets XR experimentation, you’ll need to install and configure the appropriate XR packages:

1. Open **Window > Package Manager**.
2. Install **XR Plugin Management**.
3. Under **Project Settings > XR Plugin Management**, enable the plugin for your target platform (for example, enable **Oculus** under Android if you’re deploying on a Quest device).
4. (Optional) If you prefer using Unity’s XR Interaction Toolkit, install it via the Package Manager. Note that some scripts may need minor adjustments if you choose the XR Interaction Toolkit over the classic VR SDK.

### Organizing Project Folders and Scripts

For clarity and maintainability, create the following folder structure in your **Assets** directory:

- **Assets/_Scripts/** – Place all provided scripts here:
  - AIAssistant.cs
  - DataLogger.cs
  - ExperimentController.cs
  - ExperimentUISetup.cs
  - InteractionManager.cs
  - NavigationStyle.cs
  - ObjectPlacementManager.cs
  - RoomCalibrationManager.cs

- **Assets/_Prefabs/** – Store any prefabs (e.g., study objects, UI elements).
- **Assets/_Scenes/** – Create and save your scene(s) here.
- **Assets/_Materials/** – Save any custom materials (e.g., highlight material).
- **Assets/_UI/** – Optional folder for custom UI assets.

Copy the provided scripts into the `_Scripts` folder and let Unity compile them.

### Scene Setup and Manager Configuration

1. **Create a New Scene**  
   - In the `_Scenes` folder, create a new scene named “MainExperimentScene” and open it.

2. **XR Rig Setup**  
   - Remove the default Main Camera from the scene.
   - Import your XR rig (for example, drag the `OVRCameraRig` prefab from the Oculus Integration package into the scene).

3. **Environment Setup**  
   - Add a simple floor (e.g., create a Plane from **GameObject > 3D Object > Plane** and position it at `(0, 0, 0)`).

4. **Create a Manager Object**  
   - In the Hierarchy, create an empty GameObject named “ExperimentManager”.
   - Reset its transform.
   - Attach the following scripts to it (drag them from the Project window into the Inspector):
     - ExperimentController
     - RoomCalibrationManager
     - ObjectPlacementManager
     - InteractionManager
     - DataLogger
     - AIAssistant

5. **Assign Script References**  
   - In the **ExperimentController** component, assign the following:
     - `calibrationManager`: Drag the RoomCalibrationManager component.
     - `objectManager`: Drag the ObjectPlacementManager component.
     - `assistant`: Drag the AIAssistant component.
     - `interactionManager`: Drag the InteractionManager component.
     - `dataLogger`: Drag the DataLogger component.
     - `uiSetup`: (See below for creating the UI setup object.)

---

## Configuring Components and UI

### ExperimentUISetup

1. **Create the UI Manager Object**  
   - In the Hierarchy, create an empty GameObject named “UIManager”.
   - Attach the **ExperimentUISetup** script to this object.
   - The script auto-generates all necessary UI panels (Main Menu, Calibration, Practice, Questionnaire) at runtime. You can adjust their properties later if needed.

2. **Link UIManager in ExperimentController**  
   - In the ExperimentController component (on ExperimentManager), drag the UIManager object into the `uiSetup` field.

### InteractionManager Configuration

1. **Pointer Line Setup**  
   - Under your XR rig’s right-hand controller (for example, inside `OVRCameraRig/RightHandAnchor`), create an empty GameObject named “PointerLine”.
   - Add a **Line Renderer** component to this object.
   - Assign the PointerLine GameObject to the `pointerLine` field in the InteractionManager.
2. **Layer and Material Setup**  
   - Create a new layer (e.g., "Interactable") via **Edit > Project Settings > Tags and Layers** and assign it as needed.
   - Create a highlight material in the `_Materials` folder and assign it to the InteractionManager’s `highlightMaterial`.

### ObjectPlacementManager Setup

1. **Study Object Prefabs**  
   - Create or import several 3D models to serve as the study objects.
   - Save them as prefabs in the `_Prefabs` folder.
   - Populate the `studyObjectPrefabs` array in the ObjectPlacementManager component with these prefabs.
2. **Configure Placement Constraints**  
   - Adjust parameters such as `minObjectSpacing`, `preferredObjectArea`, and `minSurfaceAreaThreshold` in the Inspector as needed for your experimental setup.

### AIAssistant Configuration

1. **Assistant Model and Animator**  
   - Import or create a simple 3D model to represent the AI assistant (this could be a capsule or a custom model).
   - Assign the model to the `assistantModel` field and, if available, set up an Animator Controller for the `robotAnimator`.
2. **Navigation Modes**  
   - The AIAssistant supports teleportation, walking, and shape-based guidance. Ensure the corresponding parameters (speed, rotation, effects) are configured.

---

## Building and Deploying the Experiment

This experiment is not a consumer app but an experimental tool for data collection. Follow these steps to build and deploy:

### In-Editor Testing

1. Connect your XR device (or use XR simulation tools) to test the experiment within the Unity Editor.
2. Press **Play** and verify that:
   - The UI (Main Menu, Calibration instructions, etc.) is visible in world space.
   - The XR rig tracks head and controller movements correctly.
   - The calibration and object placement procedures work.
   - The AIAssistant correctly guides the user when help is requested.

### Building for Deployment

1. Open **File > Build Settings**.
2. Ensure your “MainExperimentScene” is added to the Scenes In Build list.
3. Switch the target platform:
   - For a standalone XR headset (e.g., Quest), select **Android** and click **Switch Platform**.
4. Configure **Player Settings**:
   - Set the **Company Name** and **Product Name**.
   - In **Other Settings**, set the **Minimum API Level** (at least API 23 for Quest).
   - For **Scripting Backend**, choose IL2CPP.
   - Under **Target Architectures**, enable ARM64.
5. Click **Build** (or Build and Run) to generate the APK.
6. Deploy the APK to your XR device using adb or a tool like SideQuest.

---

## Data Logging and Retrieval

The DataLogger script writes experiment data as JSON and CSV files in the folder:

Application.persistentDataPath/ExperimentData

On an Android-based XR device, this is typically located at:

/storage/emulated/0/Android/data/[your-package-name]/files/ExperimentData

To retrieve data:
1. Connect your device via USB.
2. Use adb to navigate to the directory.
3. Copy the data files to your computer using a command like:

adb pull /storage/emulated/0/Android/data/[your-package-name]/files/ExperimentData [destination-folder]

This data includes session logs and trial CSV exports for later analysis.

---

## Troubleshooting and Next Steps

- **Input Issues:**  
  Verify that your XR device is correctly configured. If XR inputs (like OVRInput.Button.One) do not respond, check your XR plugin settings and device connections.

- **UI Visibility:**  
  Ensure the generated Canvas is in world space and positioned in front of the user (the default is at position (0, 1.6, 2)). Adjust the canvas scale and position if needed.

- **Performance:**  
  Monitor performance during calibration and object placement. Adjust scanning resolution and raycast frequency in RoomCalibrationManager if you experience frame drops.

- **GitHub Workflow:**  
  Continue using Git for version control. Branch off new features or experiment changes, merge them into your main branch, and tag versions as needed.

- **Experiment Customization:**  
  Adapt parameters such as navigation speed, object placement density, and UI text to suit your experimental design. Consider integrating additional feedback mechanisms if required.

---

## Final Remarks

This guide should help you get the XR AI Assistant Navigation experiment up and running, from cloning the GitHub repository to deploying the experiment on your XR device. If you have any questions or need further assistance, please refer to the project repository issues or contact the development team.

Happy experimenting!

