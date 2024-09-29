use std::sync::Once;

use windows::{
    core::{w, Result, HSTRING, PCWSTR},
    Win32::{Foundation::{HWND, LPARAM, LRESULT, WPARAM}, System::LibraryLoader::GetModuleHandleW, UI::WindowsAndMessaging::{CreateWindowExW, DefWindowProcW, DispatchMessageW, GetMessageW, LoadCursorW, RegisterClassW, ShowWindow, TranslateMessage, CREATESTRUCTW, CW_USEDEFAULT, IDC_ARROW, MSG, SW_SHOW, WM_NCCREATE, WNDCLASSW, WS_OVERLAPPEDWINDOW}}
};

static REGISTER_WINDOW_CLASS: Once = Once::new();
const WINDOW_CLASS_NAME: PCWSTR = w!("minesweeper-rs.Window");

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
    unsafe { _ = ShowWindow(window, SW_SHOW) };

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
