namespace App24
{
  using System;
  using System.Collections.Generic;
  using Windows.Perception.Spatial.Surfaces;

  class SurfaceChangeEventArgs : EventArgs
  {
    public SurfaceChangeEventArgs(
      IReadOnlyList<SpatialSurfaceInfo> changedSurfaces,
      SurfaceChangeType changeType)
    {
      this.ChangedSurfaceInfo = changedSurfaces;
      this.ChangeType = changeType;
    }
    public SurfaceChangeType ChangeType { get; private set; }
    public IReadOnlyList<SpatialSurfaceInfo> ChangedSurfaceInfo { get; private set; }
  }
}