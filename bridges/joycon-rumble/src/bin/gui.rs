#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::fmt;
use std::fs::File;
use std::path::PathBuf;
use std::process::{Child, Command, Stdio};

#[cfg(windows)]
use std::os::windows::process::CommandExt;

use iced::theme::Palette;
use iced::widget::{
    button, column, container, horizontal_space, radio, row, scrollable, text, text_input,
};
use iced::{Alignment, Color, Element, Font, Length, Task, Theme, border};

const ACTION_BUTTON_WIDTH: f32 = 150.0;
const ACTION_BUTTON_HEIGHT: f32 = 36.0;
const ACTION_COLOR: Color = Color::from_rgb(0.604, 0.302, 0.0);
const ACTION_HOVER_COLOR: Color = Color::from_rgb(0.455, 0.227, 0.0);
const DANGER_COLOR: Color = Color::from_rgb(0.702, 0.149, 0.118);
const DANGER_HOVER_COLOR: Color = Color::from_rgb(0.510, 0.086, 0.067);
const DISABLED_COLOR: Color = Color::from_rgb(0.357, 0.396, 0.451);
#[cfg(windows)]
const CREATE_NO_WINDOW: u32 = 0x0800_0000;

fn main() -> iced::Result {
    iced::application("Joy-Con Bridge - BYO Haptics", update, view)
        .theme(|_| universal_design_theme())
        .default_font(Font::with_name("Yu Gothic UI"))
        .window_size((720.0, 720.0))
        .run_with(|| (App::default(), Task::none()))
}

fn universal_design_theme() -> Theme {
    Theme::custom(
        "Universal Design".into(),
        Palette {
            background: Color::from_rgb8(0xF3, 0xF4, 0xF6),
            text: Color::from_rgb8(0x11, 0x18, 0x27),
            primary: Color::from_rgb8(0x9A, 0x4D, 0x00),
            success: Color::from_rgb8(0x00, 0x6B, 0x5E),
            danger: Color::from_rgb8(0xB3, 0x26, 0x1E),
        },
    )
}

fn action_button_style(
    _theme: &Theme,
    status: iced::widget::button::Status,
) -> iced::widget::button::Style {
    solid_button_style(status, ACTION_COLOR, ACTION_HOVER_COLOR)
}

fn danger_button_style(
    _theme: &Theme,
    status: iced::widget::button::Status,
) -> iced::widget::button::Style {
    solid_button_style(status, DANGER_COLOR, DANGER_HOVER_COLOR)
}

fn solid_button_style(
    status: iced::widget::button::Status,
    active: Color,
    hover: Color,
) -> iced::widget::button::Style {
    let background = match status {
        iced::widget::button::Status::Active => active,
        iced::widget::button::Status::Hovered => hover,
        iced::widget::button::Status::Pressed => hover,
        iced::widget::button::Status::Disabled => DISABLED_COLOR,
    };
    iced::widget::button::Style {
        background: Some(background.into()),
        text_color: Color::WHITE,
        border: border::rounded(4),
        ..iced::widget::button::Style::default()
    }
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Default)]
enum Side {
    Left,
    #[default]
    Right,
}

impl Side {
    fn argument(self) -> &'static str {
        match self {
            Self::Left => "left",
            Self::Right => "right",
        }
    }
}

impl fmt::Display for Side {
    fn fmt(&self, formatter: &mut fmt::Formatter<'_>) -> fmt::Result {
        formatter.write_str(match self {
            Self::Left => "Joy-Con (L)",
            Self::Right => "Joy-Con (R)",
        })
    }
}

struct App {
    side: Side,
    csv_path: String,
    profile_path: String,
    listen: String,
    busy: bool,
    scan_completed: bool,
    left_detected: bool,
    right_detected: bool,
    bridge: Option<Child>,
    status: String,
    log: String,
}

impl Default for App {
    fn default() -> Self {
        Self {
            side: Side::Right,
            csv_path: "joycon-imu-sweep.csv".into(),
            profile_path: "joycon-rumble-profiles.toml".into(),
            listen: "0.0.0.0:9001".into(),
            busy: false,
            scan_completed: false,
            left_detected: false,
            right_detected: false,
            bridge: None,
            status: "準備完了。Joy-Conを接続して、デバイスを検索してください。".into(),
            log: String::new(),
        }
    }
}

impl Drop for App {
    fn drop(&mut self) {
        if let Some(child) = self.bridge.as_mut() {
            let _ = child.kill();
            let _ = child.wait();
        }
    }
}

#[derive(Debug, Clone)]
enum Message {
    SideSelected(Side),
    CsvChanged(String),
    ProfileChanged(String),
    ListenChanged(String),
    Scan,
    Measure,
    CommandFinished(CommandResult),
    StartBridge,
    StopBridge,
}

#[derive(Debug, Clone)]
struct CommandResult {
    title: &'static str,
    success: bool,
    output: String,
}

fn update(app: &mut App, message: Message) -> Task<Message> {
    match message {
        Message::SideSelected(side) => app.side = side,
        Message::CsvChanged(value) => app.csv_path = value,
        Message::ProfileChanged(value) => app.profile_path = value,
        Message::ListenChanged(value) => app.listen = value,
        Message::Scan if !app.busy => {
            app.busy = true;
            app.status = "Joy-Conを検索しています…".into();
            return Task::perform(
                run_cli("デバイス検索", vec!["joycon-list".into()]),
                Message::CommandFinished,
            );
        }
        Message::Measure if !app.busy && app.bridge.is_none() => {
            app.busy = true;
            app.status = format!(
                "{}を測定しています。約1分間、動かさないでください…",
                app.side
            );
            let args = vec![
                "joycon-imu-sweep".into(),
                "--side".into(),
                app.side.argument().into(),
                "--output".into(),
                app.csv_path.clone(),
                "--profile".into(),
                app.profile_path.clone(),
            ];
            return Task::perform(
                run_cli("IMUキャリブレーション", args),
                Message::CommandFinished,
            );
        }
        Message::CommandFinished(result) => {
            app.busy = false;
            if result.title == "デバイス検索" {
                app.scan_completed = true;
                app.left_detected = result.output.contains("Left Joy-Con");
                app.right_detected = result.output.contains("Right Joy-Con");
                if app.side == Side::Left && !app.left_detected && app.right_detected {
                    app.side = Side::Right;
                } else if app.side == Side::Right && !app.right_detected && app.left_detected {
                    app.side = Side::Left;
                }
            }
            app.status = if result.title == "デバイス検索" {
                if !result.success {
                    "Joy-Conの検索に失敗しました。下のログを確認してください。".into()
                } else if app.left_detected || app.right_detected {
                    "接続済みJoy-Conを更新しました。".into()
                } else {
                    "Joy-Conが見つかりません。ボタンを押して再検索してください。".into()
                }
            } else if result.success {
                format!("{}が正常に完了しました。", result.title)
            } else {
                format!(
                    "{}に失敗しました。下のログを確認してください。",
                    result.title
                )
            };
            app.log = localize_cli_output(&result.output);
        }
        Message::StartBridge if !app.busy && app.bridge.is_none() => {
            match start_bridge(&app.listen, &app.profile_path) {
                Ok(child) => {
                    app.bridge = Some(child);
                    app.status = format!("ブリッジを{}で実行しています。", app.listen);
                    app.log = "ブリッジログ: joycon-rumble-gui-bridge.log".into();
                }
                Err(error) => {
                    app.status = "ブリッジを起動できませんでした。".into();
                    app.log = error.to_string();
                }
            }
        }
        Message::StopBridge => {
            if let Some(mut child) = app.bridge.take() {
                let _ = child.kill();
                let _ = child.wait();
                app.status = "ブリッジを停止しました。".into();
            }
        }
        _ => {}
    }
    Task::none()
}

fn view(app: &App) -> Element<'_, Message> {
    let selected_detected = match app.side {
        Side::Left => app.left_detected,
        Side::Right => app.right_detected,
    };
    let scan = (if app.busy {
        button("デバイス検索")
    } else {
        button("デバイス検索").on_press(Message::Scan)
    })
    .width(Length::Fixed(ACTION_BUTTON_WIDTH))
    .height(Length::Fixed(ACTION_BUTTON_HEIGHT))
    .style(action_button_style);
    let measure = (if app.busy || app.bridge.is_some() || !selected_detected {
        button("測定して最適化")
    } else {
        button("測定して最適化").on_press(Message::Measure)
    })
    .width(Length::Fixed(ACTION_BUTTON_WIDTH))
    .height(Length::Fixed(ACTION_BUTTON_HEIGHT))
    .style(action_button_style);
    let bridge_running = app.bridge.is_some();
    let bridge_button = (if bridge_running {
        button("ブリッジ停止").on_press(Message::StopBridge)
    } else if app.busy {
        button("ブリッジ起動")
    } else {
        button("ブリッジ起動").on_press(Message::StartBridge)
    })
    .width(Length::Fixed(ACTION_BUTTON_WIDTH))
    .height(Length::Fixed(ACTION_BUTTON_HEIGHT))
    .style(if bridge_running {
        danger_button_style
    } else {
        action_button_style
    });

    let left_status = if !app.scan_completed {
        "Joy-Con (L)  未検索"
    } else if app.left_detected {
        "Joy-Con (L)  接続済み"
    } else {
        "Joy-Con (L)  未接続"
    };
    let right_status = if !app.scan_completed {
        "Joy-Con (R)  未検索"
    } else if app.right_detected {
        "Joy-Con (R)  接続済み"
    } else {
        "Joy-Con (R)  未接続"
    };

    let device_section = container(
        column![
            text("1. Joy-Con接続").size(20),
            row![
                container(text(left_status))
                    .width(Length::Fixed(190.0))
                    .padding(10)
                    .style(iced::widget::container::bordered_box),
                container(text(right_status))
                    .width(Length::Fixed(190.0))
                    .padding(10)
                    .style(iced::widget::container::bordered_box),
                horizontal_space(),
                scan,
            ]
            .spacing(10)
            .align_y(Alignment::Center),
        ]
        .spacing(12),
    )
    .width(Length::Fill)
    .padding(16)
    .style(iced::widget::container::bordered_box);

    let calibration_section = container(
        column![
            text("2. 振動の測定と最適化").size(20),
            row![
                text("測定対象").width(Length::Fixed(170.0)),
                radio(
                    "Joy-Con (L)",
                    Side::Left,
                    Some(app.side),
                    Message::SideSelected
                ),
                radio(
                    "Joy-Con (R)",
                    Side::Right,
                    Some(app.side),
                    Message::SideSelected
                )
            ]
            .spacing(18)
            .align_y(Alignment::Center),
            text("Joy-Conを安定した場所に置き、測定中は動かさないでください。"),
            row![
                text("測定結果CSV").width(Length::Fixed(170.0)),
                text_input("joycon-imu-sweep.csv", &app.csv_path)
                    .on_input(Message::CsvChanged)
                    .width(Length::Fixed(390.0)),
            ]
            .align_y(Alignment::Center),
            row![
                text("最適化プロファイル").width(Length::Fixed(170.0)),
                text_input("joycon-rumble-profiles.toml", &app.profile_path)
                    .on_input(Message::ProfileChanged)
                    .width(Length::Fixed(390.0)),
            ]
            .align_y(Alignment::Center),
            row![horizontal_space(), measure,],
        ]
        .spacing(12),
    )
    .width(Length::Fill)
    .padding(16)
    .style(iced::widget::container::bordered_box);

    let bridge_section = container(
        column![
            text("3. OSCブリッジ").size(20),
            row![
                text("待受アドレス").width(Length::Fixed(170.0)),
                text_input("0.0.0.0:9001", &app.listen)
                    .on_input(Message::ListenChanged)
                    .width(Length::Fixed(220.0)),
                horizontal_space(),
                bridge_button,
            ]
            .align_y(Alignment::Center),
        ]
        .spacing(12),
    )
    .width(Length::Fill)
    .padding(16)
    .style(iced::widget::container::bordered_box);

    let log_section = container(
        column![
            text("詳細ログ（実行履歴）").size(20),
            scrollable(text(if app.log.is_empty() {
                "ログはありません。"
            } else {
                &app.log
            }))
            .height(Length::Fill),
        ]
        .spacing(8),
    )
    .width(Length::Fill)
    .height(Length::Fill)
    .padding(16)
    .style(iced::widget::container::bordered_box);

    let content = column![
        text("Joy-Con Bridge - BYO Haptics").size(30),
        text("接続確認、振動の最適化、OSCブリッジの操作を順番に行います。"),
        row![text("現在の実行状況:").size(16), text(&app.status).size(16),]
            .spacing(10)
            .align_y(Alignment::Center),
        device_section,
        calibration_section,
        bridge_section,
        log_section,
    ]
    .spacing(12)
    .padding(18)
    .max_width(700);

    container(content)
        .center_x(Length::Fill)
        .height(Length::Fill)
        .into()
}

async fn run_cli(title: &'static str, args: Vec<String>) -> CommandResult {
    let result = bridge_command().args(args).output();
    match result {
        Ok(output) => {
            let mut text = String::from_utf8_lossy(&output.stdout).into_owned();
            text.push_str(&String::from_utf8_lossy(&output.stderr));
            CommandResult {
                title,
                success: output.status.success(),
                output: text,
            }
        }
        Err(error) => CommandResult {
            title,
            success: false,
            output: error.to_string(),
        },
    }
}

fn localize_cli_output(output: &str) -> String {
    output
        .replace(
            "No Joy-Con HID devices found via hidapi.",
            "Joy-Conが見つかりませんでした。",
        )
        .replace(
            "Expected Nintendo VID 057e with PID 2006 (L) or 2007 (R).",
            "Nintendo VID 057e、PID 2006（L）または2007（R）を検索しました。",
        )
        .replace("Found ", "検出数: ")
        .replace(" Joy-Con HID device(s):", " 台のJoy-Con")
        .replace("Left Joy-Con", "Joy-Con (L)")
        .replace("Right Joy-Con", "Joy-Con (R)")
        .replace("product=", "製品=")
        .replace("serial=", "シリアル=")
        .replace("path=", "パス=")
        .replace("open: ok", "接続可能")
        .replace("open: failed", "接続失敗")
        .replace("Keep the ", "")
        .replace(
            " stationary. Stabilizing IMU...",
            "を動かさないでください。IMUを安定化中…",
        )
        .replace("Measuring three baseline windows...", "基準値を3回測定中…")
        .replace("baseline samples=", "基準サンプル数=")
        .replace(" noise_rms_lsb=", " ノイズRMS=")
        .replace("amplitude=", "振幅=")
        .replace(" low=", " Low=")
        .replace(" high=", " High=")
        .replace(" samples=", " サンプル数=")
        .replace(" rms=", " RMS=")
        .replace("IMU sweep finished:", "IMU測定結果:")
        .replace("optimized profile saved:", "最適化プロファイル保存先:")
}

fn start_bridge(listen: &str, profile: &str) -> std::io::Result<Child> {
    let log = File::create("joycon-rumble-gui-bridge.log")?;
    let error_log = log.try_clone()?;
    bridge_command()
        .args(["--listen", listen, "--imu-profile", profile])
        .stdout(Stdio::from(log))
        .stderr(Stdio::from(error_log))
        .spawn()
}

fn bridge_command() -> Command {
    let mut command = Command::new(bridge_executable());
    #[cfg(windows)]
    command.creation_flags(CREATE_NO_WINDOW);
    command
}

fn bridge_executable() -> PathBuf {
    let mut path =
        std::env::current_exe().unwrap_or_else(|_| PathBuf::from("joycon-rumble-gui.exe"));
    path.set_file_name(if cfg!(windows) {
        "joycon-rumble-bridge.exe"
    } else {
        "joycon-rumble-bridge"
    });
    path
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn localizes_device_and_measurement_output() {
        let localized = localize_cli_output(
            "Found 1 Joy-Con HID device(s):\nLeft Joy-Con open: ok\n\
             amplitude=1.00 low=90.0 high=180.0 samples=100 rms=1200.0\n\
             optimized profile saved: joycon-rumble-profiles.toml",
        );
        assert!(localized.contains("検出数: 1 台のJoy-Con"));
        assert!(localized.contains("Joy-Con (L) 接続可能"));
        assert!(localized.contains("振幅=1.00 Low=90.0 High=180.0 サンプル数=100 RMS=1200.0"));
        assert!(localized.contains("最適化プロファイル保存先:"));
    }

    #[test]
    fn button_text_colors_meet_wcag_normal_text_contrast() {
        for background in [
            ACTION_COLOR,
            ACTION_HOVER_COLOR,
            DANGER_COLOR,
            DANGER_HOVER_COLOR,
            DISABLED_COLOR,
        ] {
            assert!(contrast_ratio(Color::WHITE, background) >= 4.5);
        }
    }

    fn contrast_ratio(first: Color, second: Color) -> f32 {
        let first = relative_luminance(first);
        let second = relative_luminance(second);
        (first.max(second) + 0.05) / (first.min(second) + 0.05)
    }

    fn relative_luminance(color: Color) -> f32 {
        fn linear(value: f32) -> f32 {
            if value <= 0.04045 {
                value / 12.92
            } else {
                ((value + 0.055) / 1.055).powf(2.4)
            }
        }
        0.2126 * linear(color.r) + 0.7152 * linear(color.g) + 0.0722 * linear(color.b)
    }
}
