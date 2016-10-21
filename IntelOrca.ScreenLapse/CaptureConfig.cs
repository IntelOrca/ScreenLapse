namespace IntelOrca.ScreenLapse
{
    internal class CaptureConfig
    {
        public string Output { get; set; }
        public string[] Include { get; set; }
        public string[] Exclude { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Interval { get; set; }
    }
}
