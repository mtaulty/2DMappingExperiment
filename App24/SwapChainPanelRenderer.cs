namespace App24
{
  using SharpDX;
  using SharpDX.D3DCompiler;
  using SharpDX.Mathematics.Interop;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Numerics;
  using Windows.Perception.Spatial;
  using Windows.Perception.Spatial.Surfaces;
  using Windows.Storage.Streams;
  using Windows.UI.Xaml;
  using Windows.UI.Xaml.Controls;
  using D3D11 = SharpDX.Direct3D11;
  using DXGI = SharpDX.DXGI;

  internal class SwapChainPanelRenderer
  {
    internal SwapChainPanelRenderer(
      SurfaceChangeWatcher surfaceChangeWatcher,
      SwapChainPanel swapChainPanel,
      SpatialCoordinateSystem coordinateSystem,
      double trianglesPerCubicMetre = 100.0d)
    {
      this.surfaceChangeWatcher = surfaceChangeWatcher;
      this.swapChainPanel = swapChainPanel;
      this.coordinateSystem = coordinateSystem;
      this.trianglesPerCubicMetre = trianglesPerCubicMetre;
      this.bufferMap = new Dictionary<Guid, MeshBufferInfo>();
      this.surfaceChangeWatcher.SurfacesChanged += OnSurfacesChanged;
      this.yawPitchRoll = new Vector3();
    }
    public void Initialise()
    {
      this.swapChainPanel.SizeChanged += this.OnSwapChainPanelSizeChanged;

      this.InitialiseDirect3D();

      this.InitialiseScene();
    }
    void InitialiseDirect3D()
    {
      using (var defaultDevice = new D3D11.Device(
        SharpDX.Direct3D.DriverType.Hardware,
        D3D11.DeviceCreationFlags.Debug))
      {
        this.device = defaultDevice.QueryInterface<D3D11.Device2>();
      }
      this.deviceContext = this.device.ImmediateContext2;

      var rasterizerDesc = new D3D11.RasterizerStateDescription();
      rasterizerDesc.CullMode = D3D11.CullMode.None;
      rasterizerDesc.FillMode = D3D11.FillMode.Wireframe;

      this.deviceContext.Rasterizer.State =
        new D3D11.RasterizerState(this.device, rasterizerDesc);

      var swapChainDescription = new DXGI.SwapChainDescription1()
      {
        AlphaMode = DXGI.AlphaMode.Ignore,
        BufferCount = 2,
        Format = DXGI.Format.R8G8B8A8_UNorm,
        Height = (int)(this.swapChainPanel.RenderSize.Height),
        Width = (int)(this.swapChainPanel.RenderSize.Width),
        SampleDescription = new DXGI.SampleDescription(1, 0),
        Scaling = DXGI.Scaling.Stretch,
        Stereo = false,
        SwapEffect = DXGI.SwapEffect.FlipSequential,
        Usage = DXGI.Usage.RenderTargetOutput
      };

      using (var dxgiDevice3 = this.device.QueryInterface<DXGI.Device3>())
      using (var dxgiFactory3 = dxgiDevice3.Adapter.GetParent<DXGI.Factory3>())
      {
        var swapChain1 = new DXGI.SwapChain1(dxgiFactory3, this.device, ref swapChainDescription);
        this.swapChain = swapChain1.QueryInterface<DXGI.SwapChain2>();
      }

      using (var nativeObject = ComObject.As<DXGI.ISwapChainPanelNative>(this.swapChainPanel))
      {
        nativeObject.SwapChain = this.swapChain;
      }
      this.backBufferTexture =
        this.swapChain.GetBackBuffer<D3D11.Texture2D>(0);

      this.backBufferView =
        new D3D11.RenderTargetView(this.device, this.backBufferTexture);

      deviceContext.Rasterizer.SetViewport(
        0,
        0,
        (int)swapChainPanel.ActualWidth,
        (int)swapChainPanel.ActualHeight);

      Windows.UI.Xaml.Media.CompositionTarget.Rendering += (s, e) =>
      {
        this.RenderScene();
      };

      isDXInitialized = true;
    }
    internal void MoveCameraForwardBack(float amount)
    {
      this.cameraPosition = new Vector3(this.cameraPosition.X,
       this.cameraPosition.Y,
       this.cameraPosition.Z + amount);

      this.RecreateViewConstants();
    }

    internal void RotateCameraLeft(float angle)
    {
      this.yawPitchRoll.Y -= angle;
      this.RecreateViewConstants();
    }
    internal void RotateCameraUp(float angle)
    {
      this.yawPitchRoll.X -= angle;
      this.RecreateViewConstants();
    }

    void RecreateViewConstants()
    {
      var view = Matrix4x4.CreateLookAt(
          this.cameraPosition,
          new Vector3(this.cameraPosition.X, this.cameraPosition.Y, this.cameraPosition.Z + 10),
          new Vector3(0, 1, 0));

      view = Matrix4x4.Multiply(
        view, 
        Matrix4x4.CreateFromYawPitchRoll(this.yawPitchRoll.Y, this.yawPitchRoll.X, this.yawPitchRoll.Z));

      this.viewConstants = new ViewConstantsBuffer()
      {
        view = view.ToRawMatrix(),
        projection = Matrix4x4.CreateOrthographic(2, 1, 0.8f, 3).ToRawMatrix()
      };
    }
    void InitialiseScene()
    {
      var inputElements = new D3D11.InputElement[]
      {
        new D3D11.InputElement(
          "POSITION", 0, DXGI.Format.R16G16B16A16_SNorm, 0)
      };

      D3D11.InputLayout vertexLayout = null;

      using (var vsResult = ShaderBytecode.CompileFromFile(
        VERTEX_SHADER_FILE, "main", "vs_4_0", ShaderFlags.Debug))
      {
        vertexShader = new D3D11.VertexShader(device, vsResult.Bytecode.Data);

        vertexLayout = new D3D11.InputLayout(
          device, vsResult.Bytecode, inputElements);
      }

      using (var psResult = ShaderBytecode.CompileFromFile(
        PIXEL_SHADER_FILE, "main", "ps_4_0", ShaderFlags.Debug))
      {
        pixelShader = new D3D11.PixelShader(device, psResult.Bytecode.Data);
      }
      deviceContext.VertexShader.Set(vertexShader);

      deviceContext.PixelShader.Set(pixelShader);

      deviceContext.InputAssembler.InputLayout = vertexLayout;

      deviceContext.InputAssembler.PrimitiveTopology =
        SharpDX.Direct3D.PrimitiveTopology.TriangleList;

      this.cameraPosition = new Vector3(0, 0.5f, 0);
      this.yawPitchRoll = new Vector3();

      this.RecreateViewConstants();

      this.viewConstantsBuffer = D3D11.Buffer.Create(
        this.device,
        D3D11.BindFlags.ConstantBuffer,
        ref this.viewConstants);

      this.deviceContext.VertexShader.SetConstantBuffer(0, this.viewConstantsBuffer);
    }
    void RenderScene()
    {
      this.deviceContext.OutputMerger.SetRenderTargets(this.backBufferView);

      this.deviceContext.ClearRenderTargetView(
        this.backBufferView, new RawColor4(1, 1, 1, 1));

      this.deviceContext.UpdateSubresource<ViewConstantsBuffer>(
        ref this.viewConstants,
        this.viewConstantsBuffer);

      lock (this.bufferMap)
      {
        foreach (var entry in this.bufferMap)
        {
          this.deviceContext.VertexShader.SetConstantBuffer(
            1, entry.Value.constantsBuffer);

          this.deviceContext.InputAssembler.SetVertexBuffers(
            0, 
            new D3D11.VertexBufferBinding(
              entry.Value.vertexBuffer, (int)entry.Value.vertexStride, 0));

          this.deviceContext.InputAssembler.SetIndexBuffer(
            entry.Value.indexBuffer, (DXGI.Format)entry.Value.indexFormat, 0);

          this.deviceContext.DrawIndexed(entry.Value.indexCount, 0, 0);
        }
      }
      this.swapChain.Present(0, DXGI.PresentFlags.None);
    }
    async void OnSurfacesChanged(object sender, SurfaceChangeEventArgs e)
    {    
      if (this.isDXInitialized)
      {
        switch (e.ChangeType)
        {
          case SurfaceChangeType.Added:
          case SurfaceChangeType.Updated:
            foreach (var surface in e.ChangedSurfaceInfo)
            {
              var mesh = await surface.TryComputeLatestMeshAsync(
                this.trianglesPerCubicMetre, meshOptions);

              if (mesh != null)
              {
                this.AddSurface(mesh);
              }
            }
            break;
          case SurfaceChangeType.Removed:
            lock (this.bufferMap)
            {
              foreach (var surface in e.ChangedSurfaceInfo)
              {
                this.bufferMap.Remove(surface.Id);
              }
            }
            break;
          default:
            break;
        }
      }
    }
    void AddSurface(SpatialSurfaceMesh mesh)
    {
      var bufferInfo = new MeshBufferInfo();
      var byteSize = 0;

      var vertexPtr = mesh.VertexPositions.Data.AsIntPtr(out byteSize);

      bufferInfo.vertexBuffer = new D3D11.Buffer(
        this.device,
        vertexPtr,
        new D3D11.BufferDescription(
          byteSize, D3D11.BindFlags.VertexBuffer, D3D11.ResourceUsage.Default));

      bufferInfo.vertexCount = (int)mesh.VertexPositions.ElementCount;
      bufferInfo.vertexStride = mesh.VertexPositions.Stride;

      var indicesPtr = mesh.TriangleIndices.Data.AsIntPtr(out byteSize);

      bufferInfo.indexBuffer = new D3D11.Buffer(
        this.device,
        indicesPtr,
        new D3D11.BufferDescription(
          byteSize, D3D11.BindFlags.IndexBuffer, D3D11.ResourceUsage.Default));

      bufferInfo.indexFormat = mesh.TriangleIndices.Format;
      bufferInfo.indexCount = (int)mesh.TriangleIndices.ElementCount;    

      var coordTransform = mesh.CoordinateSystem.TryGetTransformTo(
        this.coordinateSystem);

      PerModelConstantsBuffer cb = new PerModelConstantsBuffer();
      var scale = Matrix4x4.CreateScale(mesh.VertexPositionScale);
      var world = Matrix4x4.Transpose(Matrix4x4.Multiply(scale, coordTransform.Value));

      cb.modelToWorld = world.ToRawMatrix();
      
      bufferInfo.constantsBuffer = D3D11.Buffer.Create(
        this.device,
        D3D11.BindFlags.ConstantBuffer,
        ref cb);

      lock (this.bufferMap)
      {
        this.bufferMap[mesh.SurfaceInfo.Id] = bufferInfo;
      }
    }   
    void OnSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (this.isDXInitialized)
      {
        var newSize = new Size2((int)e.NewSize.Width, (int)e.NewSize.Height);

        Utilities.Dispose(ref this.backBufferView);
        Utilities.Dispose(ref this.backBufferTexture);

        swapChain.ResizeBuffers(
          swapChain.Description.BufferCount,
          (int)e.NewSize.Width,
          (int)e.NewSize.Height,
          swapChain.Description1.Format,
          swapChain.Description1.Flags);

        this.backBufferTexture =
          D3D11.Resource.FromSwapChain<D3D11.Texture2D>(this.swapChain, 0);

        this.backBufferView = new D3D11.RenderTargetView(this.device, this.backBufferTexture);

        swapChain.SourceSize = newSize;

        deviceContext.Rasterizer.SetViewport(
          0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height);
      }
    }

    D3D11.Device2 device;
    D3D11.DeviceContext deviceContext;
    DXGI.SwapChain2 swapChain;
    D3D11.Texture2D backBufferTexture;
    D3D11.RenderTargetView backBufferView;
    D3D11.VertexShader vertexShader;
    D3D11.PixelShader pixelShader;
    SwapChainPanel swapChainPanel;
    bool isDXInitialized;

    SurfaceChangeWatcher surfaceChangeWatcher;

    Dictionary<Guid, MeshBufferInfo> bufferMap;
    Vector3 cameraPosition;
    Vector3 yawPitchRoll;
    D3D11.Buffer viewConstantsBuffer;
    ViewConstantsBuffer viewConstants;
    SpatialCoordinateSystem coordinateSystem;
    double trianglesPerCubicMetre;

    static SpatialSurfaceMeshOptions meshOptions =
      new SpatialSurfaceMeshOptions()
      {
        IncludeVertexNormals = false
      };

    static readonly string PIXEL_SHADER_FILE = @"ps.hlsl";
    static readonly string VERTEX_SHADER_FILE = @"vs.hlsl";

  }
}

