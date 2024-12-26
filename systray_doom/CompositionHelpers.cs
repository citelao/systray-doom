using Windows.Win32;
using Windows.Win32.Graphics.Direct2D;
using Windows.Win32.Graphics.Direct3D;
using Windows.Win32.Graphics.Direct3D11;
using Windows.Win32.Graphics.Dxgi;
using Systray;
using System.Runtime.InteropServices;

namespace systray_doom;

public static class CompositionHelpers
{
    public struct CreateDirect2DDeviceResult
    {
        public ID3D11Device Device;
        public ID3D11DeviceContext Context;
        public D3D_FEATURE_LEVEL FeatureLevel;
    }

    // https://learn.microsoft.com/en-us/windows/uwp/composition/composition-native-interop
    // TODO: watch for device loss like the example.
    public static CreateDirect2DDeviceResult CreateDirect3DDevice()
    {
        ID3D11Device device;
        ID3D11DeviceContext context;
        D3D_FEATURE_LEVEL featureLevel;
        unsafe {
            PInvokeHelpers.THROW_IF_FAILED(PInvoke.D3D11CreateDevice(
                null,
                D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
                Systray.NoReleaseSafeHandle.Null,
                D3D11_CREATE_DEVICE_FLAG.D3D11_CREATE_DEVICE_BGRA_SUPPORT,
                [
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                    D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1,
                ],
                (uint)PInvoke.D3D11_SDK_VERSION,
                out device,
                &featureLevel,
                out context
            ));
        }

        return new CreateDirect2DDeviceResult
        {
            Device = device,
            Context = context,
            FeatureLevel = featureLevel,
        };
    }

    public static ID2D1Factory1 CreateD2DFactory()
    {
        object obj;
        unsafe {
            D2D1_FACTORY_OPTIONS options = new()
            {
                debugLevel = D2D1_DEBUG_LEVEL.D2D1_DEBUG_LEVEL_NONE,
            };
            PInvoke.D2D1CreateFactory(
                D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED,
                // via https://github.com/microsoft/CsWin32/issues/449
                typeof(Windows.Win32.Graphics.Direct2D.ID2D1Factory1).GUID,
                options,
                out obj
            );
        }
        return (ID2D1Factory1)obj;
    }

    public static ID2D1Device GetD2DDevice(ID3D11Device device, ID2D1Factory1? factory = null)
    {
        var d2dFactory = factory ?? CreateD2DFactory();

        // Obtain the underlying DXGI device of the Direct3D device.
        var dxgiDevice = (IDXGIDevice)device;

        d2dFactory.CreateDevice(dxgiDevice, out var d2dDevice);
        return d2dDevice;
    }
}