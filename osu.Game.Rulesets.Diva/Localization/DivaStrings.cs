// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Rulesets.Diva.Localization
{
    public static class DivaStrings
    {
        #region Judgement display names (proper nouns — same in zh/en)

        public const string JUDGEMENT_COOL = "COOL";
        public const string JUDGEMENT_FINE = "FINE";
        public const string JUDGEMENT_SAFE = "SAFE";
        public const string JUDGEMENT_SAD = "SAD";
        public const string JUDGEMENT_WRONG = "WRONG";
        public const string JUDGEMENT_WORST = "WORST";

        #endregion

        #region Settings

        public static readonly LocalisableString SETTINGS_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("osu!DIVA", "osu!DIVA");

        public static readonly LocalisableString SETTINGS_OPEN_PATH_WIZARD =
            new DivaLocalizationManager.DivaLocalisableString("打开谱面曲库路径向导", "Open beatmap library path wizard");

        public static readonly LocalisableString SETTINGS_IMPORT_TO_REALM =
            new DivaLocalizationManager.DivaLocalisableString("将谱面导入 Realm", "Import charts into Realm");

        public static readonly LocalisableString SETTINGS_IMPORT_TO_REALM_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "启用后，.diva 谱面会转换为 .osu 并导入 osu! 曲库。在 Ez2Lazer 上可关闭此项以改为外部挂载文件夹。",
            "When enabled, .diva charts are converted to .osu and imported into the osu! library. On Ez2Lazer, disable to mount folders externally instead.");

        public static readonly LocalisableString SETTINGS_USE_XBOX_BUTTON_ICONS =
            new DivaLocalizationManager.DivaLocalisableString("使用 XBox 按键图标", "Use XBox Button Icons");

        public static readonly LocalisableString SETTINGS_ENABLE_VISUAL_BURSTS =
            new DivaLocalizationManager.DivaLocalisableString("启用视觉爆发特效", "Enable visual bursts");

        public static readonly LocalisableString SETTINGS_ENABLE_BUILTIN_HIT_SOUNDS =
            new DivaLocalizationManager.DivaLocalisableString("启用内置打击音效", "Enable built-in hit sounds");

        public static readonly LocalisableString SETTINGS_ENABLE_BUILTIN_HIT_SOUNDS_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "按键时播放规则集内置的 ProjectDIVA 打击 SE。不影响曲终评级 VO。",
            "Play the ruleset-embedded ProjectDIVA hit SE on key presses. Does not affect end-of-song grade VO.");

        public static readonly LocalisableString SETTINGS_JUDGEMENT_LOCK =
            new DivaLocalizationManager.DivaLocalisableString("判定锁定", "Judgement lock");

        public static readonly LocalisableString SETTINGS_JUDGEMENT_LOCK_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "ProjectDIVA Strict/Standard：启用时，在判定窗内按错键会消耗该音符（WRONG）；关闭时忽略错键。",
            "ProjectDIVA Strict/Standard: when enabled, a wrong key within the timing window consumes the note (WRONG). When disabled, wrong keys are ignored.");

        public static readonly LocalisableString SETTINGS_INPUT_OFFSET =
            new DivaLocalizationManager.DivaLocalisableString("输入偏移 (ms)", "Input offset (ms)");

        public static readonly LocalisableString SETTINGS_INPUT_OFFSET_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "调整用于判定的打击时机（类似 Offset Plus）。不改变音频/时钟同步。范围 ±200ms。",
            "Adjusts hit timing used for judgement (like Offset Plus). Does not change audio/clock sync. Range ±200ms.");

        public static readonly LocalisableString SETTINGS_NOTE_SIZE =
            new DivaLocalizationManager.DivaLocalisableString("音符大小", "Note size");

        public static readonly LocalisableString SETTINGS_PLAYFIELD_SCALE =
            new DivaLocalizationManager.DivaLocalisableString("游玩区域缩放", "Playfield scale");

        public static readonly LocalisableString SETTINGS_PLAYFIELD_SCALE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "按本谱逻辑活动区域等比塞进可用屏幕后再缩放（随谱面/屏幕比例自适应）。1.0 为最大可贴合且不超出屏幕。",
            "Contain-fits this chart's logical note field into the available area, then scales (adapts to chart/screen aspect). 1.0 = maximum fit without going off-screen.");

        public static readonly LocalisableString SETTINGS_APPROACH_PREEMPT_SCALE =
            new DivaLocalizationManager.DivaLocalisableString("接近预判缩放", "Approach preempt scale");

        public static readonly LocalisableString SETTINGS_APPROACH_PREEMPT_SCALE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "ProjectDIVA note_standing×BPM 的倍率（1.0 = PD 默认）。越小则音符出现越晚。",
            "Multiplier on ProjectDIVA note_standing×BPM (1.0 = PD default). Lower = notes appear later.");

        public static readonly LocalisableString SETTINGS_HIT_EXPLOSION_ALPHA =
            new DivaLocalizationManager.DivaLocalisableString("打击爆发透明度", "Hit Explosion alpha");

        public static readonly LocalisableString SETTINGS_NOTE_APPEARANCE =
            new DivaLocalizationManager.DivaLocalisableString("音符出现方式", "Note appearance");

        public static readonly LocalisableString SETTINGS_NOTE_APPEARANCE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "DIVA 原生：飞行件在场地外不绘制、进场即满尺寸弹出，目标音符从 1.8 倍缩回并匀速旋转（与 ProjectDIVA 一致）。淡入：保留原有整体淡入 + 指针缩放旋转。",
            "DIVA native: flying pieces are not drawn outside the field and pop in at full size, while the target note shrinks back from 1.8x and spins at a constant rate (matches ProjectDIVA). Fade in: the previous whole-note fade with pointer scale/spin.");

        public static readonly LocalisableString APPEARANCE_DIVA_NATIVE =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 原生", "DIVA native");

        public static readonly LocalisableString APPEARANCE_FADE_IN =
            new DivaLocalizationManager.DivaLocalisableString("淡入", "Fade in");

        public static readonly LocalisableString SETTINGS_FLIGHT_CURVE =
            new DivaLocalizationManager.DivaLocalisableString("飞行轨迹曲线", "Flight curve");

        public static readonly LocalisableString SETTINGS_FLIGHT_CURVE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "ProjectDIVA 原版为 S 形三次贝塞尔（中点在直线上）；其余为二次单弧、样条以及只改变速度剖面的缓动直线。侧偏大小见「轨迹侧偏幅度」。",
            "ProjectDIVA's original is an S-shaped cubic Bézier (midpoint lies on the chord); alternatives are a quadratic arc, a spline, and eased straight lines that only change the speed profile. Lateral size is set by the flight amplitude.");

        public static readonly LocalisableString CURVE_DIVA_NATIVE =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 原生（S 形贝塞尔）", "DIVA native (S Bézier)");

        public static readonly LocalisableString CURVE_QUADRATIC =
            new DivaLocalizationManager.DivaLocalisableString("二次单弧（C 形）", "Quadratic arc (C shape)");

        public static readonly LocalisableString CURVE_CATMULL_ROM =
            new DivaLocalizationManager.DivaLocalisableString("样条（Hermite）", "Spline (Hermite)");

        public static readonly LocalisableString CURVE_EASED_SMOOTH_STEP =
            new DivaLocalizationManager.DivaLocalisableString("缓入缓出（直线）", "Ease in-out (straight)");

        public static readonly LocalisableString CURVE_EASED_EXPO_OUT =
            new DivaLocalizationManager.DivaLocalisableString("急出减速（直线）", "Ease out (straight)");

        public static readonly LocalisableString SETTINGS_FLIGHT_AMPLITUDE =
            new DivaLocalizationManager.DivaLocalisableString("轨迹侧偏幅度", "Flight lateral amplitude");

        public static readonly LocalisableString SETTINGS_FLIGHT_AMPLITUDE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "100% = 各曲线自身的 ProjectDIVA 基准幅度（默认即原版观感），0% = 直线。对缓动直线无效。",
            "100% = each curve's own ProjectDIVA baseline (the default matches the original), 0% = straight line. Has no effect on the eased straight lines.");

        public static readonly LocalisableString SETTINGS_HOLD_STAR_DENSITY =
            new DivaLocalizationManager.DivaLocalisableString("长条星尘密度", "Strip star density");

        public static readonly LocalisableString SETTINGS_HOLD_STAR_DENSITY_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "长条 body 上星尘的数量：100% 为默认间距（约为修复前 60fps 的观感），星星按弧长等距分布、贴住长条缩短自动减少，不会无限累积；短长条有最低数量以维持观感，0% 关闭星尘。",
            "How many stars the hold body carries: 100% is the default spacing (roughly the pre-fix 60fps look). Stars stay evenly spaced by arc length and drop away as the strip shrinks, so they never accumulate without bound; short strips keep a floor so they still read as stardust. 0% hides them.");

        public static readonly LocalisableString SETTINGS_CANNOT_OPEN_WIZARD =
            new DivaLocalizationManager.DivaLocalisableString("无法从此界面打开路径向导。", "Cannot open path wizard from this screen.");

        public static readonly LocalisableString SETTINGS_ADD_VALID_PATH_FIRST =
            new DivaLocalizationManager.DivaLocalisableString("请先添加至少一个有效的文件夹路径。", "Add at least one valid folder path first.");

        public static readonly LocalisableString SETTINGS_CLEARING_LIBRARY =
            new DivaLocalizationManager.DivaLocalisableString("正在清空 DIVA 曲库路径…", "Clearing DIVA library paths…");

        public static readonly LocalisableString SETTINGS_IMPORTING_LIBRARY =
            new DivaLocalizationManager.DivaLocalisableString("正在导入 DIVA 谱面…", "Importing DIVA charts…");

        public static readonly LocalisableString SETTINGS_LINKING_LIBRARY =
            new DivaLocalizationManager.DivaLocalisableString("正在链接 DIVA 外部曲库…", "Linking DIVA external library…");

        public static readonly LocalisableString SETTINGS_LIBRARY_CLEARED =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 曲库路径已清空。", "DIVA library paths cleared.");

        public static readonly LocalisableString SETTINGS_LIBRARY_UPDATE_COMPLETE =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 曲库更新完成。", "DIVA library update complete.");

        public static readonly LocalisableString SETTINGS_PATHS_CLEARED_PROGRESS =
            new DivaLocalizationManager.DivaLocalisableString("路径已清空。", "Paths cleared.");

        public static readonly LocalisableString SETTINGS_NO_PATHS_CONFIGURED =
            new DivaLocalizationManager.DivaLocalisableString("未配置曲库路径。", "No library paths configured.");

        public static string Settings_PathStatus(int pathCount, int songCount, int difficultyCount) =>
            settings_path_status_template.Format(pathCount, songCount, difficultyCount);

        public static string Settings_CollectionsSynced(int collectionCount, int chartCount) =>
            settings_collections_synced_template.Format(collectionCount, chartCount);

        public static string Settings_CollectionsSyncedDiva(int collectionCount, int chartCount) =>
            settings_collections_synced_diva_template.Format(collectionCount, chartCount);

        public static string Settings_LibraryUpdateFailed(string typeName, string message) =>
            settings_library_update_failed_template.Format(typeName, message);

        private static readonly DivaLocalizationManager.DivaLocalisableString settings_path_status_template =
            new DivaLocalizationManager.DivaLocalisableString("路径数 {0}，总歌曲数 {1}，总难度数 {2}",
                "Paths: {0}, songs: {1}, difficulties: {2}");

        private static readonly DivaLocalizationManager.DivaLocalisableString settings_collections_synced_template =
            new DivaLocalizationManager.DivaLocalisableString(" 已同步 {0} 个路径收藏夹，共 {1} 张谱面。",
                " Synced {0} path collection(s), {1} chart(s).");

        private static readonly DivaLocalizationManager.DivaLocalisableString settings_collections_synced_diva_template =
            new DivaLocalizationManager.DivaLocalisableString("已同步 {0} 个 DIVA 路径收藏夹，共 {1} 张谱面。",
                "Synced {0} DIVA path collection(s), {1} chart(s).");

        private static readonly DivaLocalizationManager.DivaLocalisableString settings_library_update_failed_template =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 曲库更新失败：{0}: {1}",
                "DIVA library update failed: {0}: {1}");

        #endregion

        #region Path wizard

        public static readonly LocalisableString PATH_WIZARD_TITLE =
            new DivaLocalizationManager.DivaLocalisableString("DIVA 谱面曲库路径", "DIVA beatmap library paths");

        public static readonly LocalisableString PATH_WIZARD_TITLE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "选择包含 ProjectDIVA 歌曲包的文件夹（含 .diva 文件的目录）。",
            "Select folders that contain ProjectDIVA song packages (folders with .diva files).");

        public static readonly LocalisableString PATH_WIZARD_INTRO = new DivaLocalizationManager.DivaLocalisableString(
            "添加一个或多个曲库根目录，然后点击「应用」。导入模式会将谱面转换并写入 osu! 曲库。",
            "Add one or more song library roots, then Apply. Import mode converts charts into the osu! library.");

        public static readonly LocalisableString PATH_WIZARD_INTRO_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "Ez2Lazer 可关闭「导入」以外部挂载文件夹，无需复制文件。",
            "Ez2Lazer can disable Import to mount folders externally without copying files.");

        public static readonly LocalisableString PATH_WIZARD_IMPORT_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "开：将 .diva 转为 .osu 并导入。关（仅 Ez2Lazer）：外部文件夹挂载。",
            "On: convert .diva → .osu and import. Off (Ez2Lazer only): external folder mount.");

        public static readonly LocalisableString PATH_WIZARD_ADD_CURRENT_PATH =
            new DivaLocalizationManager.DivaLocalisableString("添加当前路径", "Add current path");

        public static readonly LocalisableString PATH_WIZARD_CLEAR_LIST =
            new DivaLocalizationManager.DivaLocalisableString("清空列表", "Clear list");

        public static readonly LocalisableString PATH_WIZARD_ADDED_PATHS =
            new DivaLocalizationManager.DivaLocalisableString("已添加的路径", "Added paths");

        public static readonly LocalisableString PATH_WIZARD_CLOSE =
            new DivaLocalizationManager.DivaLocalisableString("关闭", "Close");

        public static readonly LocalisableString PATH_WIZARD_APPLY =
            new DivaLocalizationManager.DivaLocalisableString("应用", "Apply");

        public static readonly LocalisableString PATH_WIZARD_SEPARATE_AUDIO =
            new DivaLocalizationManager.DivaLocalisableString("分离音轨", "Separate audio");

        public static readonly LocalisableString PATH_WIZARD_NO_PATHS_YET =
            new DivaLocalizationManager.DivaLocalisableString("暂未添加路径。", "No paths added yet.");

        public static readonly LocalisableString PATH_WIZARD_REMOVE =
            new DivaLocalizationManager.DivaLocalisableString("移除", "Remove");

        public static readonly LocalisableString PATH_WIZARD_REMOVE_DIALOG_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("移除曲库路径？", "Remove library path?");

        public static readonly LocalisableString PATH_WIZARD_SEPARATE_AUDIO_DIALOG_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("分离音轨并修改谱面？", "Separate audio and modify charts?");

        public static readonly LocalisableString PATH_WIZARD_SEPARATE_AUDIO_DIALOG_BODY = new DivaLocalizationManager.DivaLocalisableString(
            "将扫描当前库路径下的歌曲：若 WAV 文件夹没有音频，会尝试用 ffmpeg 从 RES 视频抽出音轨写入 WAV，"
            + "并修改该歌曲下所有 .diva 的 wav 关联。此操作会改磁盘上的谱面文件，请先备份。完成后会自动应用。",
            "Scans songs under the current library paths: if the WAV folder has no audio, ffmpeg will try extracting a track from RES video into WAV, "
            + "and rewrite wav associations on all .diva charts in that song. This modifies chart files on disk — back up first. Apply runs automatically when done.");

        public static readonly LocalisableString PATH_WIZARD_SEPARATE_AUDIO_NEED_PATH =
            new DivaLocalizationManager.DivaLocalisableString("请先添加至少一个曲库路径再分离音轨。",
                "Add at least one library path before separating audio tracks.");

        public static string PathWizard_SeparateAudioFailed(string message) =>
            path_wizard_separate_audio_failed_template.Format(message);

        public static string PathWizard_SeparateAudioSummary(string summary) =>
            path_wizard_separate_audio_summary_template.Format(summary);

        private static readonly DivaLocalizationManager.DivaLocalisableString path_wizard_separate_audio_failed_template =
            new DivaLocalizationManager.DivaLocalisableString("[DIVA] 分离音轨失败：{0}", "[DIVA] Separate audio failed: {0}");

        private static readonly DivaLocalizationManager.DivaLocalisableString path_wizard_separate_audio_summary_template =
            new DivaLocalizationManager.DivaLocalisableString("[DIVA] {0}", "[DIVA] {0}");

        #endregion

        #region Editor

        public static readonly LocalisableString EDITOR_VARIANT =
            new DivaLocalizationManager.DivaLocalisableString("编辑器", "Editor");

        public static readonly LocalisableString EDITOR_TAP_TOOL =
            new DivaLocalizationManager.DivaLocalisableString("单击", "Tap");

        public static readonly LocalisableString EDITOR_HOLD_TOOL =
            new DivaLocalizationManager.DivaLocalisableString("长按", "Hold");

        public static readonly LocalisableString EDITOR_GRID_SNAP =
            new DivaLocalizationManager.DivaLocalisableString("格子吸附", "Grid Snap");

        public static readonly LocalisableString EDITOR_REPLACE_ON_SAME_TIME =
            new DivaLocalizationManager.DivaLocalisableString("同拍替换", "Replace same time");

        public static readonly LocalisableString EDITOR_REPLACE_ON_SAME_TIME_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "开启后，在同一拍再放会覆盖已有音符。关闭后可在同一拍叠放多个音符。",
            "When on, placing on the same beat replaces the existing note. When off, multiple notes can share a beat.");

        public static readonly LocalisableString EDITOR_INSPECTOR_GRID_X =
            new DivaLocalizationManager.DivaLocalisableString("格子 X", "Grid X");

        public static readonly LocalisableString EDITOR_INSPECTOR_GRID_Y =
            new DivaLocalizationManager.DivaLocalisableString("格子 Y", "Grid Y");

        public static readonly LocalisableString EDITOR_INSPECTOR_APPROACH_X =
            new DivaLocalizationManager.DivaLocalisableString("飞入 X", "Approach X");

        public static readonly LocalisableString EDITOR_INSPECTOR_APPROACH_Y =
            new DivaLocalizationManager.DivaLocalisableString("飞入 Y", "Approach Y");

        public static readonly LocalisableString EDITOR_BUTTONS_GROUP =
            new DivaLocalizationManager.DivaLocalisableString("按键", "buttons");

        public static readonly LocalisableString EDITOR_BUTTONS_ARROW =
            new DivaLocalizationManager.DivaLocalisableString("箭头", "Arrows");

        public static readonly LocalisableString EDITOR_BUTTONS_SYMBOL =
            new DivaLocalizationManager.DivaLocalisableString("符号", "Symbols");

        public static readonly LocalisableString EDITOR_BUTTONS_FAMILY_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "按 WASD 选方向，点这里或按 5 切换 WASD 放置箭头还是符号。",
            "WASD picks a direction; click here or press 5 to switch whether WASD places arrows or symbols.");

        public static readonly LocalisableString EDITOR_INSPECTOR_ACTION =
            new DivaLocalizationManager.DivaLocalisableString("按键", "Action");

        public static readonly LocalisableString EDITOR_INSPECTOR_GRID =
            new DivaLocalizationManager.DivaLocalisableString("格子", "Grid");

        public static readonly LocalisableString EDITOR_INSPECTOR_APPROACH =
            new DivaLocalizationManager.DivaLocalisableString("飞入", "Approach");

        public static readonly LocalisableString EDITOR_INSPECTOR_DURATION =
            new DivaLocalizationManager.DivaLocalisableString("时长", "Duration");

        public static readonly LocalisableString EDITOR_EXPORT_DIVA =
            new DivaLocalizationManager.DivaLocalisableString("导出 .diva", "Export .diva");

        public static readonly LocalisableString EDITOR_EVENTS_GROUP =
            new DivaLocalizationManager.DivaLocalisableString("谱面事件", "chart events");

        public static readonly LocalisableString EDITOR_CHANCE_TIME =
            new DivaLocalizationManager.DivaLocalisableString("Chance Time", "Chance Time");

        public static readonly LocalisableString EDITOR_CHANCE_ENABLED =
            new DivaLocalizationManager.DivaLocalisableString("启用 Chance Time", "Enable Chance Time");

        public static readonly LocalisableString EDITOR_CHANCE_START =
            new DivaLocalizationManager.DivaLocalisableString("开始 (ms)", "Start (ms)");

        public static readonly LocalisableString EDITOR_CHANCE_END =
            new DivaLocalizationManager.DivaLocalisableString("结束 (ms)", "End (ms)");

        public static readonly LocalisableString EDITOR_SET_FROM_CLOCK =
            new DivaLocalizationManager.DivaLocalisableString("用当前时间", "Use clock");

        public static readonly LocalisableString EDITOR_BGS_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("BGS", "BGS");

        public static readonly LocalisableString EDITOR_RES_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("RES", "RES");

        public static readonly LocalisableString EDITOR_WAV_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("WAV 文件", "WAV files");

        public static readonly LocalisableString EDITOR_RESOURCE_FILES_HEADER =
            new DivaLocalizationManager.DivaLocalisableString("资源文件", "Resource files");

        public static readonly LocalisableString EDITOR_ADD_AT_CLOCK =
            new DivaLocalizationManager.DivaLocalisableString("在当前时间添加", "Add at clock");

        public static readonly LocalisableString EDITOR_REMOVE_SELECTED =
            new DivaLocalizationManager.DivaLocalisableString("删除选中", "Remove selected");

        public static readonly LocalisableString EDITOR_EVENT_TIME =
            new DivaLocalizationManager.DivaLocalisableString("时间 (ms)", "Time (ms)");

        public static readonly LocalisableString EDITOR_EVENT_SLOT =
            new DivaLocalizationManager.DivaLocalisableString("槽", "Slot");

        public static readonly LocalisableString EDITOR_EVENT_WAV_ID =
            new DivaLocalizationManager.DivaLocalisableString("WAV ID", "WAV ID");

        public static readonly LocalisableString EDITOR_EVENT_RESOURCE_ID =
            new DivaLocalizationManager.DivaLocalisableString("资源 ID", "Resource ID");

        public static readonly LocalisableString EDITOR_EVENT_SEEK =
            new DivaLocalizationManager.DivaLocalisableString("音源偏移 (ms)", "Source seek (ms)");

        public static readonly LocalisableString EDITOR_FILE_ID =
            new DivaLocalizationManager.DivaLocalisableString("ID", "ID");

        public static readonly LocalisableString EDITOR_FILE_PATH =
            new DivaLocalizationManager.DivaLocalisableString("路径", "Path");

        public static readonly LocalisableString EDITOR_ADD_FILE =
            new DivaLocalizationManager.DivaLocalisableString("添加", "Add");

        public static readonly LocalisableString EDITOR_NO_SELECTION =
            new DivaLocalizationManager.DivaLocalisableString("未选中", "None selected");

        public static readonly LocalisableString EDITOR_VIEW_GROUP =
            new DivaLocalizationManager.DivaLocalisableString("视图", "View");

        public static readonly LocalisableString EDITOR_PLAYFIELD_ZOOM =
            new DivaLocalizationManager.DivaLocalisableString("游玩区域缩放", "Playfield zoom");

        public static readonly LocalisableString EDITOR_PLAYFIELD_ZOOM_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "只影响编辑器视图，范围 0.1×–10×。飞入起点常在场地之外，缩小后才够得着。快捷键：鼠标在游玩区域上时按住 Alt 滚轮。",
            "Editor view only, 0.1x-10x. Flight start points sit outside the field, so zoom out to reach them. Shortcut: hold Alt and scroll over the play area.");

        public static string Editor_ExportDivaComplete(string path) =>
            editor_export_diva_complete_template.Format(path);

        public static string Editor_ExportDivaFailed(string message) =>
            editor_export_diva_failed_template.Format(message);

        private static readonly DivaLocalizationManager.DivaLocalisableString editor_export_diva_complete_template =
            new DivaLocalizationManager.DivaLocalisableString("已导出 .diva：{0}", "Exported .diva: {0}");

        private static readonly DivaLocalizationManager.DivaLocalisableString editor_export_diva_failed_template =
            new DivaLocalizationManager.DivaLocalisableString("导出 .diva 失败：{0}", "Failed to export .diva: {0}");

        #endregion

        #region Mods

        public static readonly LocalisableString MOD_KEY1_DESCRIPTION =
            new DivaLocalizationManager.DivaLocalisableString("使用一个按键游玩。", "Play with one button.");

        public static readonly LocalisableString MOD_KEY2_DESCRIPTION =
            new DivaLocalizationManager.DivaLocalisableString("使用两个按键游玩。", "Play with two buttons.");

        public static readonly LocalisableString MOD_KEY3_DESCRIPTION =
            new DivaLocalizationManager.DivaLocalisableString("使用三个按键游玩。", "Play with three buttons.");

        public static readonly LocalisableString MOD_KEY4_DESCRIPTION =
            new DivaLocalizationManager.DivaLocalisableString("使用四个按键游玩。", "Play with four buttons.");

        public static readonly LocalisableString MOD_NO_DOUBLES_DESCRIPTION =
            new DivaLocalizationManager.DivaLocalisableString("同一时间只能按一个键。", "Only one button at a time.");

        #endregion

        #region Library import / sync progress

        public static string Import_ScanningFolders() => import_scanning_folders;

        public static string Import_Packaging(string name) => import_packaging_template.Format(name);

        public static string Import_Importing(string name) => import_importing_template.Format(name);

        public static string Import_NoSongFolders() => import_no_song_folders;

        public static string Import_Imported(int imported, int total) => import_imported_template.Format(imported, total);

        public static string Import_Linking(string name) => import_linking_template.Format(name);

        public static string Import_Linked(int count) => import_linked_template.Format(count);

        public static string Separate_Scanning(int count) => separate_scanning_template.Format(count);

        public static string Separate_Processing(string name) => separate_processing_template.Format(name);

        public static string Separate_Summary(int scanned, int updated, int rewritten, int skipped) =>
            separate_summary_template.Format(scanned, updated, rewritten, skipped);

        private static readonly DivaLocalizationManager.DivaLocalisableString import_scanning_folders =
            new DivaLocalizationManager.DivaLocalisableString("正在扫描 DIVA 歌曲文件夹…", "Scanning DIVA song folders…");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_packaging_template =
            new DivaLocalizationManager.DivaLocalisableString("正在打包 {0}…", "Packaging {0}…");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_importing_template =
            new DivaLocalizationManager.DivaLocalisableString("正在导入 {0}…", "Importing {0}…");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_no_song_folders =
            new DivaLocalizationManager.DivaLocalisableString("未找到 .diva 歌曲文件夹。", "No .diva song folders found.");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_imported_template =
            new DivaLocalizationManager.DivaLocalisableString("已导入 {0}/{1} 个歌曲集。", "Imported {0}/{1} song sets.");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_linking_template =
            new DivaLocalizationManager.DivaLocalisableString("正在链接 {0}…", "Linking {0}…");

        private static readonly DivaLocalizationManager.DivaLocalisableString import_linked_template =
            new DivaLocalizationManager.DivaLocalisableString("已链接 {0} 个外部歌曲集。", "Linked {0} external song sets.");

        private static readonly DivaLocalizationManager.DivaLocalisableString separate_scanning_template =
            new DivaLocalizationManager.DivaLocalisableString("正在扫描 {0} 个歌曲文件夹…", "Scanning {0} song folders…");

        private static readonly DivaLocalizationManager.DivaLocalisableString separate_processing_template =
            new DivaLocalizationManager.DivaLocalisableString("正在处理 {0}…", "Processing {0}…");

        private static readonly DivaLocalizationManager.DivaLocalisableString separate_summary_template =
            new DivaLocalizationManager.DivaLocalisableString("已扫描 {0} 首歌曲；更新 {1}；重写 {2} 张谱面；跳过 {3}。",
                "Scanned {0} songs; updated {1}; rewrote {2} charts; skipped {3}.");

        #endregion
    }
}
