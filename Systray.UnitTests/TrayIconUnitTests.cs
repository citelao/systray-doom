using System.ComponentModel;
using Systray.NativeTypes;
using Windows.Win32;
using Windows.Win32.UI.Shell;

namespace Systray.UnitTests;

public class TrayIconUnitTests : IDisposable
{
    public static readonly Guid s_guid = new("0e1e23de-db34-4dac-b0ca-49424eac3bbd");
    public static readonly NoReleaseHwnd s_fakeHwnd = new(0x12345678);

    public void Dispose()
    {
        TrayIcon.Shell_NotifyIconFn = PInvokeCore.Shell_NotifyIcon;
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) => new WindowSubclassHandler(hwnd, wndProc);
        TrayIcon.DefWindowProcFn = PInvokeSystray.DefWindowProc;
    }

    [Fact]
    public void TestBasicPropertyValidation()
    {
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            // We don't actually want to do anything here.
            return true;
        };

        uint callbackMessage = 0x0400 + 1;
        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false,
            callbackMessage: callbackMessage);

        Assert.Equal(s_guid, icon.Guid);
        Assert.Equal(s_fakeHwnd, icon.OwnerHwnd);
        Assert.Equal(callbackMessage, icon.CallbackMessage);
        Assert.Equal("", icon.Tooltip);
        Assert.NotEqual(NoReleaseHicon.Null, icon.Icon);
    }

    [Fact]
    public void TestRegistrationFunctions()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // First, we add the icon.
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_ADD, messages[0].Message);
        Assert.Equal(s_fakeHwnd.ToHwnd(), messages[0].Data.hWnd);
        Assert.Equal(0u, messages[0].Data.uID);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP, messages[0].Data.uFlags);
        Assert.Equal(0u, messages[0].Data.uCallbackMessage);
        Assert.NotEqual(NoReleaseHicon.Null.ToHicon(), messages[0].Data.hIcon);
        Assert.Equal("", messages[0].Data.Tip.ToString());
        Assert.Equal(default, messages[0].Data.dwState);
        Assert.Equal(default, messages[0].Data.dwStateMask);
        Assert.Equal("", messages[0].Data.Info);
        Assert.Equal(PInvokeSystray.NOTIFYICON_VERSION_4, messages[0].Data.Anonymous.uVersion);
        Assert.Equal("", messages[0].Data.InfoTitle);
        Assert.Equal(NOTIFY_ICON_INFOTIP_FLAGS.NIIF_NONE, messages[0].Data.dwInfoFlags);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NoReleaseHicon.Null.ToHicon(), messages[0].Data.hBalloonIcon);

        // Then, we set the version.
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, messages[1].Message);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_GUID | NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP, messages[1].Data.uFlags);
        Assert.Equal(s_guid, messages[1].Data.guidItem);
        Assert.Equal(PInvokeSystray.NOTIFYICON_VERSION_4, messages[1].Data.Anonymous.uVersion);
    }

    [Fact]
    public void TestHandlesCreationFailure()
    {
        var mockHandler = new MockWindowSubclassHandler();
        bool wasSubclassCalled = false;
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            wasSubclassCalled = true;
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            if (dwMessage == NOTIFY_ICON_MESSAGE.NIM_ADD)
            {
                return false;
            }
            return true;
        };

        var ex = Assert.Throws<Win32Exception>(() => new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd));
        Assert.Equal("Failed to add icon to the notification area.", ex.Message);
        Assert.Single(messages);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_ADD, messages[0].Message);

        // The window subclass should not have been created yet.
        Assert.False(wasSubclassCalled);
        Assert.Null(mockHandler.WndProcDelegate);
    }

    [Fact]
    public void TestSetTooltip()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // Change the tooltip.
        messages.Clear();
        icon.Tooltip = "New Tooltip";

        Assert.Single(messages);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_MODIFY, messages[0].Message);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_TIP | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID, messages[0].Data.uFlags);
        Assert.Equal("New Tooltip", messages[0].Data.Tip.ToString());

        Assert.Equal("New Tooltip", icon.Tooltip);
    }

    [Fact]
    public void TestSetIcon()
    {
        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false);

        Assert.Equal(2, messages.Count);

        // Change the icon.
        messages.Clear();
        var newIcon = new NoReleaseHicon(new IntPtr(0x87654321));
        icon.Icon = newIcon;

        Assert.Single(messages);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_MODIFY, messages[0].Message);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_SHOWTIP | NOTIFY_ICON_DATA_FLAGS.NIF_GUID, messages[0].Data.uFlags);
        Assert.Equal(newIcon.ToHicon(), messages[0].Data.hIcon);

        Assert.Equal(newIcon, icon.Icon);
    }

    [Fact]
    public void TestWindowMessageHandling_ContextMenu()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var handlerMessages = new List<(NoReleaseHwnd hwnd, uint msg, NativeTypes.WPARAM wParam, NativeTypes.LPARAM lParam)>();
        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var contextMenuCalled = false;
        var contextMenuPoint = new PhysicalPoint(0, 0);

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        icon.ContextMenu = (hwnd, pt) =>
        {
            contextMenuCalled = true;
            contextMenuPoint = pt;
            return true;
        };

        // Act - simulate a context menu message
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0x12345678), // x=0x5678, y=0x1234
            new NativeTypes.LPARAM((nint)0x007B)); // WM_CONTEXTMENU event type

        // Assert
        Assert.True(contextMenuCalled);
        Assert.Equal(0x5678, contextMenuPoint.X); // x coordinate from wParam
        Assert.Equal(0x1234, contextMenuPoint.Y); // y coordinate from wParam

        // Message handled; DefWindowProc should NOT be called
        Assert.Empty(defWindowProcCalls);
        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_Select()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var selectCalled = false;
        var selectPoint = new PhysicalPoint(0, 0);

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        icon.Select = (hwnd, pt) =>
        {
            selectCalled = true;
            selectPoint = pt;
            return true;
        };

        // Act - simulate a select message
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0x87654321), // x=0x4321, y=0x8765
            new NativeTypes.LPARAM((nint)0x0400)); // NIN_SELECT event type

        // Assert
        Assert.True(selectCalled);
        Assert.Equal(0x4321, selectPoint.X); // x coordinate from wParam
        Assert.Equal(0x8765, selectPoint.Y); // y coordinate from wParam

        // Message handled; DefWindowProc should NOT be called
        Assert.Empty(defWindowProcCalls);
        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_MouseMove()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var mouseMoveCalled = false;
        var mouseMovePoint = new System.Drawing.Point(0, 0);

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        icon.MouseMove = (hwnd, pt) =>
        {
            mouseMoveCalled = true;
            mouseMovePoint = pt;
            return true;
        };

        // Act - simulate a mouse move message
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0xAABBCCDD), // x=0xCCDD, y=0xAABB
            new NativeTypes.LPARAM((nint)0x0200)); // WM_MOUSEMOVE event type

        // Assert
        Assert.True(mouseMoveCalled);
        Assert.Equal(0xCCDD, mouseMovePoint.X); // x coordinate from wParam
        Assert.Equal(0xAABB, mouseMovePoint.Y); // y coordinate from wParam

        // Message handled; DefWindowProc should NOT be called
        Assert.Empty(defWindowProcCalls);
        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_TaskbarCreated()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        var messages = new List<(NOTIFY_ICON_MESSAGE Message, NOTIFYICONDATAW Data)>();
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            messages.Add((dwMessage, lpData));
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Clear the initial messages from icon creation (NIM_ADD and NIM_SETVERSION)
        messages.Clear();

        // Act - simulate a TaskbarCreated message using the actual registered message ID
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            TrayIcon.s_taskbarCreatedWindowMessage, // Use the actual static field
            new NativeTypes.WPARAM(0),
            new NativeTypes.LPARAM(0));

        // Assert - should have deleted and re-created the icon
        // The TaskbarCreated handler deletes first, then calls Create() which does NIM_ADD + NIM_SETVERSION
        Assert.Equal(3, messages.Count);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_DELETE, messages[0].Message);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_ADD, messages[1].Message);
        Assert.Equal(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, messages[2].Message);
        Assert.Equal(s_guid, messages[0].Data.guidItem);
        Assert.Equal(s_guid, messages[1].Data.guidItem);
        
        // TaskbarCreated message still go to DefWindowProc
        Assert.Single(defWindowProcCalls);
        Assert.Equal(s_fakeHwnd.ToHwnd(), defWindowProcCalls[0].hwnd);
        Assert.Equal(TrayIcon.s_taskbarCreatedWindowMessage, defWindowProcCalls[0].msg);
        Assert.NotNull(result);
        Assert.Equal(MOCK_DEFWINDOWPROC_RESULT, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_UnhandledCallbackMessage()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Act - simulate an unhandled callback message (e.g., WM_LBUTTONDOWN)
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0x12345678),
            new NativeTypes.LPARAM((nint)0x0201)); // WM_LBUTTONDOWN unhandled event type

        // Message unhandled; DefWindowProc should be called
        Assert.Single(defWindowProcCalls);
        Assert.Equal(s_fakeHwnd.ToHwnd(), defWindowProcCalls[0].hwnd);
        Assert.Equal(0x0401u, defWindowProcCalls[0].msg);
        Assert.NotNull(result);
        Assert.Equal(MOCK_DEFWINDOWPROC_RESULT, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_CallbackReturnsFalse()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var contextMenuCalled = false;

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Set up callback that returns false (message not handled)
        icon.ContextMenu = (hwnd, pt) =>
        {
            contextMenuCalled = true;
            return false; // Not handled
        };

        // Act - simulate a context menu message
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0x12345678), // x=0x5678, y=0x1234
            new NativeTypes.LPARAM((nint)0x007B)); // WM_CONTEXTMENU event type

        // Assert
        // When callback returns false, HandleCallbackMessage returns null,
        // so DefWindowProc IS called
        Assert.True(contextMenuCalled);
        Assert.Single(defWindowProcCalls);
        Assert.Equal(s_fakeHwnd.ToHwnd(), defWindowProcCalls[0].hwnd);
        Assert.Equal(0x0401u, defWindowProcCalls[0].msg);
        Assert.NotNull(result);
        Assert.Equal(MOCK_DEFWINDOWPROC_RESULT, result.Value.Value);
    }

    [Fact]
    public void TestWindowMessageHandling_NoCallbackHandlers()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Don't set any callback handlers (ContextMenu, Select, MouseMove are all null)

        // Act - simulate a context menu message with no handler
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0401, // callback message
            new NativeTypes.WPARAM(0x12345678),
            new NativeTypes.LPARAM((nint)0x007B)); // WM_CONTEXTMENU event type

        // Assert - with no handler, ContextMenu?.Invoke returns null (false),
        // so DefWindowProc IS called
        Assert.Single(defWindowProcCalls);
        Assert.Equal(s_fakeHwnd.ToHwnd(), defWindowProcCalls[0].hwnd);
        Assert.Equal(0x0401u, defWindowProcCalls[0].msg);
        Assert.NotNull(result);
        Assert.Equal(MOCK_DEFWINDOWPROC_RESULT, result.Value.Value);
    }

    [Fact]
    public void TestWindowSubclassHandlerFactory_CreatesCorrectType()
    {
        // Arrange
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        IWindowSubclassHandler? createdHandler = null;
        NoReleaseHwnd? passedHwnd = null;
        WindowSubclassHandler.WndProcDelegate? passedDelegate = null;

        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            passedHwnd = hwnd;
            passedDelegate = wndProc;
            createdHandler = new MockWindowSubclassHandler();
            return createdHandler;
        };

        // Act
        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Assert
        Assert.NotNull(createdHandler);
        Assert.Equal(s_fakeHwnd, passedHwnd);
        Assert.NotNull(passedDelegate);
    }

    [Fact]
    public void TestWindowSubclassHandlerFactory_NotCalledWhenShouldHandleMessagesIsFalse()
    {
        // Arrange
        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        var factoryCalled = false;
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            factoryCalled = true;
            return new MockWindowSubclassHandler();
        };

        // Act
        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: false); // Don't handle messages

        // Assert
        Assert.False(factoryCalled);
        Assert.Null(icon.CallbackMessage);
    }

    [Fact]
    public void TestWindowMessageHandling_NonCallbackMessage()
    {
        // Arrange
        var defWindowProcCalls = new List<(Windows.Win32.Foundation.HWND hwnd, uint msg, Windows.Win32.Foundation.WPARAM wParam, Windows.Win32.Foundation.LPARAM lParam)>();
        const int MOCK_DEFWINDOWPROC_RESULT = 0x12345678;

        TrayIcon.Shell_NotifyIconFn = (NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData) =>
        {
            return true;
        };

        TrayIcon.DefWindowProcFn = (hwnd, msg, wParam, lParam) =>
        {
            defWindowProcCalls.Add((hwnd, msg, wParam, lParam));
            return new Windows.Win32.Foundation.LRESULT(MOCK_DEFWINDOWPROC_RESULT);
        };

        var mockHandler = new MockWindowSubclassHandler();
        TrayIcon.WindowSubclassHandlerFactoryFn = (hwnd, wndProc) =>
        {
            mockHandler.WndProcDelegate = wndProc;
            return mockHandler;
        };

        var icon = new TrayIcon(
            guid: s_guid,
            ownerHwnd: s_fakeHwnd,
            shouldHandleMessages: true,
            callbackMessage: 0x0401);

        // Act - simulate a non-callback message
        var result = mockHandler.SimulateMessage(
            s_fakeHwnd,
            0x0100, // WM_KEYDOWN - not our callback message
            new NativeTypes.WPARAM(0x42),
            new NativeTypes.LPARAM(0));

        // Assert - this is NOT a callback message (different from CallbackMessage),
        // so HandleMessage returns null, meaning "pass to the original window procedure"
        // We shouldn't call DefWindowProc ourselves.
        Assert.Empty(defWindowProcCalls);
        Assert.Null(result);
    }

    /// <summary>
    /// Mock implementation of IWindowSubclassHandler for testing.
    /// </summary>
    private sealed class MockWindowSubclassHandler : IWindowSubclassHandler
    {
        public WindowSubclassHandler.WndProcDelegate? WndProcDelegate { get; set; }
        public bool IsDisposed = false;

        public MockWindowSubclassHandler()
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        /// <summary>
        /// Simulates a window message being sent to the handler.
        /// </summary>
        public NativeTypes.LRESULT? SimulateMessage(NoReleaseHwnd hwnd, uint msg, NativeTypes.WPARAM wParam, NativeTypes.LPARAM lParam)
        {
            var result = WndProcDelegate?.Invoke(hwnd.ToHwnd(), msg, wParam.ToWin32(), lParam.ToWin32());
            return result.HasValue ? new NativeTypes.LRESULT(result.Value.Value) : null;
        }
    }
}
