// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Diva.Beatmaps.DivaFormat;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Diva.Graphics
{
    /// <summary>
    ///     Helpers for remaining ProjectDIVA particle / colour assets (note/press atlases removed).
    /// </summary>
    public static class DivaProjectDivaAtlas
    {
        public const string PARTICLE_STAR = "ProjectDiva/particle_star";
        public const string PARTICLE_5STAR = "ProjectDiva/particle_5star";
        public const string PARTICLE_STAR2 = "ProjectDiva/particle_star2";
        public const string BLOCK = "ProjectDiva/block";
        public const string FLAME = "ProjectDiva/flame";

        private static readonly Color4[] unit_colors =
        [
            new Color4(126 / 255f, 240 / 255f, 142 / 255f, 1f), // Circle
            new Color4(255 / 255f, 141 / 255f, 166 / 255f, 1f), // Square
            new Color4(96 / 255f, 203 / 255f, 255 / 255f, 1f), // Cross
            new Color4(234 / 255f, 51 / 255f, 255 / 255f, 1f), // Triangle
            new Color4(126 / 255f, 240 / 255f, 142 / 255f, 1f), // Right
            new Color4(255 / 255f, 141 / 255f, 166 / 255f, 1f), // Left
            new Color4(96 / 255f, 203 / 255f, 255 / 255f, 1f), // Down
            new Color4(234 / 255f, 51 / 255f, 255 / 255f, 1f), // Up
        ];

        public static Color4 GetUnitColor(DivaAction action)
        {
            int index = DivaActionEncoding.ToUnitIndex(action);
            return unit_colors[index];
        }

        public static Texture? GetFlame(TextureStore textures) => textures.Get(FLAME);

        public static Texture? GetParticleStar(TextureStore textures) => textures.Get(PARTICLE_STAR);

        public static Texture? GetParticle5Star(TextureStore textures) => textures.Get(PARTICLE_5STAR);

        public static Texture? GetBlock(TextureStore textures) => textures.Get(BLOCK);
    }
}
