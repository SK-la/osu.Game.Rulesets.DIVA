// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Diva.Beatmaps;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Localization;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Diva.Edit
{
    public partial class DivaChartEventsToolbox : EditorToolboxGroup
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        private readonly BindableBool chanceEnabled = new BindableBool();
        private readonly BindableDouble chanceStart = new BindableDouble
        {
            MinValue = -600000,
            MaxValue = 600000,
            Precision = 1
        };
        private readonly BindableDouble chanceEnd = new BindableDouble
        {
            MinValue = -600000,
            MaxValue = 600000,
            Precision = 1
        };

        private FillFlowContainer bgsList = null!;
        private FillFlowContainer resList = null!;
        private FillFlowContainer wavList = null!;
        private FillFlowContainer resourceFileList = null!;
        private FillFlowContainer bgsEditor = null!;
        private FillFlowContainer resEditor = null!;

        private readonly BindableDouble bgsTime = new BindableDouble { MinValue = -600000, MaxValue = 600000, Precision = 1 };
        private readonly BindableDouble bgsSlot = new BindableDouble { MinValue = 0, MaxValue = 9, Precision = 1 };
        private readonly BindableDouble bgsWavId = new BindableDouble { MinValue = 0, MaxValue = 99, Precision = 1 };
        private readonly BindableDouble bgsSeek = new BindableDouble { MinValue = 0, MaxValue = 600000, Precision = 1 };
        private readonly BindableDouble resTime = new BindableDouble { MinValue = -600000, MaxValue = 600000, Precision = 1 };
        private readonly BindableDouble resId = new BindableDouble { MinValue = 0, MaxValue = 99, Precision = 1 };
        private readonly BindableDouble resSeek = new BindableDouble { MinValue = 0, MaxValue = 600000, Precision = 1 };

        private int selectedBgs = -1;
        private int selectedRes = -1;
        private bool applyingFromModel;

        public DivaChartEventsToolbox()
            : base("events")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(6),
                Children =
                [
                    new OsuSpriteText { Text = DivaStrings.EDITOR_CHANCE_TIME, Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold) },
                    new FormCheckBox { Caption = DivaStrings.EDITOR_CHANCE_ENABLED, Current = chanceEnabled },
                    createSlider(DivaStrings.EDITOR_CHANCE_START, chanceStart),
                    createSlider(DivaStrings.EDITOR_CHANCE_END, chanceEnd),
                    createButton(DivaStrings.EDITOR_SET_FROM_CLOCK, setChanceFromClock),
                    new OsuSpriteText { Text = DivaStrings.EDITOR_BGS_HEADER, Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold), Margin = new MarginPadding { Top = 6 } },
                    bgsList = createList(),
                    bgsEditor = createBgsEditor(),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(4),
                        Children =
                        [
                            createButton(DivaStrings.EDITOR_ADD_AT_CLOCK, addBgsAtClock),
                            createButton(DivaStrings.EDITOR_REMOVE_SELECTED, removeSelectedBgs)
                        ]
                    },
                    new OsuSpriteText { Text = DivaStrings.EDITOR_RES_HEADER, Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold), Margin = new MarginPadding { Top = 6 } },
                    resList = createList(),
                    resEditor = createResEditor(),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(4),
                        Children =
                        [
                            createButton(DivaStrings.EDITOR_ADD_AT_CLOCK, addResAtClock),
                            createButton(DivaStrings.EDITOR_REMOVE_SELECTED, removeSelectedRes)
                        ]
                    },
                    new OsuSpriteText { Text = DivaStrings.EDITOR_WAV_HEADER, Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold), Margin = new MarginPadding { Top = 6 } },
                    wavList = createList(),
                    createButton(DivaStrings.EDITOR_ADD_FILE, () => addFile(true)),
                    new OsuSpriteText { Text = DivaStrings.EDITOR_RESOURCE_FILES_HEADER, Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold), Margin = new MarginPadding { Top = 6 } },
                    resourceFileList = createList(),
                    createButton(DivaStrings.EDITOR_ADD_FILE, () => addFile(false))
                ]
            };

            chanceEnabled.BindValueChanged(_ => applyChance());
            chanceStart.BindValueChanged(_ => applyChance());
            chanceEnd.BindValueChanged(_ => applyChance());
            bgsTime.BindValueChanged(_ => applySelectedBgs());
            bgsSlot.BindValueChanged(_ => applySelectedBgs());
            bgsWavId.BindValueChanged(_ => applySelectedBgs());
            bgsSeek.BindValueChanged(_ => applySelectedBgs());
            resTime.BindValueChanged(_ => applySelectedRes());
            resId.BindValueChanged(_ => applySelectedRes());
            resSeek.BindValueChanged(_ => applySelectedRes());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            reloadFromBeatmap();
        }

        private void reloadFromBeatmap()
        {
            applyingFromModel = true;

            (double start, double end, bool enabled) = readChance();
            chanceEnabled.Value = enabled;
            chanceStart.Value = start;
            chanceEnd.Value = Math.Max(chanceStart.Value, end);

            DivaChartEvents events = DivaBeatmap.EventsOf(editorBeatmap) ?? new DivaChartEvents();
            rebuildBgsList(events);
            rebuildResList(events);
            rebuildFileList(wavList, events.WavFiles, true);
            rebuildFileList(resourceFileList, events.ResourceFiles, false);
            syncBgsEditor(events);
            syncResEditor(events);

            applyingFromModel = false;
        }

        private (double Start, double End, bool Enabled) readChance()
        {
            var kiai = editorBeatmap.ControlPointInfo.EffectPoints.Where(p => p.KiaiMode).OrderBy(p => p.Time).ToArray();
            if (kiai.Length == 0)
                return (editorClock.CurrentTime, editorClock.CurrentTime + 2000, false);

            double start = kiai[0].Time;
            var off = editorBeatmap.ControlPointInfo.EffectPoints.Where(p => !p.KiaiMode && p.Time > start).OrderBy(p => p.Time).FirstOrDefault();
            double end = off?.Time ?? editorBeatmap.HitObjects.LastOrDefault()?.GetEndTime() ?? start + 2000;
            return (start, end, true);
        }

        private void setChanceFromClock()
        {
            applyingFromModel = true;
            chanceEnabled.Value = true;
            chanceStart.Value = Math.Round(editorClock.CurrentTime);
            if (chanceEnd.Value <= chanceStart.Value)
                chanceEnd.Value = chanceStart.Value + 2000;
            applyingFromModel = false;
            applyChance();
        }

        private void applyChance()
        {
            if (applyingFromModel)
                return;

            double start = chanceStart.Value;
            double end = Math.Max(start + 1, chanceEnd.Value);

            editorBeatmap.BeginChange();
            clearEffectPoints();

            if (chanceEnabled.Value)
            {
                editorBeatmap.ControlPointInfo.Add(start, new EffectControlPoint { KiaiMode = true });
                editorBeatmap.ControlPointInfo.Add(end, new EffectControlPoint { KiaiMode = false });
            }

            editorBeatmap.EndChange();
        }

        private void clearEffectPoints()
        {
            foreach (EffectControlPoint point in editorBeatmap.ControlPointInfo.EffectPoints.ToArray())
            {
                ControlPointGroup? group = editorBeatmap.ControlPointInfo.GroupAt(point.Time);
                group?.Remove(point);
                if (group != null && group.ControlPoints.Count == 0)
                    editorBeatmap.ControlPointInfo.RemoveGroup(group);
            }
        }

        private void addBgsAtClock()
        {
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            events.BgmEvents.Add(new DivaBgmEvent
            {
                Sequence = events.BgmEvents.Count,
                TimeMs = Math.Round(editorClock.CurrentTime),
                Slot = 0,
                WavId = events.WavFiles.Keys.DefaultIfEmpty(0).Min()
            });
            selectedBgs = events.BgmEvents.Count - 1;
            reloadFromBeatmap();
        }

        private void removeSelectedBgs()
        {
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            if (selectedBgs < 0 || selectedBgs >= events.BgmEvents.Count)
                return;

            events.BgmEvents.RemoveAt(selectedBgs);
            selectedBgs = Math.Min(selectedBgs, events.BgmEvents.Count - 1);
            reloadFromBeatmap();
        }

        private void addResAtClock()
        {
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            events.ResourceEvents.Add(new DivaResourceEvent
            {
                Sequence = events.ResourceEvents.Count,
                TimeMs = Math.Round(editorClock.CurrentTime),
                ResourceId = events.ResourceFiles.Keys.DefaultIfEmpty(0).Min()
            });
            selectedRes = events.ResourceEvents.Count - 1;
            reloadFromBeatmap();
        }

        private void removeSelectedRes()
        {
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            if (selectedRes < 0 || selectedRes >= events.ResourceEvents.Count)
                return;

            events.ResourceEvents.RemoveAt(selectedRes);
            selectedRes = Math.Min(selectedRes, events.ResourceEvents.Count - 1);
            reloadFromBeatmap();
        }

        private void applySelectedBgs()
        {
            if (applyingFromModel)
                return;

            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            if (selectedBgs < 0 || selectedBgs >= events.BgmEvents.Count)
                return;

            DivaBgmEvent e = events.BgmEvents[selectedBgs];
            e.TimeMs = bgsTime.Value;
            e.Slot = (int)bgsSlot.Value;
            e.WavId = (int)bgsWavId.Value;
            e.DeclaredSourceOffsetMs = bgsSeek.Value > 0 ? bgsSeek.Value : null;
            rebuildBgsList(events);
        }

        private void applySelectedRes()
        {
            if (applyingFromModel)
                return;

            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            if (selectedRes < 0 || selectedRes >= events.ResourceEvents.Count)
                return;

            DivaResourceEvent e = events.ResourceEvents[selectedRes];
            e.TimeMs = resTime.Value;
            e.ResourceId = (int)resId.Value;
            e.DeclaredSourceOffsetMs = resSeek.Value > 0 ? resSeek.Value : null;
            rebuildResList(events);
        }

        private void addFile(bool wav)
        {
            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
            Dictionary<int, string> map = wav ? events.WavFiles : events.ResourceFiles;
            int id = 0;
            while (map.ContainsKey(id))
                id++;
            map[id] = wav ? "audio.ogg" : "video.mp4";
            reloadFromBeatmap();
        }

        private void rebuildBgsList(DivaChartEvents events)
        {
            bgsList.Clear();
            for (int i = 0; i < events.BgmEvents.Count; i++)
            {
                int index = i;
                DivaBgmEvent e = events.BgmEvents[i];
                bgsList.Add(createRowButton(
                    $"{e.TimeMs:0}  s{e.Slot}  wav{e.WavId}",
                    () =>
                    {
                        selectedBgs = index;
                        reloadFromBeatmap();
                    },
                    index == selectedBgs));
            }

            if (events.BgmEvents.Count == 0)
                bgsList.Add(new OsuSpriteText { Text = DivaStrings.EDITOR_NO_SELECTION, Font = OsuFont.Default.With(size: 12) });
        }

        private void rebuildResList(DivaChartEvents events)
        {
            resList.Clear();
            for (int i = 0; i < events.ResourceEvents.Count; i++)
            {
                int index = i;
                DivaResourceEvent e = events.ResourceEvents[i];
                resList.Add(createRowButton(
                    $"{e.TimeMs:0}  id{e.ResourceId}",
                    () =>
                    {
                        selectedRes = index;
                        reloadFromBeatmap();
                    },
                    index == selectedRes));
            }

            if (events.ResourceEvents.Count == 0)
                resList.Add(new OsuSpriteText { Text = DivaStrings.EDITOR_NO_SELECTION, Font = OsuFont.Default.With(size: 12) });
        }

        private void rebuildFileList(FillFlowContainer list, Dictionary<int, string> files, bool wav)
        {
            list.Clear();
            foreach ((int id, string path) in files.OrderBy(p => p.Key))
            {
                int capturedId = id;
                var pathBindable = new Bindable<string>(path);
                pathBindable.BindValueChanged(v =>
                {
                    DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
                    Dictionary<int, string> map = wav ? events.WavFiles : events.ResourceFiles;
                    map[capturedId] = v.NewValue;
                });

                list.Add(new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(2),
                    Children =
                    [
                        new OsuSpriteText
                        {
                            Text = string.Create(CultureInfo.InvariantCulture, $"{DivaStrings.EDITOR_FILE_ID}: {capturedId}"),
                            Font = OsuFont.Default.With(size: 12)
                        },
                        new FormTextBox
                        {
                            Caption = DivaStrings.EDITOR_FILE_PATH,
                            Current = pathBindable
                        },
                        createButton(DivaStrings.EDITOR_REMOVE_SELECTED, () =>
                        {
                            DivaChartEvents events = DivaBeatmap.GetOrCreateEvents(editorBeatmap);
                            (wav ? events.WavFiles : events.ResourceFiles).Remove(capturedId);
                            reloadFromBeatmap();
                        })
                    ]
                });
            }
        }

        private void syncBgsEditor(DivaChartEvents events)
        {
            bool valid = selectedBgs >= 0 && selectedBgs < events.BgmEvents.Count;
            bgsEditor.Alpha = valid ? 1 : 0;
            if (!valid)
                return;

            DivaBgmEvent e = events.BgmEvents[selectedBgs];
            bgsTime.Value = e.TimeMs;
            bgsSlot.Value = e.Slot;
            bgsWavId.Value = e.WavId;
            bgsSeek.Value = e.DeclaredSourceOffsetMs ?? 0;
        }

        private void syncResEditor(DivaChartEvents events)
        {
            bool valid = selectedRes >= 0 && selectedRes < events.ResourceEvents.Count;
            resEditor.Alpha = valid ? 1 : 0;
            if (!valid)
                return;

            DivaResourceEvent e = events.ResourceEvents[selectedRes];
            resTime.Value = e.TimeMs;
            resId.Value = e.ResourceId;
            resSeek.Value = e.DeclaredSourceOffsetMs ?? 0;
        }

        private FillFlowContainer createBgsEditor() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(4),
            Children =
            [
                createSlider(DivaStrings.EDITOR_EVENT_TIME, bgsTime),
                createSlider(DivaStrings.EDITOR_EVENT_SLOT, bgsSlot),
                createSlider(DivaStrings.EDITOR_EVENT_WAV_ID, bgsWavId),
                createSlider(DivaStrings.EDITOR_EVENT_SEEK, bgsSeek)
            ]
        };

        private FillFlowContainer createResEditor() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(4),
            Children =
            [
                createSlider(DivaStrings.EDITOR_EVENT_TIME, resTime),
                createSlider(DivaStrings.EDITOR_EVENT_RESOURCE_ID, resId),
                createSlider(DivaStrings.EDITOR_EVENT_SEEK, resSeek)
            ]
        };

        private static FillFlowContainer createList() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(3)
        };

        private static FormSliderBar<double> createSlider(LocalisableString caption, BindableDouble current) => new FormSliderBar<double>
        {
            Caption = caption,
            Current = current,
            TransferValueOnCommit = true,
            KeyboardStep = 1
        };

        private static RoundedButton createButton(LocalisableString text, Action action) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 28,
            Text = text,
            Action = action
        };

        private static RoundedButton createRowButton(string text, Action action, bool selected) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 24,
            Text = selected ? $"• {text}" : text,
            Action = action
        };
    }
}
