using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using InputManager;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

class BHReplayViewer : ReplayViewer
{
    public BHReplayViewer() : base("Brawlhalla", "Brawlhalla", false)
    {
        Title = (Bitmap)System.Drawing.Image.FromFile("Images/Title.png");
    }

    public void StartLoop(int ReplaysToPlay, Keys RecordStart, Keys RecordStop)
    {
        NoErrors = true;
        getProcess();

        if (NoErrors)
        {
            previousscreen = ScreenGrabber.PrintWindow(hWnd);
            ResX = previousscreen.Width;
            ResY = previousscreen.Height;
            
            if ((double)ResX/ResY > 4.0/3.0)
            {   //Scaling factor for normal resolutions
                ScalingFactor = ResY / 1080.0;
                Res4by3 = false;
            }
            else
            {   //Scaling factor for 4:3 resolutions
                ScalingFactor = ResX / (1080.0 * 1.5);
                Res4by3 = true;
            }

            ResizeBilinear Resize = new ResizeBilinear( Convert.ToInt32(220 *ScalingFactor) , Convert.ToInt32(146 *ScalingFactor) );
            Title = Resize.Apply(Title);

            CurrentReplayLocation = FindCurrentLocation(previousscreen);

            LoopThread = new Thread(() => PlaybackLoop(ReplaysToPlay, RecordStart, RecordStop));
            LoopThread.Start();
        }
        else
        {
            OnLoopEnd(new EventArgs());
        }
    }

    protected override void MenuStateActive(ref bool menu)
    {
        screen = ScreenGrabber.PrintWindow(hWnd);

        // Need different method to find out if were at the main menu
        // The logo is not a constant across patches
        menu = ScreenGrabber.Contains(screen, Title);

        if (menu)
            return;

        Difference diff = new Difference(screen);

        if (ScreenGrabber.IsBlack(diff.Apply(previousscreen)))
        {
            Keyboard.KeyDown(Keys.Escape);
            Thread.Sleep(50);
            Keyboard.KeyUp(Keys.Escape);
            Thread.Sleep(50);
            Keyboard.KeyDown(Keys.Up);
            Thread.Sleep(50);
            Keyboard.KeyUp(Keys.Up);
            Thread.Sleep(50);
            Keyboard.KeyDown(Keys.Enter);
            Thread.Sleep(50);
            Keyboard.KeyUp(Keys.Enter);
            return;
        }
        previousscreen = screen;
    }

    protected override void NavigateDefault()
    {
        // Click on replay button
        if (!Res4by3)
        {   // normal resolutions
            Mouse.Move(Convert.ToInt32(ResX - 65 * ScalingFactor), Convert.ToInt32(61 * ScalingFactor));
        }
        else
        {   // 4:3 resolutions
            Mouse.Move(Convert.ToInt32(ResX - 65 * ScalingFactor), Convert.ToInt32(61 * ScalingFactor + ResY/2 - ResX/3));
        }
        
        Thread.Sleep(20);
        Mouse.PressButton(Mouse.MouseKeys.Left);

        Thread.Sleep(100);

        // TODO - navigate to appropriate replay
        for (int i = 0; i < CurrentReplayLocation; i++)
        {
            Keyboard.KeyDown(Keys.Down);
            Thread.Sleep(50);
            Keyboard.KeyUp(Keys.Down);
            Thread.Sleep(50);
        }

        Keyboard.KeyDown(Keys.Enter);
        Thread.Sleep(50);
        Keyboard.KeyUp(Keys.Enter);
        Mouse.Move(9999, 0);
    }

    protected override void GameNotOpen()
    {
        base.GameNotOpen();
        ProgressText = "Brawlhalla was not open";
        NoErrors = false;
    }

    protected override void InMatchInputs()
    {

    } 

    protected int FindCurrentLocation(Bitmap bmp)
    {
        HSLFiltering filter = new HSLFiltering
        {
            Saturation = new Range(0.99f, 1f),
            Luminance = new Range(0.09f, 0.11f),
            Hue = new IntRange(239, 241)
        };

        bmp = filter.Apply(bmp);

        for (int i = 0; i < 17; i++)
        {
            Crop crop = new Crop(new Rectangle(ReplayBoundsX[0], ReplayBoundsY[i, 0], ReplayBoundsX[1] - ReplayBoundsX[0], ReplayBoundsY[i, 1] - ReplayBoundsY[i, 0]));
            ImageStatisticsHSL stat = new ImageStatisticsHSL(crop.Apply(bmp));
            if (stat.Luminance.Mean < 0.001)
                return i;
        }
        return -1;
    }

    private double ScalingFactor;
    private int ResX;
    private int ResY;
    private int CurrentReplayLocation;
    private int[] ReplayBoundsX = new int[2] { 989, 1527 };
    private int[,] ReplayBoundsY = 
    {
        { 144, 176 },
        { 186, 218 },
        { 228, 260 },
        { 270, 302 },
        { 312, 344 },
        { 354, 386 },
        { 397, 429 },
        { 439, 471 },
        { 481, 513 },
        { 523, 555 },
        { 565, 597 },
        { 608, 640 },
        { 650, 682 },
        { 692, 724 },
        { 734, 766 },
        { 776, 808 },
        { 819, 851 }
    };
    private bool Res4by3;
    private bool NoErrors;
    private Bitmap screen;
    private Bitmap previousscreen;
    private Bitmap Title;
}