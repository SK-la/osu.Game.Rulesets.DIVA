// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osu.Game.Rulesets.Diva.Objects.Drawables;

namespace osu.Game.Rulesets.Diva.Edit
{
    /// <summary>
    ///     Texture lookup for editor overlays. Matches gameplay note sprites without sharing drawable types.
    /// </summary>
    public static class DivaEditorNoteVisual
    {
        public static bool IsDirectionAction(DivaAction action) =>
            action is DivaAction.Left or DivaAction.Right or DivaAction.Up or DivaAction.Down;

        public static DivaAction TextureStem(DivaAction action) => action switch
        {
            DivaAction.Right => DivaAction.Circle,
            DivaAction.Down => DivaAction.Cross,
            DivaAction.Up => DivaAction.Triangle,
            DivaAction.Left => DivaAction.Square,
            _ => action
        };

        public static string TexturePrefix(DivaAction action)
            => IsDirectionAction(action) ? "Doubles/" : string.Empty;

        public static string StatTextureName(DivaAction action)
            => $"{TexturePrefix(action)}{TextureStem(action)}Stat";

        public static float NoteSize => DrawableDivaHitObject.BASE_SIZE;

        public static int ToUnitIndex(DivaAction action) => DivaActionEncoding.ToUnitIndex(action);
    }
}
