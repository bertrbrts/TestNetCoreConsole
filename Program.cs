using Microsoft.MixedReality.WebRTC;
using TestNetCoreConsole;



AudioTrackSource microphoneSource = null;
VideoTrackSource webcamSource = null;
Transceiver audioTransceiver = null;
Transceiver videoTransceiver = null;
LocalAudioTrack localAudioTrack = null;
LocalVideoTrack localVideoTrack = null;



try
{
    bool needVideo = true; //Array.Exists(args, arg => (arg == "-v") || (arg == "--video"));
    bool needAudio = false; //Array.Exists(args, arg => (arg == "-a") || (arg == "--audio"));

    var deviceList = await DeviceVideoTrackSource.GetCaptureDevicesAsync();
    foreach (var device in deviceList)
    {
        Console.WriteLine($"Found webcam {device.name} (id: {device.id})");
    }

    using var pc = new PeerConnection();
    using var signaler = new NamedPipeSignaler(pc, "testpipe");

    var config = new PeerConnectionConfiguration
    {
        IceServers = new List<IceServer> {
            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
        }
    };

    await pc.InitializeAsync(config);
    Console.WriteLine("Peer connection initialized");

    if (needVideo)
    {
        Console.WriteLine("Opening local webcam...");
        webcamSource = await DeviceVideoTrackSource.CreateAsync();

        Console.WriteLine("Create local video track...");
        var videoTrackConfig = new LocalVideoTrackInitConfig { trackName = "webcam_track" };
        localVideoTrack = LocalVideoTrack.CreateFromSource(webcamSource, videoTrackConfig);

        Console.WriteLine("Create video transceiver and add webcam track...");
        videoTransceiver = pc.AddTransceiver(MediaKind.Video);
        videoTransceiver.LocalVideoTrack = localVideoTrack;
        videoTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
    }

    if (needAudio)
    {
        Console.WriteLine("Opening local microphone...");
        microphoneSource = await DeviceAudioTrackSource.CreateAsync();

        Console.WriteLine("Create local audio track...");
        var audioTrackConfig = new LocalAudioTrackInitConfig { trackName = "microphone-track" };
        localAudioTrack = LocalAudioTrack.CreateFromSource(microphoneSource, audioTrackConfig);

        Console.WriteLine("Create audio transceiver and add mic track...");
        audioTransceiver = pc.AddTransceiver(MediaKind.Audio);
        audioTransceiver.LocalAudioTrack = localAudioTrack;
        audioTransceiver.DesiredDirection = Transceiver.Direction.SendReceive;
    }

    // Setup signaling
    Console.WriteLine("Starting signaling...");
    signaler.SdpMessageReceived += async (SdpMessage message) => 
    {
        await pc.SetRemoteDescriptionAsync(message);
        if (message.Type == SdpMessageType.Offer)
        {
            pc.CreateAnswer();
        }
    };

    signaler.IceCandidateReceived += (IceCandidate candidate) => 
    { 
        pc.AddIceCandidate(candidate); 
    };

    await signaler.StartAsync();


}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}

localAudioTrack?.Dispose();
localVideoTrack?.Dispose();
microphoneSource?.Dispose();
webcamSource?.Dispose();