namespace App24
{
  using System;
  using System.ComponentModel;
  using System.Runtime.CompilerServices;
  using Windows.UI.Core;

  class SurfaceMetrics : INotifyPropertyChanged
  {
    public event PropertyChangedEventHandler PropertyChanged;

    public SurfaceMetrics(CoreDispatcher dispatcher, 
      SurfaceChangeWatcher surfaceWatcher)
    {
      this.dispatcher = dispatcher;
      surfaceWatcher.SurfacesChanged += OnSurfacesChanged;
    }
    void OnSurfacesChanged(object sender, SurfaceChangeEventArgs e)
    {
      switch (e.ChangeType)
      {
        case SurfaceChangeType.Added:
          this.AddedCount += e.ChangedSurfaceInfo.Count;
          this.CurrentCount += e.ChangedSurfaceInfo.Count;
          break;
        case SurfaceChangeType.Removed:
          this.RemovedCount += e.ChangedSurfaceInfo.Count;
          this.CurrentCount -= e.ChangedSurfaceInfo.Count;
          break;
        case SurfaceChangeType.Updated:
          this.UpdatedCount += e.ChangedSurfaceInfo.Count;
          break;
        default:
          break;
      }
    }
    public int CurrentCount
    {
      get
      {
        return (this.currentCount);
      }
      set
      {
        this.currentCount = value;
        this.DispatchPropertyChanged();
      }
    }
    int currentCount;
    public int AddedCount
    {
      get
      {
        return (this.addedCount);
      }
      set
      {
        this.addedCount = value;
        this.DispatchPropertyChanged();
      }
    }
    int addedCount;
    public int RemovedCount
    {
      get
      {
        return (this.removedCount);
      }
      set
      {
        this.removedCount = value;
        this.DispatchPropertyChanged();
      }
    }
    int removedCount;
    public int UpdatedCount
    {
      get
      {
        return (this.updatedCount);
      }
      set
      {
        this.updatedCount = value;
        this.DispatchPropertyChanged();
      }
    }
    int updatedCount;

    async void DispatchPropertyChanged([CallerMemberName] string propertyName = null)
    {
      await this.dispatcher.RunAsync(
        CoreDispatcherPriority.Normal,
        () =>
        {
          this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
      );
    }
    CoreDispatcher dispatcher;
  }
}
