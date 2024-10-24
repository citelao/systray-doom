// Do I know it's weird to expose C bindings to Rust bindings to a game written
// in C? Of course I do!
//
// But I don't like compiling C, and I know you don't either.

use doomgeneric::game::DoomGeneric;

#[no_mangle]
pub extern "C" fn rust_function() -> i32 {
    println!("Hello from Rust!");
    42
}

// Simple shim to allow calling MessageBoxW
#[link(name = "user32")]
extern "system" {
    fn MessageBoxW(
        hWnd: *const u8,
        lpText: *const u8,
        lpCaption: *const u8,
        uType: u32,
    ) -> i32;
}

#[repr(C)]
#[derive(Debug, Clone, Copy)]
pub struct CKeyData {
    pub pressed: bool,
    pub key: u8,
}

type DrawFrameCb = extern "C" fn(*const u32, usize, usize);
type GetKeyCb = extern "C" fn() -> *mut CKeyData;
type SetWindowTitleCb = extern "C" fn(*const u8, usize);

#[repr(C)]
pub struct PublicGame {
    draw_frame_cb: DrawFrameCb,
    get_key_cb: GetKeyCb,
    set_window_title_cb: SetWindowTitleCb,
}

impl DoomGeneric for PublicGame {
    fn draw_frame(&mut self, screen_buffer: &[u32], xres: usize, yres: usize) {
        (self.draw_frame_cb)(screen_buffer.as_ptr(), xres, yres);
    }

    fn get_key(&mut self) -> Option<doomgeneric::input::KeyData> {
        let key = (self.get_key_cb)();
        if key.is_null() {
            return None;
        }
        let key = unsafe { *key };
        Some(doomgeneric::input::KeyData {
            pressed: key.pressed,
            key: key.key,
        })
    }

    fn set_window_title(&mut self, title: &str) {
        (self.set_window_title_cb)(title.as_ptr(), title.len());
    }
}

// Start a game!
#[no_mangle]
pub extern "C" fn create_game(
    draw_frame_cb: DrawFrameCb,
    get_key_cb: GetKeyCb,
    set_window_title_cb: SetWindowTitleCb,
) -> *mut PublicGame {
    Box::into_raw(Box::new(PublicGame {
        draw_frame_cb,
        get_key_cb,
        set_window_title_cb,
    }))
}