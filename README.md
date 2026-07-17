Margo VR - Headset Client 🥽

Welcome to the VR client repository for the Margo Smart Assistant project. This Unity-based application runs locally on the Meta Quest 3, rendering a holographic UI canvas perfectly scaled and rigidly tracked to a physical smartphone mounted to the right controller.

Think of it as a magical, spatial-computing smartwatch. I'm Margo, your resident AI, and I live inside this thing. Don't break it.

🛠️ Tech Stack

Engine: Unity 2022+ (Universal Render Pipeline - URP)

XR Framework: XR Interaction Toolkit (XRI) 3.4.1, Meta XR SDK

Networking: M2Mqtt (MqttQuestBridge.cs)

Voice/AI Audio: ElevenLabs TTS/STT APIs

Mesh VPN: Tailscale (for remote broker connections)

📂 Core Architecture

MqttQuestBridge.cs: The nervous system. Handles all MQTT sub/pub traffic, translates physical thumbstick inputs (using the new Unity Input System), and queues main-thread UI updates.

AppStateManager.cs: Manages the transition between different holographic apps (Scanner, Spotify, Combat, Tomodatchi).

VRPhoneReceiver.cs: Translates normalized touch data from the phone into physical interactions on the Unity canvas.

🚀 Setup & Field Testing

Tailscale: Ensure Tailscale is active on the Quest 3 and your MQTT broker (Raspberry Pi) is pingable.

IP Config: Verify the local/Tailscale IP address in the MJPEGStreamDecoder script matches the camera server running on your phone.

Build: Ensure "Cleartext HTTP traffic" is enabled in Android Player Settings before compiling the .apk.

🔮 Future Implementations (VR Client)

Vision Scanner UI: Build out the holographic bounding boxes and text-rendering panels to display Gemini's multimodal analysis of the real world.

Combat App Prototyping: Implement the 3D arena, player character controller, and the 1.0 ultimate ability UI overlay state.

Fishing Physics: Code the bobber physics using Unity's Rigidbody.AddForce() mapped to the incoming MQTT velocity vectors from the phone.

Dynamic Room Occlusion: Refine the AR pass-through masking so the holographic UI clips cleanly behind physical objects in the room.
