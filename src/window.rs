use std::sync::Once;

use windows::{
    core::{w, Result, HSTRING, PCWSTR},
    Win32::{
        Foundation::{HWND, LPARAM, LRESULT, RECT, WPARAM},
        System::LibraryLoader::GetModuleHandleW,
        UI::WindowsAndMessaging::{
            AdjustWindowRectEx, CreateWindowExW, DefWindowProcW, GetWindowLongPtrW, LoadCursorW,
            RegisterClassW, SetWindowLongPtrW, ShowWindow, CREATESTRUCTW, CW_USEDEFAULT,
            GWLP_USERDATA, IDC_ARROW, SW_SHOW, WM_NCCREATE, WNDCLASSW, WS_EX_NOREDIRECTIONBITMAP,
            WS_OVERLAPPEDWINDOW,
        },
    },
};

static REGISTER_WINDOW_CLASS: Once = Once::new();
const WINDOW_CLASS_NAME: PCWSTR = w!("systray-doom.Window");

pub trait WndProc {
    fn wnd_proc_message_handler(
        &mut self,
        window: HWND,
        message: u32,
        wparam: WPARAM,
        lparam: LPARAM,
    ) -> Option<LRESULT>;
}

// Adapted from https://github.com/robmikh/minesweeper-rs/blob/135ab04eb5c02a3fb7d50265cbbacad372c73ed1/src/window.rs
pub struct Window {
    pub handle: HWND,

    message_handler: Box<dyn WndProc>,
}

impl Window {
    pub fn new(
        title: &str,
        width: u32,
        height: u32,
        message_handler: impl WndProc + 'static,
    ) -> Result<Box<Self>> {
        let instance = unsafe { GetModuleHandleW(None)? };

        // TODO: generalize this to support multiple window classes.
        REGISTER_WINDOW_CLASS.call_once(|| {
            let class = WNDCLASSW {
                hCursor: unsafe { LoadCursorW(None, IDC_ARROW).ok().unwrap() },
                hInstance: instance.into(),
                lpszClassName: WINDOW_CLASS_NAME,
                lpfnWndProc: Some(Self::wnd_proc),
                ..Default::default()
            };
            assert_ne!(unsafe { RegisterClassW(&class) }, 0);
        });

        let window_ex_style = WS_EX_NOREDIRECTIONBITMAP;
        let window_style = WS_OVERLAPPEDWINDOW;

        let (adjusted_width, adjusted_height) = {
            let mut rect = RECT {
                left: 0,
                top: 0,
                right: width as i32,
                bottom: height as i32,
            };
            unsafe {
                AdjustWindowRectEx(&mut rect, window_style, false, window_ex_style)?;
            }
            (rect.right - rect.left, rect.bottom - rect.top)
        };

        let mut result = Box::new(Self {
            handle: HWND(0),
            message_handler: Box::new(message_handler),
        });

        let window = unsafe {
            CreateWindowExW(
                window_ex_style,
                WINDOW_CLASS_NAME,
                &HSTRING::from(title),
                window_style,
                CW_USEDEFAULT,
                CW_USEDEFAULT,
                adjusted_width,
                adjusted_height,
                None,
                None,
                instance,
                Some(result.as_mut() as *mut _ as _),
            )
            // .ok()?
        };
        unsafe { _ = ShowWindow(window, SW_SHOW) };

        Ok(result)
    }

    unsafe extern "system" fn wnd_proc(
        window: HWND,
        message: u32,
        wparam: WPARAM,
        lparam: LPARAM,
    ) -> LRESULT {
        if message == WM_NCCREATE {
            let cs = lparam.0 as *const CREATESTRUCTW;
            let this = (*cs).lpCreateParams as *mut Self;
            (*this).handle = window;

            SetWindowLongPtrW(window, GWLP_USERDATA, this as _);
        } else {
            let this = GetWindowLongPtrW(window, GWLP_USERDATA) as *mut Self;

            if let Some(this) = this.as_mut() {
                if let Some(result) = (*(*this).message_handler)
                    .wnd_proc_message_handler(window, message, wparam, lparam)
                {
                    return result;
                }
            }
        }
        DefWindowProcW(window, message, wparam, lparam)
    }

    // https://github.com/robmikh/minesweeper-rs/blob/135ab04eb5c02a3fb7d50265cbbacad372c73ed1/src/window.rs
    pub fn get_mouse_position(lparam: LPARAM) -> (isize, isize) {
        let x = lparam.0 & 0xffff;
        let y = (lparam.0 >> 16) & 0xffff;
        (x, y)
    }
}
