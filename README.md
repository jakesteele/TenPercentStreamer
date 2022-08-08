# TenPercentStreamer
10 Percent Streamer

## Settings (Program.cs)
Ln 55 - Width, Height, FrameRate, Change Percent
```
var videoSource = new TenPercentSource(new VideoConfig(1920, 1080, 30), 10);
```

Ln 60 - Vp8 Encoder Settings (tuned for speed on my macbook not quality)
```
videoEncoder.SetCodecConfig(new FM.LiveSwitch.Vpx.EncoderConfig()
{
    MinQuantizer = 4, // recommended 0 - 4 - accepts 0-63 lower higher quality
    MaxQuantizer = 63, // recommended 50 - 63 - accepts 0-63 lower higher quality
    EndUsageMode = EndUsageMode.Vbr, // Constant Quality, CQ level is default to 10 (not exposed)
    Threads = 4, // Max number of threads allowed.
    Cpu = 4, // 0 - 16 possible, lower is slower but better quality
    ErrorResilient = ErrorResilientType.Default, // loss vs noisy
    LagInFrames = 0, // not for realtime
    UndershootPercentage = 0, //0-1000
    OvershootPercentage = 0, // 0-1000
    KeyframeMode = KeyframeMode.Disabled, // we will produce when requested
    ResizeAllowed = false, // allow encoding and sending a smaller frame thats upscaled in decoder
    ResizeDownThreshold = 30, // 0-100
    ResizeUpThreshold = 70, // resizedown-100
    Profile = 0,
    Usage = 0,
    BufferSize = 6500,
    BufferInitialSize = 4500,
    BufferOptimalSize = 5000
});
videoEncoder.TargetBitrate = 4500; // Recommended value depends on frame rate and resolution. https://stream.twitch.tv/encoding/
videoEncoder.MaxBitrate = 6500; // Recommended value depends on frame rate and resolution. 
```

## Docker Setup
- docker build -t tenpercent-streamer -f Dockerfile .
- docker run -it --rm tenpercent-streamer 

