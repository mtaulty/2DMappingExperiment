namespace App24
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Windows.Perception.Spatial.Surfaces;
  using SpatialSurfaceEntry = System.Collections.Generic.KeyValuePair<System.Guid, Windows.Perception.Spatial.Surfaces.SpatialSurfaceInfo>;


  class SurfaceChangeWatcher
  {
    public event EventHandler<SurfaceChangeEventArgs> SurfacesChanged;

    class EntryComparer : EqualityComparer<SpatialSurfaceEntry>
    {
      public override bool Equals(
        SpatialSurfaceEntry x,
        SpatialSurfaceEntry y)
      {
        return (x.Key == y.Key);
      }
      public override int GetHashCode(SpatialSurfaceEntry obj)
      {
        return (obj.Key.GetHashCode());
      }
    }

    public SurfaceChangeWatcher(SpatialSurfaceObserver surfaceObserver)
    {
      this.surfacesMap = new Dictionary<Guid, SpatialSurfaceInfo>();

      this.surfaceObserver = surfaceObserver;
    }
    void OnObservedSurfacesChanged(SpatialSurfaceObserver sender, object args)
    {
      this.LoadSurfaces();
    }
    public void Start()
    {
      this.LoadSurfaces();
      this.surfaceObserver.ObservedSurfacesChanged += OnObservedSurfacesChanged;
    }
    public void Stop()
    {
      this.surfaceObserver.ObservedSurfacesChanged -= this.OnObservedSurfacesChanged;

      this.FireSurfaceChangeEvent(
        this.surfacesMap.Values.ToList().AsReadOnly(), 
        SurfaceChangeType.Removed);

      this.surfacesMap = null;     
    }
    void LoadSurfaces()
    {
      var currentSurfaces = this.surfaceObserver.GetObservedSurfaces();

      List<SpatialSurfaceEntry> newSurfaces = null;
      List<SpatialSurfaceEntry> removedSurfaces = null;
      List<SpatialSurfaceEntry> updatedSurfaces = null;

      lock (this.surfacesMap)
      {
        var entryComparer = new EntryComparer();

        newSurfaces = currentSurfaces.Except(this.surfacesMap, entryComparer).ToList();

        removedSurfaces = this.surfacesMap.Except(currentSurfaces, entryComparer).ToList();

        updatedSurfaces = currentSurfaces
          .Join(
            this.surfacesMap,
            o => o.Key,
            i => i.Key,
            (o, i) => o.Value.UpdateTime > i.Value.UpdateTime ? o : default(SpatialSurfaceEntry))
          .Where(entry => entry.Key != default(Guid))
          .ToList();

        foreach (var item in removedSurfaces)
        {
          this.surfacesMap.Remove(item.Key);
        }
        foreach (var item in updatedSurfaces.Concat(newSurfaces))
        {
          this.surfacesMap[item.Key] = item.Value;
        }
      }
      this.FireSurfaceChangeEvent(newSurfaces.Select(s => s.Value).ToList(), SurfaceChangeType.Added);
      this.FireSurfaceChangeEvent(removedSurfaces.Select(s => s.Value).ToList(), SurfaceChangeType.Removed);
      this.FireSurfaceChangeEvent(updatedSurfaces.Select(s => s.Value).ToList(), SurfaceChangeType.Updated);
    }
    void FireSurfaceChangeEvent(
      IReadOnlyList<SpatialSurfaceInfo> surfaces,
      SurfaceChangeType changeType)
    {
      if (surfaces?.Count() > 0)
      {
        this.SurfacesChanged?.Invoke(
          this, new SurfaceChangeEventArgs(surfaces, changeType));
      }
    }
    void OnSurfaceChanged(SpatialSurfaceObserver sender, object args)
    {
      this.LoadSurfaces();
    }
    Dictionary<Guid, SpatialSurfaceInfo> surfacesMap;
    SpatialSurfaceObserver surfaceObserver;
  }
}