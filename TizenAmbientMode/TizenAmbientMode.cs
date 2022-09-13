using System;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace TizenAmbientMode
{
    class Program : NUIApplication
    {
        /// <summary>
        /// Background data handling 
        /// </summary>
        JSONManager ImageFinder;



        /// <summary>
        /// Tizen View Objects and Graphical Objects
        /// </summary>
        public View parentView;
        public TextLabel text1;
        protected override void OnCreate()
        {
            base.OnCreate();
            Initialize();
        }

        void Initialize()
        {
            ImageFinder = new JSONManager();
            parentView = new View();
            parentView.WidthResizePolicy = ResizePolicyType.FillToParent;
            parentView.HeightResizePolicy = ResizePolicyType.FillToParent;

            var defaultWindow = Tizen.NUI.Window.Instance;
            //Tizen.NUI.Window thisWindow = Window.Instance;
            defaultWindow.KeyEvent += OnKeyEvent;
            Timer T = new Timer(1000);
            T.Start();
            T.Tick += IntervalPassed;
            //T.Tick += new EventHandlerWithReturnType<void, Timer.TickEventArgs, bool> (timeIntervalPassed;
            ImageView backGroundImage = new ImageView(DirectoryInfo.Resource + "paperBackground.jpg");
            backGroundImage.HeightResizePolicy = ResizePolicyType.FillToParent;
            backGroundImage.WidthResizePolicy = ResizePolicyType.FillToParent;
            parentView.Add(backGroundImage);


            defaultWindow.GetDefaultLayer().Add(parentView);
            
            text1 = new TextLabel("Hello Tizen NUI World");
            text1.HorizontalAlignment = HorizontalAlignment.Center;
            text1.VerticalAlignment = VerticalAlignment.Center;
            text1.TextColor = Color.Black;
            text1.PointSize = 42.0f;
            text1.HeightResizePolicy = ResizePolicyType.FillToParent;
            text1.WidthResizePolicy = ResizePolicyType.FillToParent;
            defaultWindow.GetDefaultLayer().Add(text1);

            //Animation animation = new Animation(2000);
            //animation.AnimateTo(text, "Orientation", new Rotation(new Radian(new Degree(180.0f)), PositionAxis.X), 0, 500);
            //animation.AnimateTo(text, "Orientation", new Rotation(new Radian(new Degree(0.0f)), PositionAxis.X), 500, 1000);
            //animation.Looping = true;
            //animation.Play();
        }

        public void OnKeyEvent(object sender, Window.KeyEventArgs e)
        {
            if (e.Key.State == Key.StateType.Down && (e.Key.KeyPressedName == "XF86Back" || e.Key.KeyPressedName == "Escape"))
            {
                Exit();
            }
        }

        public bool IntervalPassed(object sender, Timer.TickEventArgs e)
        { 
            text1.Text = new Random().Next(100).ToString()+ (System.DateTime.Now); 
            return true;
        }

        static void Main(string[] args)
        {
            var app = new Program();
            app.Run(args);
        }
    }
}
