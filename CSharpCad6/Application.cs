using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace OpenCSharpCad;

public static class Application
{
    private static ProgressBar progressBar;
    private static TextWidget statusText;
    private static FlowLayoutWidget progressPanel;
    private static string lastSection = "";
    private static Stopwatch timer;

    public static bool EnableF5Collect { get; set; }

    public static bool EnableNetworkTraffic { get; set; } = true;

    //public GuiWidget CreateSearchButton()
    //{
    //    return new IconButton(StaticData.Instance.LoadIcon("icon_search_24x24.png", 16, 16).SetToColor(TextColor), this)
    //    {
    //        ToolTipText = "Search".Localize(),
    //    };
    //}

    public static RootSystemWindow LoadRootWindow(int width, int height)
    {
        timer = Stopwatch.StartNew();

        //if (false)
        //{
        //    // set the default font
        //    AggContext.DefaultFont = ApplicationController.GetTypeFace(NamedTypeFace.Nunito_Regular);
        //    AggContext.DefaultFontBold = ApplicationController.GetTypeFace(NamedTypeFace.Nunito_Bold);
        //    AggContext.DefaultFontItalic = ApplicationController.GetTypeFace(NamedTypeFace.Nunito_Italic);
        //    AggContext.DefaultFontBoldItalic = ApplicationController.GetTypeFace(NamedTypeFace.Nunito_Bold_Italic);
        //}

        var rootSystemWindow = new RootSystemWindow(width, height);


        ////  var searchButton = theme.CreateSearchButton();
        //  searchButton.Name = "App Search Button";
        //  searchButton.MouseDown += (s, e) =>
        //  {
        //      searchPanelOpenOnMouseDown = searchPanel != null;
        //  };

        ////  searchButton.Click += SearchButton_Click;
        //  rootSystemWindow.AddChild(searchButton);



        //var overlay = new GuiWidget()
        //{
        //    BackgroundColor = Color.White//ppContext.Theme.BackgroundColor,
        //};
        //overlay.AnchorAll();

        //rootSystemWindow.AddChild(overlay);

        //progressPanel = new FlowLayoutWidget(FlowDirection.TopToBottom)
        //{
        //    Position = new Vector2(0, height * .25),
        //    HAnchor = HAnchor.Center | HAnchor.Fit,
        //    VAnchor = VAnchor.Fit,
        //    MinimumSize = new Vector2(400, 100),
        //    Margin = new BorderDouble(0, 0, 0, 200)
        //};
        //overlay.AddChild(progressPanel);

        //progressPanel.AddChild(statusText = new TextWidget("", textColor: Color.Black)
        //{
        //    MinimumSize = new Vector2(200, 30),
        //    HAnchor = HAnchor.Center,
        //    AutoExpandBoundsToText = true
        //});

        //progressPanel.AddChild(progressBar = new ProgressBar()
        //{
        //    FillColor = Color.Pink,
        //    BorderColor = Color.Gray, // theme.BorderColor75,
        //    Height = 11 * GuiWidget.DeviceScale,
        //    Width = 230 * GuiWidget.DeviceScale,
        //    HAnchor = HAnchor.Center,
        //    VAnchor = VAnchor.Absolute
        //});

        //   AppContext.RootSystemWindow = rootSystemWindow;

        // Hook SystemWindow load and spin up MatterControl once we've hit first draw
        rootSystemWindow.Load += (s, e) =>
        {


            LoadMC();

        };


        void LoadMC()
        {
            //  ReportStartupProgress(0.02, "First draw->RunOnIdle");

            // UiThread.RunOnIdle(() =>
            Task.Run(async () =>
            {
                try
                {
                    //     ReportStartupProgress(0.15, "MatterControlApplication.Initialize");

                    //    ApplicationController.LoadTranslationMap();

                    //var mainView = await Initialize(rootSystemWindow, (progress0To1, status) =>
                    //{
                    //////    ReportStartupProgress(0.2 + progress0To1 * 0.7, status);
                    //});

                    //      ReportStartupProgress(0.9, "AddChild->MainView");
                    //   rootSystemWindow.AddChild(mainView, 0);

                    //      ReportStartupProgress(1, "");
                    rootSystemWindow.BackgroundColor = Color.Transparent;
                    //   overlay.Close();
                }
                catch (Exception ex)
                {
                    UiThread.RunOnIdle(() =>
                    {
                        statusText.Visible = false;

                        var errorTextColor = Color.White;

                        progressPanel.Margin = 0;
                        progressPanel.VAnchor = VAnchor.Center | VAnchor.Fit;
                        progressPanel.BackgroundColor = Color.DarkGray;
                        progressPanel.Padding = 20;
                        progressPanel.Border = 1;
                        progressPanel.BorderColor = Color.Red;

                        //  var theme = new ThemeConfig();

                        progressPanel.AddChild(
                            new TextWidget("Startup Failure:", pointSize: 10, textColor: errorTextColor));

                        progressPanel.AddChild(
                            new TextWidget(ex.Message, pointSize: 10, textColor: errorTextColor));

                        //var closeButton = new TextButton("Close", theme)
                        //{
                        //    BackgroundColor = theme.SlightShade,
                        //    HAnchor = HAnchor.Right,
                        //    VAnchor = VAnchor.Absolute
                        //};
                        //closeButton.Click += (s1, e1) =>
                        //{
                        //    rootSystemWindow.Close();
                        //};

                        //spinner.SpinLogo = false;
                        progressBar.Visible = false;

                        // progressPanel.AddChild(closeButton);
                    });
                }

                //   AppContext.IsLoading = false;
            });
        }


        return rootSystemWindow;
    }
}
