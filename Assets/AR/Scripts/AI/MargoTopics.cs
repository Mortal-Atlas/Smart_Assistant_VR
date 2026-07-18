public static class MargoTopics
{
    // The Rebrand: All topics use 'rika' for code/routing, but the AI is Margo
    public const string VoiceListen = "rika/voice/listen";
    public const string VoiceInput = "rika/voice/input";
    public const string AIResponse = "rika/response";
    public const string Commands = "rika/commands";
    public const string AppSwitch = "rika/app/switch";
    
    // Modules
    public const string VisionCapture = "rika/vision/capture";
    public const string VisionResult = "rika/vision/result";
    public const string SpotifyToggle = "rika/haos/spotify/toggle";
    public const string SpotifyState = "rika/spotify/state";
    
    // Phone & Pet State
    public const string PhoneTouch = "rika/phone/touch";
    public const string PhoneCamera = "rika/phone/camera";
    public const string PhoneRumble = "rika/phone/rumble";
    public const string PetState = "rika/pet/state";
    public const string CombatInput = "rika/game/combat";
    public const string FishingCast = "rika/game/fishing/cast";
    
    // System
    public const string VRStatus = "vr/status";
}