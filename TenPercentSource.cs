using System;
using FM.LiveSwitch;
namespace TenPercentStreamer
{
    public class TenPercentSource : CameraSourceBase
    {
        static IDataBufferPool _DataBufferPool = DataBufferPool.GetTracer(typeof(TenPercentSource));

        /// <summary>
        /// Gets a label that identifies this class.
        /// </summary>
        public override string Label
        {
            get { return "Ten Percent Source"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeAudioSource" /> class.
        /// </summary>
        /// <param name="config">The output configuration.</param>
        /// <param name="percentChange">Percent to change each frame.</param>
        public TenPercentSource(VideoConfig config, int percentChange)
            : this(config, percentChange, VideoFormat.Bgr)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FakeAudioSource" /> class.
        /// </summary>
        /// <param name="config">The output configuration.</param>
        /// <param name="percentChange">Percent of the frame to change each run.</param>
        /// <param name="format">The output format.</param>
        public TenPercentSource(VideoConfig config, int percentChange, VideoFormat format)
            : base(format, config)
        {
            var minimumBufferLength = VideoBuffer.GetMinimumBufferLength(config.Width, config.Height, format.Name);
            picture = new byte[minimumBufferLength];
            this.percentChange = percentChange * 0.01;
        }

        private ManagedTimer _Timer;
        private double _Hue;
        private double _Saturation;
        private double _Brightness;
        private byte[] picture;
        private double percentChange;

        /// <summary>
        /// Starts this instance.
        /// </summary>
        protected override Future<object> DoStart()
        {
            var promise = new Promise<object>();
            try
            {
                _Hue = 0.0;
                _Saturation = 1.0;
                _Brightness = 1.0;
                red = rand.Next(0, 255);
                blue = rand.Next(0, 255);
                green = rand.Next(0, 255);

                Config = TargetConfig;

                _Timer = new ManagedTimer((int)(1000 / Config.FrameRate), RaiseData);
                _Timer.Start();

                promise.Resolve(null);
            }
            catch (Exception ex)
            {
                promise.Reject(ex);
            }
            return promise;
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        protected override Future<object> DoStop()
        {
            return _Timer.StopAsync();
        }

        Random rand = new Random();
        int red;
        int blue;
        int green;

        int index = 0;
        private void RaiseData()
        {
            var config = Config;

            int changeSize = Convert.ToInt32(picture.Length * percentChange);

            for (int l = (index + changeSize); index < l;)
            {
                Binary.ToBytes8(blue, picture, index++);
                Binary.ToBytes8(green, picture, index++);
                Binary.ToBytes8(red, picture, index++);
            }
            if (index >= picture.Length)
            {
                index = 0;
                red = rand.Next(0, 255);
                blue = rand.Next(0, 255);
                green = rand.Next(0, 255);
            }

            var dataBuffer = DataBuffer.Wrap(picture);
            try
            {
                var videoBuffer = new VideoBuffer(config.Width, config.Height, dataBuffer, VideoFormat.Bgr);
                RaiseFrame(new VideoFrame(videoBuffer));
            }
            catch (Exception ex)
            {
                Log.Error("Could not raise frame.", ex);
            }
            finally
            {
                dataBuffer.Free();
            }
        }
    }
}
