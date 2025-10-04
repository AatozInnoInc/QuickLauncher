#![cfg_attr(all(not(debug_assertions), target_os = "windows"), windows_subsystem = "windows")]

use tauri::{Manager};

fn main() {
    tauri::Builder::default()
        .setup(|app| {
            // Optionally hide the window on startup; toggle via global shortcut.
            if let Some(window) = app.get_window("main") {
                let _ = window.hide();
            }

            // Register a global shortcut: CmdOrCtrl+Space to toggle window visibility
            let app_handle = app.handle();
            let mut gsm = app.global_shortcut_manager();
            gsm.register("CmdOrCtrl+Space", move || {
                if let Some(window) = app_handle.get_window("main") {
                    let is_visible = window.is_visible().unwrap_or(false);
                    if is_visible {
                        let _ = window.hide();
                    } else {
                        let _ = window.show();
                        let _ = window.set_focus();
                    }
                }
            })?;

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}


