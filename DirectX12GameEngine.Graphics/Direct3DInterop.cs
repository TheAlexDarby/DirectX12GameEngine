﻿using Vortice.DXGI;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics.DirectX.Direct3D11;
using SharpGen.Runtime;

namespace DirectX12GameEngine.Graphics
{
    public static class Direct3DInterop
    {
        private static readonly Guid ID3D11Resource = new Guid("DC8E63F3-D12B-4952-B47B-5E45026A862D");

        [ComImport]
        [Guid("F92F19D2-3ADE-45A6-A20C-F6F1EA90554B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        public interface ISwapChainPanelNative : IDisposable
        {
            Result SetSwapChain([In] IntPtr swapChain);
        }

        [ComImport]
        [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IDirect3DDxgiInterfaceAccess : IDisposable
        {
            IntPtr GetInterface([In] ref Guid iid);
        }

        public static IDirect3DDevice CreateDirect3DDevice(IDXGIDevice dxgiDevice)
        {
            Result result = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out IntPtr graphicsDevice);

            if (result.Failure) throw new COMException("Device creation failed.", result.Code);

            IDirect3DDevice d3DInteropDevice = (IDirect3DDevice)Marshal.GetObjectForIUnknown(graphicsDevice);
            Marshal.Release(graphicsDevice);

            return d3DInteropDevice;
        }

        public static IDirect3DSurface CreateDirect3DSurface(IDXGISurface dxgiSurface)
        {
            Result result = CreateDirect3D11SurfaceFromDXGISurface(dxgiSurface.NativePointer, out IntPtr graphicsSurface);

            if (result.Failure) throw new COMException("Surface creation failed.", result.Code);

            IDirect3DSurface d3DSurface = (IDirect3DSurface)Marshal.GetObjectForIUnknown(graphicsSurface);
            Marshal.Release(graphicsSurface);

            return d3DSurface;
        }

        public static IDXGIDevice CreateDXGIDevice(IDirect3DDevice direct3DDevice)
        {
            IDirect3DDxgiInterfaceAccess dxgiDeviceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DDevice;
            IntPtr device = dxgiDeviceInterfaceAccess.GetInterface(ID3D11Resource);

            return new IDXGIDevice(device);
        }

        public static IDXGISurface CreateDXGISurface(IDirect3DSurface direct3DSurface)
        {
            IDirect3DDxgiInterfaceAccess dxgiSurfaceInterfaceAccess = (IDirect3DDxgiInterfaceAccess)direct3DSurface;
            IntPtr surface = dxgiSurfaceInterfaceAccess.GetInterface(ID3D11Resource);

            return new IDXGISurface(surface);
        }

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Result CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11SurfaceFromDXGISurface",
            SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern Result CreateDirect3D11SurfaceFromDXGISurface(IntPtr dxgiSurface, out IntPtr graphicsSurface);
    }
}
