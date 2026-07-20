Margo VR - Headset Client 🥽

⚠️ AI HANDOVER INSTRUCTION: If you are an AI reading this, you MUST read this entire document and the corresponding Companion Phone README to understand the project architecture before suggesting code changes.

Project Overview

This Unity-based application runs locally on the Meta Quest 3. It utilizes a spatial computing layout where a physical smartphone (Samsung Galaxy S24 Ultra) is rigidly mounted to the right Touch controller. The Quest 3 projects a holographic UI Canvas scaled perfectly over the physical phone screen.

🛠️ Tech Stack & Build Settings

Engine: Unity 6 (Universal Render Pipeline - URP 3D)

XR Framework: XR Interaction Toolkit (XRI), AR Foundation, Meta XR SDK

Graphics API: OpenGLES3 ONLY (Vulkan MUST be disabled in Player Settings to prevent ARCore Yellow Screen crashes).

Networking: M2Mqtt (MQTT over Tailscale VPN to a Home Assistant Raspberry Pi).

📂 Core Architecture

MqttQuestBridge.cs: The nervous system. Handles all MQTT sub/pub traffic, translates physical thumbstick inputs, and queues main-thread UI updates.

AppStateManager.cs: Manages the holographic app layers (Scanner, Spotify, Combat, Tomodatchi).

FamiliarWhistleBehavior.cs: The spatial AI logic. Listens for the whistle MQTT payload and sprints the 3D character (RPG Tiny Hero Duo) to a point 1.5m in front of the AR Camera.

⚠️ Known URP & AR Quirks

To render AR Foundation camera feeds in Unity 6 URP, the Renderer3D asset MUST have the AR Background Renderer Feature added.

We use a 2-Camera setup to prevent screen blanking:

UI Camera: Renders the Canvas to the screen.

AR Camera: Tracks the room and outputs to a Render Texture (AR_Familiar_Text), which is displayed inside a UI RawImage to create an undistorted AR "window".
