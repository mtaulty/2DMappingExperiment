namespace App24
{
  using System;
  using Windows.ApplicationModel;
  using Windows.ApplicationModel.Activation;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  using Windows.UI.Xaml.Navigation;

  sealed partial class App : Application
  {
    public App()
    {
      this.InitializeComponent();
    }
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
      if (this.content == null)
      {
        this.content = new MainPage();
        Window.Current.Content = this.content;
      }
      Window.Current.Activate();
    }
    MainPage content;
  }
}
