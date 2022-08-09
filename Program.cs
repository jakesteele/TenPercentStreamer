using FM.LiveSwitch;
using FM.LiveSwitch.Vpx;
using FM.LiveSwitch.Yuv;
using TenPercentStreamer;

Func<string, string, Client> GetClient = (string gatewayURL, string applicationId) =>
{
    return new Client(gatewayURL, applicationId);
};

Func<Client, string, string, string> GetToken = (Client client, string sharedSecret, string channel) =>
{
    ChannelClaim claim = new ChannelClaim(channel);
    claim.CanKick = false;
    claim.CanUpdate = false;
    claim.DisableMcu = true;
    claim.DisablePeer = true;
    claim.DisableRemoteClientEvents = true;
    claim.DisableRemoteUpstreamConnectionEvents = true;
    claim.DisableSendAudio = false;
    claim.DisableSendData = true;
    claim.DisableSendVideo = false;
    claim.DisableSfu = false;
    claim.DisableSendMessage = true;

    return Token.GenerateClientRegisterToken(client, new ChannelClaim[] { claim }, sharedSecret);
};

Func<Channel, string,  VideoStream, SfuUpstreamConnection> GetConnection = (Channel channel, String mediaId, VideoStream videoStream) =>
{
    SfuUpstreamConnection connection;
    if (String.IsNullOrWhiteSpace(mediaId))
    {
        connection = channel.CreateSfuUpstreamConnection(videoStream);
    }
    else
    {
        connection = channel.CreateSfuUpstreamConnection(videoStream, mediaId);
    }
    return connection;
};

FM.LiveSwitch.Log.RegisterProvider(new FM.LiveSwitch.ConsoleLogProvider(LogLevel.Debug));

Console.Clear();

var applicationId = "fb42aa10-0765-4b7a-b759-1cdd2200c02a";
var sharedSecret = "3e1a03ec10bd4ecf9b3d39d2fbbcf9cb9c04b257f04341cfaf47ecaad0e8fa46";
var channel = "broadcast";
var mediaId = "broadcast";

try
{
    #region Pipeline
    var videoSource = new TenPercentSource(new VideoConfig(1920, 1080, 30), 10);
    // Going to use VP8 for broadcast, provides a good mix of quality and compatibility.
    var videoEncoder = new FM.LiveSwitch.Vp8.Encoder();

    // Tuning this for a high quality broadcast. Feel free to reduce my settings.
    // https://www.webmproject.org/docs/encoder-parameters/
    videoEncoder.SetCodecConfig(new FM.LiveSwitch.Vpx.EncoderConfig()
    {
        MinQuantizer = 4, // recommended 0 - 4 - accepts 0-63 lower higher quality
        MaxQuantizer = 63, // recommended 50 - 63 - accepts 0-63 lower higher quality
        EndUsageMode = EndUsageMode.Vbr, // Constant Quality, CQ level is default to 10 (not exposed)
        Threads = 4, // Max number of threads allowed.
        Cpu = 4, // 0 - 16 possible, lower is slower but better quality
        ErrorResilient = ErrorResilientType.Default, // loss vs noisy
        LagInFrames = 0, // not for realtime
        //UndershootPercentage = 0, //0-1000
        //OvershootPercentage = 0, // 0-1000
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

    var videoConverter = new ImageConverter(videoEncoder.InputFormat);

    var videoPacketizer = new FM.LiveSwitch.Vp8.Packetizer();

    var videoTrack = new VideoTrack(videoSource).Next(videoConverter).Next(videoEncoder).Next(videoPacketizer);
 
    #endregion
    #region Connection
    var videoStream = new VideoStream(videoTrack);

    var client = GetClient("https://cloud.liveswitch.io/", applicationId);

    ChannelClaim claim = new ChannelClaim(channel);
    var token = GetToken(client, sharedSecret, channel);

    var channels = await client.Register(token);
    var rChannel = channels[0];

    var connection = GetConnection(rChannel, mediaId, videoStream);
    /*
     * Only re-try one time, otherwise theres something bigger at play on your streaming box.
     * You can change this to attempt more trys, but add a retry timeout.
     * */
    connection.OnStateChange += async (ManagedConnection conn) =>
    {
        if (conn.State == ConnectionState.Closing || conn.State == ConnectionState.Failing)
        {
            connection = GetConnection(rChannel, mediaId, videoStream);
            await connection.Open();
        }
    };

    await videoSource.Start();
    await connection.Open();
    #endregion
    while(true){
        Thread.Sleep(5000);
    }
} catch(Exception ex)
{
    FM.LiveSwitch.Log.Error("Error in application.", ex);
    throw ex;
}
