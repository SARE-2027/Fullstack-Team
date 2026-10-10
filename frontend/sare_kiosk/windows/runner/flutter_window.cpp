#include "flutter_window.h"

#include <dwmapi.h>
#include <optional>

#include "flutter/generated_plugin_registrant.h"

FlutterWindow::FlutterWindow(const flutter::DartProject& project)
    : project_(project) {}

FlutterWindow::~FlutterWindow() {}

bool FlutterWindow::OnCreate() {
  if (!Win32Window::OnCreate()) {
    return false;
  }

  RECT frame = GetClientArea();

  // The size here must match the window dimensions to avoid unnecessary surface
  // creation / destruction in the startup path.
  flutter_controller_ = std::make_unique<flutter::FlutterViewController>(
      frame.right - frame.left, frame.bottom - frame.top, project_);
  // Ensure that basic setup of the controller was successful.
  if (!flutter_controller_->engine() || !flutter_controller_->view()) {
    return false;
  }
  RegisterPlugins(flutter_controller_->engine());
  SetChildContent(flutter_controller_->view()->GetNativeWindow());

  // Listen for dark/light mode toggle events from Flutter
  window_channel_ =
      std::make_unique<flutter::MethodChannel<flutter::EncodableValue>>(
          flutter_controller_->engine()->messenger(),
          "com.sare.sare_kiosk/window",
          &flutter::StandardMethodCodec::GetInstance());

#ifndef DWMWA_USE_IMMERSIVE_DARK_MODE
#define DWMWA_USE_IMMERSIVE_DARK_MODE 20
#endif
#ifndef DWMWA_CAPTION_COLOR
#define DWMWA_CAPTION_COLOR 35
#endif
#ifndef DWMWA_TEXT_COLOR
#define DWMWA_TEXT_COLOR 36
#endif

  window_channel_->SetMethodCallHandler(
      [this](const flutter::MethodCall<flutter::EncodableValue>& call,
             std::unique_ptr<flutter::MethodResult<flutter::EncodableValue>> result) {
        if (call.method_name().compare("setDarkMode") == 0) {
          if (const auto* is_dark = std::get_if<bool>(call.arguments())) {
            BOOL enable_dark_mode = *is_dark ? TRUE : FALSE;
            COLORREF caption_color = *is_dark ? RGB(0, 0, 0) : RGB(255, 255, 255);
            COLORREF text_color = *is_dark ? RGB(255, 255, 255) : RGB(0, 0, 0);

            DwmSetWindowAttribute(GetHandle(), DWMWA_USE_IMMERSIVE_DARK_MODE,
                                  &enable_dark_mode, sizeof(enable_dark_mode));
            DwmSetWindowAttribute(GetHandle(), DWMWA_CAPTION_COLOR,
                                  &caption_color, sizeof(caption_color));
            DwmSetWindowAttribute(GetHandle(), DWMWA_TEXT_COLOR,
                                  &text_color, sizeof(text_color));
            SetWindowPos(GetHandle(), nullptr, 0, 0, 0, 0,
                         SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
            result->Success();
            return;
          }
        } else if (call.method_name().compare("centerWindow") == 0) {
          RECT rc;
          GetWindowRect(GetHandle(), &rc);
          int w = rc.right - rc.left;
          int h = rc.bottom - rc.top;
          int screen_w = GetSystemMetrics(SM_CXSCREEN);
          int screen_h = GetSystemMetrics(SM_CYSCREEN);
          int x = (screen_w - w) / 2;
          int y = (screen_h - h) / 2;
          SetWindowPos(GetHandle(), nullptr, x > 0 ? x : 0, y > 0 ? y : 0, 0, 0,
                       SWP_NOSIZE | SWP_NOZORDER);
          result->Success();
          return;
        }
        result->NotImplemented();
      });

  flutter_controller_->engine()->SetNextFrameCallback([&]() {
    this->Show();
  });

  // Flutter can complete the first frame before the "show window" callback is
  // registered. The following call ensures a frame is pending to ensure the
  // window is shown. It is a no-op if the first frame hasn't completed yet.
  flutter_controller_->ForceRedraw();

  return true;
}

void FlutterWindow::OnDestroy() {
  if (flutter_controller_) {
    flutter_controller_ = nullptr;
  }

  Win32Window::OnDestroy();
}

LRESULT
FlutterWindow::MessageHandler(HWND hwnd, UINT const message,
                              WPARAM const wparam,
                              LPARAM const lparam) noexcept {
  // Give Flutter, including plugins, an opportunity to handle window messages.
  if (flutter_controller_) {
    std::optional<LRESULT> result =
        flutter_controller_->HandleTopLevelWindowProc(hwnd, message, wparam,
                                                      lparam);
    if (result) {
      return *result;
    }
  }

  switch (message) {
    case WM_FONTCHANGE:
      flutter_controller_->engine()->ReloadSystemFonts();
      break;
  }

  return Win32Window::MessageHandler(hwnd, message, wparam, lparam);
}
