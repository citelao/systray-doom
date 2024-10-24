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

type DrawFrameCb = extern "C" fn(*const u32, usize, usize);
type GetKeyCb = extern "C" fn() -> Option<doomgeneric::input::KeyData>;
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
        (self.get_key_cb)()
    }

    fn set_window_title(&mut self, title: &str) {
        (self.set_window_title_cb)(title.as_ptr(), title.len());
    }
}

// Create an opaque handle to the game
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

// #[no_mangle]
// pub extern "C" fn draw_frame(game: *mut PublicGame, screen_buffer: *const u32, xres: usize, yres: usize) {
//     let game = unsafe { &mut *game };
//     let screen_buffer = unsafe { std::slice::from_raw_parts(screen_buffer, xres * yres) };
//     game.draw_frame(screen_buffer, xres, yres);
// }

// #[no_mangle]
// pub extern "C" fn get_key(game: *mut PublicGame) -> Option<doomgeneric::input::KeyData> {
//     let game = unsafe { &mut *game };
//     game.get_key()
// }

// #[no_mangle]
// pub extern "C" fn set_window_title(game: *mut PublicGame, title: *const u8, len: usize) {
//     let title = unsafe { std::str::from_utf8(std::slice::from_raw_parts(title, len)).unwrap() };
//     let game = unsafe { &mut *game };
//     game.set_window_title(title);
// }