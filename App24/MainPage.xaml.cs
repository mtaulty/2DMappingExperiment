namespace App24
{
  using System;
  using System.ComponentModel;
  using System.Numerics;
  using Windows.Perception.Spatial;
  using Windows.Perception.Spatial.Surfaces;
  using Windows.UI.Popups;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  using System.Runtime.CompilerServices;
  using Windows.Graphics.Holographic;

  public sealed partial class MainPage : Page, INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    internal SurfaceMetrics SurfaceMetrics
    {
      get
      {
        return (this.surfaceMetrics);
      }
      set
      {
        this.surfaceMetrics = value;
        this.FirePropertyChanged();
      }
    }
    public MainPage()
    {      
      this.InitializeComponent();
      this.Loaded += OnLoaded;
    }
    async void OnLoaded(object sender, RoutedEventArgs e)
    {
      bool tryInitialisation = true;

      if (Windows.Foundation.Metadata.ApiInformation.IsApiContractPresent(
          "Windows.Foundation.UniversalApiContract", 4, 0))
      {
        tryInitialisation = SpatialSurfaceObserver.IsSupported();
      }

      if (tryInitialisation)
      {
        var access = await SpatialSurfaceObserver.RequestAccessAsync();

        if (access == SpatialPerceptionAccessStatus.Allowed)
        {
          this.InitialiseSurfaceObservation();
        }
        else
        {
          tryInitialisation = false;
        }
      }
      if (!tryInitialisation)
      {
        var dialog = new MessageDialog(
          "Spatial observation is either not supported or not allowed", "Not Available");

        await dialog.ShowAsync();
      }
    }
    void InitialiseSurfaceObservation()
    {
      // We want the default locator.
      this.locator = SpatialLocator.GetDefault();

      // We try to make a frame of reference that is fixed at the current position (i.e. not
      // moving with the user).
      var frameOfReference = this.locator.CreateStationaryFrameOfReferenceAtCurrentLocation();

      this.baseCoordinateSystem = frameOfReference.CoordinateSystem;

      // Make a box which is centred at the origin (the user's startup location)
      // and is hopefully oriented to the Z axis and a certain width/height.
      var boundingVolume = SpatialBoundingVolume.FromSphere(
        this.baseCoordinateSystem,
        new SpatialBoundingSphere()
        {
          Center = new Vector3(0, 0, 0),
          Radius = SPHERE_RADIUS
        }
      );
      this.surfaceObserver = new SpatialSurfaceObserver();
      this.surfaceObserver.SetBoundingVolume(boundingVolume);

      this.surfaceWatcher = new SurfaceChangeWatcher(this.surfaceObserver);
      this.SurfaceMetrics = new SurfaceMetrics(this.Dispatcher, this.surfaceWatcher);

      this.swapChainRenderer = new SwapChainPanelRenderer(
          this.surfaceWatcher,
          this.swapChainPanel,
          this.baseCoordinateSystem);

      this.swapChainRenderer.Initialise();

      this.surfaceWatcher.Start();
    }
    void FirePropertyChanged([CallerMemberName] string memberName = null)
    {
      this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
    void OnForward(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.MoveCameraForwardBack(0.5f);
    }
    void OnBack(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.MoveCameraForwardBack(-0.5f);
    }
    void OnLeft(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.RotateCameraLeft(ANGLE_DELTA);
    }
    void OnRight(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.RotateCameraLeft(0 - ANGLE_DELTA);
    }
    void OnUp(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.RotateCameraUp(0 - ANGLE_DELTA);
    }
    void OnDown(object sender, RoutedEventArgs e)
    {
      this.swapChainRenderer.RotateCameraUp(ANGLE_DELTA);
    }
    SpatialCoordinateSystem baseCoordinateSystem;
    SwapChainPanelRenderer swapChainRenderer;
    SurfaceMetrics surfaceMetrics;
    SurfaceChangeWatcher surfaceWatcher;
    SpatialLocator locator;
    SpatialSurfaceObserver surfaceObserver;

    static readonly float SPHERE_RADIUS = 5.0f;
    static readonly float ANGLE_DELTA = (float)Math.PI / 6.0f;
  }
}