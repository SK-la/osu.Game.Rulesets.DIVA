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

        public static readonly LocalisableString SETTINGS_APPROACH_PREEMPT_SCALE =
            new DivaLocalizationManager.DivaLocalisableString("接近预判缩放", "Approach preempt scale");

        public static readonly LocalisableString SETTINGS_APPROACH_PREEMPT_SCALE_TOOLTIP = new DivaLocalizationManager.DivaLocalisableString(
            "ProjectDIVA note_standing×BPM 的倍率（1.0 = PD 默认）。越小则音符出现越晚。",
            "Multiplier on ProjectDIVA note_standing×BPM (1.0 = PD default). Lower = notes appear later.");

        public static readonly LocalisableString SETTINGS_HIT_EXPLOSION_ALPHA =
            new DivaLocalizationManager.DivaLocalisableString("打击爆发透明度", "Hit Explosion alpha");

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
