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

#[repr(C)]
pub struct PublicGame {
}

impl DoomGeneric for PublicGame {
    fn draw_frame(&mut self, screen_buffer: &[u32], xres: usize, yres: usize) {
        todo!()
    }

    fn get_key(&mut self) -> Option<doomgeneric::input::KeyData> {
        todo!()
    }

    fn set_window_title(&mut self, title: &str) {
        todo!()
    }
}

// Create an opaque handle to the game
#[no_mangle]
pub extern "C" fn create_game() -> *mut PublicGame {
    Box::into_raw(Box::new(PublicGame {}))
}

#[no_mangle]
pub extern "C" fn draw_frame(game: *mut PublicGame, screen_buffer: *const u32, xres: usize, yres: usize) {
    let game = unsafe { &mut *game };
    let screen_buffer = unsafe { std::slice::from_raw_parts(screen_buffer, xres * yres) };
    game.draw_frame(screen_buffer, xres, yres);
}

#[no_mangle]
pub extern "C" fn get_key(game: *mut PublicGame) -> Option<doomgeneric::input::KeyData> {
    let game = unsafe { &mut *game };
    game.get_key()
}

#[no_mangle]
pub extern "C" fn set_window_title(game: *mut PublicGame, title: *const u8, len: usize) {
    let title = unsafe { std::str::from_utf8(std::slice::from_raw_parts(title, len)).unwrap() };
    let game = unsafe { &mut *game };
    game.set_window_title(title);
}