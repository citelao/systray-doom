use windows::{
    core::GUID, Win32::{Foundation::HWND, UI::{Shell::{NIF_GUID, NIF_ICON, NIF_MESSAGE, NIF_SHOWTIP, NIF_TIP, NOTIFYICONDATAW, NOTIFYICONDATAW_0, NOTIFYICON_VERSION_4}, WindowsAndMessaging::HICON}}
};

#[derive(Debug, Clone, Default)]
pub struct TrayIconMessage
{
    pub guid: GUID,
    pub hwnd: Option<HWND>,
    pub callback_message: Option<u32>,

    pub tooltip: Option<String>,

    pub show_tooltip: bool,

    pub icon: Option<HICON>,
}

impl TrayIconMessage
{
    pub fn new(guid: GUID) -> Self {
        Self {
            guid,
            ..Default::default()
        }
    }

    pub fn build(&self) -> NOTIFYICONDATAW {
        let mut notify_icon = NOTIFYICONDATAW { ..Default::default() };
        notify_icon.cbSize = std::mem::size_of::<NOTIFYICONDATAW>() as u32;
        notify_icon.guidItem = self.guid;
        notify_icon.uFlags = NIF_GUID;
        notify_icon.Anonymous = NOTIFYICONDATAW_0 {
            uVersion: NOTIFYICON_VERSION_4,
        };

        if let Some(hwnd) = self.hwnd {
            notify_icon.hWnd = hwnd;
        }

        if let Some(callback_message) = self.callback_message {
            notify_icon.uCallbackMessage = callback_message;
            notify_icon.uFlags |= NIF_MESSAGE;
        }

        if self.show_tooltip {
            notify_icon.uFlags |= NIF_SHOWTIP;

            if let Some(tooltip) = &self.tooltip {
                // TODO: I don't know rust
                let vec = tooltip.encode_utf16().take(128).collect::<Vec<u16>>();
                let arr: [u16; 128] = std::array::from_fn(|i| {
                    if i >= vec.len() { return 0 }
                    return vec[i];
                });

                notify_icon.szTip = arr;
                notify_icon.uFlags = NIF_TIP;
            }
        }

        if let Some(icon) = &self.icon {
            notify_icon.hIcon = *icon;
            notify_icon.uFlags |= NIF_ICON;
        }

        return notify_icon;
    }
}

