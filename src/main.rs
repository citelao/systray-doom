use std::{
    collections::VecDeque,
    sync::{Arc, Mutex, Once},
};

use doomgeneric::{
    game::DoomGeneric,
    input::{keys, KeyData},
};
use window::{Window, WndProc};
use windows::{
    core::{w, Result, GUID, PCWSTR},
    Win32::{
        Foundation::{HWND, LPARAM, LRESULT, WPARAM},
        UI::{
            Input::KeyboardAndMouse::{
                VIRTUAL_KEY, VK_DOWN, VK_LCONTROL, VK_LEFT, VK_MENU, VK_OEM_COMMA,
                VK_OEM_PERIOD, VK_RCONTROL, VK_RIGHT, VK_SHIFT, VK_SPACE, VK_UP,
            },
            Shell::{
                Shell_NotifyIconW, NIM_ADD, NIM_MODIFY,
                NIM_SETVERSION,
            },
            WindowsAndMessaging::{
                CreateIcon, DestroyIcon, DispatchMessageW,
                GetMessageW, LoadIconW, PostQuitMessage, TranslateMessage, HICON, IDI_ASTERISK, MSG, WM_DESTROY, WM_KEYDOWN, WM_KEYUP,
            },
        },
    },
};

mod tray_icon_message;
mod window;

// 3889a1fb-1354-42a2-a0d6-cb6493d2e91e
const SYSTRAY_GUID: GUID = GUID::from_values(
    0x3889a1fb,
    0x1354,
    0x42a2,
    [0xa0, 0xd6, 0xcb, 0x64, 0x93, 0xd2, 0xe9, 0x1e],
);

struct Game {
    previous_frame: Option<HICON>,
    input_queue: Arc<Mutex<VecDeque<KeyData>>>,
}

struct GameWindow {
    input_queue: Arc<Mutex<VecDeque<KeyData>>>,
}

impl WndProc for GameWindow {
    fn wnd_proc_message_handler(
        &mut self,
        _window: HWND,
        message: u32,
        wparam: WPARAM,
        _lparam: LPARAM,
    ) -> Option<LRESULT> {
        match message {
            WM_KEYDOWN => {
                if let Some(key) = vkey_to_doom_key(wparam) {
                    let key_data = KeyData {
                        key,
                        pressed: true,
                    };
                    self.input_queue.lock().unwrap().push_back(key_data);
                    // println!("Key: {}", key);
                }
            }
            WM_KEYUP => {
                if let Some(key) = vkey_to_doom_key(wparam) {
                    let key_data = KeyData {
                        key,
                        pressed: false,
                    };
                    self.input_queue.lock().unwrap().push_back(key_data);
                }
            }
            WM_DESTROY => {
                unsafe { PostQuitMessage(0) };
                return Some(LRESULT(0));
            }
            _ => {}
        }
        None
    }
}

impl DoomGeneric for Game {
    fn draw_frame(&mut self, screen_buffer: &[u32], xres: usize, yres: usize) {
        // This one draws the whole screen.
        // Taken from piston-doom's impl.
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
        assert!(xres > DESIRED_WIDTH);
        assert!(yres > DESIRED_HEIGHT);
        let x_range = (
            (xres - DESIRED_WIDTH) / 2,
            (xres - DESIRED_WIDTH) / 2 + DESIRED_WIDTH,
        );
        // let y_range = ((yres - DESIRED_HEIGHT) / 2, (yres - DESIRED_HEIGHT) / 2 + DESIRED_HEIGHT);
        let y_range = (0, DESIRED_HEIGHT);
        let mut screen_buffer_rgba: Vec<u8> =
            Vec::with_capacity(DESIRED_HEIGHT * DESIRED_WIDTH * 4);
        for i in 0..screen_buffer.len() {
            let current_posn = (i % xres, i / xres);
            if current_posn.0 < x_range.0 || current_posn.0 >= x_range.1 {
                continue;
            }
            if current_posn.1 < y_range.0 || current_posn.1 >= y_range.1 {
                continue;
            }
            let argb = &screen_buffer[i];
            screen_buffer_rgba.push(((argb >> 16) & 0xFF) as u8);
            screen_buffer_rgba.push(((argb >> 8) & 0xFF) as u8);
            screen_buffer_rgba.push(((argb >> 0) & 0xFF) as u8);
            // Alpha seems to be opacity. Inverting it.
            screen_buffer_rgba.push(255 - ((argb >> 24) & 0xFF) as u8);
        }

        let icon = unsafe {
            CreateIcon(
                None,
                DESIRED_WIDTH as i32,
                DESIRED_HEIGHT as i32,
                4,
                8,
                screen_buffer_rgba.as_ptr(),
                screen_buffer_rgba.as_ptr(),
            )
            .expect("Could not create icon")
        };
        let icon_info = tray_icon_message::TrayIconMessage {
            guid: SYSTRAY_GUID,
            icon: Some(icon),
            ..Default::default()
        }
        .build();
        unsafe {
            assert_ne!(Shell_NotifyIconW(NIM_MODIFY, &icon_info), false);
        }
        if let Some(previous_frame) = self.previous_frame {
            unsafe { DestroyIcon(previous_frame).expect("delete previous frame") };
        }
        self.previous_frame = Some(icon);
    }

    fn get_key(&mut self) -> Option<doomgeneric::input::KeyData> {
        return self.input_queue.lock().unwrap().pop_front();
    }

    fn set_window_title(&mut self, title: &str) {
        let icon_info = tray_icon_message::TrayIconMessage {
            guid: SYSTRAY_GUID,
            tooltip: Some(title.to_string()),
            ..Default::default()
        }
        .build();
        unsafe {
            assert_ne!(Shell_NotifyIconW(NIM_MODIFY, &icon_info), false);
        }
    }
}

fn vkey_to_doom_key(button: WPARAM) -> Option<u8> {
    let key = VIRTUAL_KEY(button.0 as u16);
    match key {
        // Map keyboard keys from m_controller.c
        VK_RIGHT => Some(*keys::KEY_RIGHT),
        VK_LEFT => Some(*keys::KEY_LEFT),
        VK_UP => Some(*keys::KEY_UP),
        VK_DOWN => Some(*keys::KEY_DOWN),
        VK_OEM_COMMA => Some(*keys::KEY_STRAFELEFT),
        VK_OEM_PERIOD => Some(*keys::KEY_STRAFERIGHT),
        VK_LCONTROL | VK_RCONTROL => Some(*keys::KEY_FIRE),
        VK_SPACE => Some(*keys::KEY_USE),
        VK_MENU => Some(*keys::KEY_STRAFE),
        VK_SHIFT => Some(*keys::KEY_SPEED),
        // Let doom deal with the rest
        _ => keys::from_char(button.0 as u8 as char),
    }
}

fn run() -> Result<()> {
    println!("Hello, world!");

    // println!("Registering window class...");
    // let instance = unsafe { GetModuleHandleW(None)? };
    //     REGISTER_WINDOW_CLASS.call_once(|| {
    //         let class = WNDCLASSW {
    //             hCursor: unsafe { LoadCursorW(None, IDC_ARROW).ok().unwrap() },
    //             hInstance: instance.into(),
    //             lpszClassName: WINDOW_CLASS_NAME,
    //             lpfnWndProc: Some(wnd_proc),
    //             ..Default::default()
    //         };
    //         assert_ne!(unsafe { RegisterClassW(&class) }, 0);
    //     });

    let shared_input_queue = Arc::new(Mutex::new(VecDeque::new()));
    let game_state = Game {
        previous_frame: None,
        input_queue: shared_input_queue.clone(),
    };
    let window = Window::new(
        "test",
        640,
        400,
        GameWindow {
            input_queue: shared_input_queue,
        },
    )?;

    // Systray!
    let icon = unsafe { LoadIconW(None, IDI_ASTERISK)? };
    let icon_info = tray_icon_message::TrayIconMessage {
        guid: SYSTRAY_GUID,
        hwnd: Some(window.handle),
        callback_message: Some(0),
        tooltip: Some("Starting Doom...".to_string()),
        icon: Some(icon),
        ..Default::default()
    }
    .build();
    unsafe {
        assert_ne!(Shell_NotifyIconW(NIM_ADD, &icon_info), false);
        assert_ne!(Shell_NotifyIconW(NIM_SETVERSION, &icon_info), false);
    }

    // Start a new thread...
    println!("Starting a thread to play Doom...");
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
