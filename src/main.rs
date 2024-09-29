use std::{collections::VecDeque, sync::{Arc, Once}};

use doomgeneric::{game::DoomGeneric, input::KeyData};
use windows::{
    core::{w, Result, GUID, HSTRING, PCWSTR},
    Win32::{Foundation::{HWND, LPARAM, LRESULT, WPARAM}, Graphics::Gdi::CreateBitmap, System::LibraryLoader::GetModuleHandleW, UI::{Shell::{Shell_NotifyIconW, NIF_GUID, NIF_ICON, NIF_SHOWTIP, NIF_TIP, NIM_ADD, NIM_MODIFY, NIM_SETVERSION, NOTIFYICONDATAW, NOTIFYICONDATAW_0, NOTIFYICON_VERSION_4}, WindowsAndMessaging::{CreateIcon, CreateIconFromResourceEx, CreateWindowExW, DefWindowProcW, DispatchMessageW, GetMessageW, LoadCursorW, LoadIconW, RegisterClassW, TranslateMessage, CW_USEDEFAULT, IDC_ARROW, IDI_ASTERISK, MSG, WM_NCCREATE, WNDCLASSW, WS_OVERLAPPEDWINDOW}}}
};

static REGISTER_WINDOW_CLASS: Once = Once::new();
const WINDOW_CLASS_NAME: PCWSTR = w!("minesweeper-rs.Window");

// 3889a1fb-1354-42a2-a0d6-cb6493d2e91e
const SYSTRAY_GUID: GUID = GUID::from_values(0x3889a1fb, 0x1354, 0x42a2, [0xa0, 0xd6, 0xcb, 0x64, 0x93, 0xd2, 0xe9, 0x1e]);

struct Game {
    input_queue: VecDeque<KeyData>
}

impl DoomGeneric for Game {
    fn draw_frame(&mut self, screen_buffer: &[u32], xres: usize, yres: usize) {
        // This one draws the whole screen:
        //
        // let mut screen_buffer_rgba: Vec<u8> = Vec::with_capacity(xres * yres * 4);
        // for argb in screen_buffer {
        //     screen_buffer_rgba.push(((argb >> 16) & 0xFF) as u8);
        //     screen_buffer_rgba.push(((argb >> 8) & 0xFF) as u8);
        //     screen_buffer_rgba.push(((argb >> 0) & 0xFF) as u8);
        //     // Alpha seems to be opacity. Inverting it.
        //     screen_buffer_rgba.push(255 - ((argb >> 24) & 0xFF) as u8);
        // }

        // Use the center-top of the screen.
        // It's 640x400 usually.
        // assert_eq!(640, xres);
        // assert_eq!(400, yres);
        const DESIRED_WIDTH: usize = 320;
        const DESIRED_HEIGHT: usize = 320;
        let x_range = ((xres - DESIRED_WIDTH) / 2, (xres - DESIRED_WIDTH) / 2 + DESIRED_WIDTH);
        // let y_range = ((yres - DESIRED_HEIGHT) / 2, (yres - DESIRED_HEIGHT) / 2 + DESIRED_HEIGHT);
        let y_range = (0, DESIRED_HEIGHT);
        let mut screen_buffer_rgba: Vec<u8> = Vec::with_capacity(DESIRED_HEIGHT * DESIRED_WIDTH * 4);
        for i in 0..screen_buffer.len() {
            let current_posn = (i % xres, i / xres);
            if current_posn.0 < x_range.0 || current_posn.0 >= x_range.1
            {
                continue
            }
            if current_posn.1 < y_range.0 || current_posn.1 >= y_range.1
            {
                continue
            }
            let argb = &screen_buffer[i];
            screen_buffer_rgba.push(((argb >> 16) & 0xFF) as u8);
            screen_buffer_rgba.push(((argb >> 8) & 0xFF) as u8);
            screen_buffer_rgba.push(((argb >> 0) & 0xFF) as u8);
            // Alpha seems to be opacity. Inverting it.
            screen_buffer_rgba.push(255 - ((argb >> 24) & 0xFF) as u8);
        }

        let icon = unsafe { CreateIcon(None,
            DESIRED_WIDTH as i32,
            DESIRED_HEIGHT as i32,
            4,
            8,
            screen_buffer_rgba.as_ptr(),
            screen_buffer_rgba.as_ptr()).expect("Could not create icon") };
        let icon_info = NOTIFYICONDATAW {
            cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
            guidItem: SYSTRAY_GUID,
            uFlags: NIF_GUID | NIF_ICON | NIF_SHOWTIP,
            hIcon: icon,
            ..Default::default()
        };
        unsafe {
            assert_ne!(Shell_NotifyIconW(NIM_MODIFY, &icon_info), false);
        }
    }

    fn get_key(&mut self) -> Option<doomgeneric::input::KeyData> {
        return self.input_queue.pop_front()
    }

    fn set_window_title(&mut self, title: &str) {
        // TODO: I don't know rust
        let vec = title.encode_utf16().take(128).collect::<Vec<u16>>();
        let arr: [u16; 128] = std::array::from_fn(|i| {
            if i >= vec.len() { return 0 }
            return vec[i];
        });
        let icon_info = NOTIFYICONDATAW {
            cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
            guidItem: SYSTRAY_GUID,
            uFlags: NIF_GUID | NIF_TIP | NIF_SHOWTIP,
            szTip: arr,
            ..Default::default()
        };
        unsafe {
            assert_ne!(Shell_NotifyIconW(NIM_MODIFY, &icon_info), false);
        }
    }
}

unsafe extern "system" fn wnd_proc(
    window: HWND,
    message: u32,
    wparam: WPARAM,
    lparam: LPARAM,
) -> LRESULT {
    if message == WM_NCCREATE {
        // let cs = lparam.0 as *const CREATESTRUCTW;
        // let this = (*cs).lpCreateParams as *mut Self;
        // (*this).handle = window;

        // SetWindowLongPtrW(window, GWLP_USERDATA, this as _);
    } else {
        // let this = GetWindowLongPtrW(window, GWLP_USERDATA) as *mut Self;

        // if let Some(this) = this.as_mut() {
        //     return this.message_handler(message, wparam, lparam);
        // }
    }
    DefWindowProcW(window, message, wparam, lparam)
}

fn run() -> Result<()> {
    println!("Hello, world!");

    println!("Registering window class...");
    let instance = unsafe { GetModuleHandleW(None)? };
        REGISTER_WINDOW_CLASS.call_once(|| {
            let class = WNDCLASSW {
                hCursor: unsafe { LoadCursorW(None, IDC_ARROW).ok().unwrap() },
                hInstance: instance.into(),
                lpszClassName: WINDOW_CLASS_NAME,
                lpfnWndProc: Some(wnd_proc),
                ..Default::default()
            };
            assert_ne!(unsafe { RegisterClassW(&class) }, 0);
        });

    let window = unsafe {
        CreateWindowExW(
            windows::Win32::UI::WindowsAndMessaging::WINDOW_EX_STYLE(0),
            WINDOW_CLASS_NAME,
            &HSTRING::from("Test window"),
            WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            None,
            None,
            instance,
            None, // Some(result.as_mut() as *mut _ as _),
        )
        // .ok()?
    };
    // unsafe { _ = ShowWindow(window, SW_SHOW) };

    // Systray!
    let icon = unsafe {
        LoadIconW(None, IDI_ASTERISK)?
    };
    let icon_info = NOTIFYICONDATAW {
        cbSize: std::mem::size_of::<NOTIFYICONDATAW>() as u32,
        guidItem: SYSTRAY_GUID,
        uFlags: NIF_GUID | NIF_ICON,
        hIcon: icon as _,
        hWnd: window,
        // szTip: "test".encode_utf16().collect(),
        // szTip: Default::default(),
        // szInfo: None,
        // dwState: 0,
        // dwStateMask: 0,
        // szInfoTitle: None,
        // dwInfoFlags: 0,
        Anonymous: NOTIFYICONDATAW_0 {
            uVersion: NOTIFYICON_VERSION_4,
        },
        ..Default::default()
    };
    unsafe {
        assert_ne!(Shell_NotifyIconW(NIM_ADD, &icon_info), false);
        assert_ne!(Shell_NotifyIconW(NIM_SETVERSION, &icon_info), false);
    }

    // Start a new thread...
    println!("Starting a thread to play Doom...");
    let game_state = Game {
        input_queue: VecDeque::new(),
    };
    std::thread::spawn(|| {
        doomgeneric::game::init(game_state);
    });

    println!("Starting message loop...");
    let mut message = MSG::default();
    unsafe {
        while GetMessageW(&mut message, None, 0, 0).into() {
            _ = TranslateMessage(&message);
            DispatchMessageW(&message);
        }
    }
    Ok(())
}

fn main() {
    let result = run();

    // We do this for nicer HRESULT printing when errors occur.
    if let Err(error) = result {
        error.code().unwrap();
    }
}
