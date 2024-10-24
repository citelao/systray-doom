// Do I know it's weird to expose C bindings to Rust bindings to a game written
// in C? Of course I do!
//
// But I don't like compiling C, and I know you don't either.

#[no_mangle]
pub extern "C" fn rust_function() {
    println!("Hello from Rust!");
}