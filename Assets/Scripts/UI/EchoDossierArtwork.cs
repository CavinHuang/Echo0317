using UnityEngine;

namespace Echo0317.UI
{
    /// <summary>
    /// Original, deterministic in-engine dossier card artwork. The playable
    /// UI ships with its own textured surfaces; no stock textures, external
    /// downloads or extra Unity packages are needed for the internal Alpha.
    /// </summary>
    public static class EchoDossierArtwork
    {
        static Sprite card, slot;
        public static Sprite Card => card != null ? card : (card = Build(false));
        public static Sprite Slot => slot != null ? slot : (slot = Build(true));

        static Sprite Build(bool slotStyle)
        {
            const int width = 320, height = 128;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = slotStyle ? "Echo-EmptyEvidenceSlot" : "Echo-EvidenceDossier",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int noise = ((x * 73 + y * 151 + (x * y) * 19) ^ (x * 29 + y * 11)) & 3;
                    int baseRed = slotStyle ? 18 : 27;
                    int baseGreen = slotStyle ? 32 : 41;
                    int baseBlue = slotStyle ? 44 : 55;
                    bool topRule = y > height - 18;
                    bool footerRule = y < 15;
                    if (topRule || footerRule) { baseRed -= 4; baseGreen -= 5; baseBlue -= 5; }
                    bool grid = (x % 36 == 0 || y % 29 == 0) && x > 8 && y > 8;
                    if (grid) { baseRed += 5; baseGreen += 6; baseBlue += 5; }
                    // Ink-printed edge marks and a small circular security stamp
                    // are deliberately decorative; they never encode clue identity.
                    bool edge = x < 5 && y > 4 && y < height - 4;
                    int dx = x - 282, dy = y - 40;
                    int radiusSquared = dx * dx + dy * dy;
                    bool stamp = !slotStyle && radiusSquared > 17 * 17 && radiusSquared < 19 * 19;
                    bool dash = slotStyle &&
                        ((x >= 4 && x <= 6 && (y / 9) % 2 == 0) ||
                         (x >= width - 7 && x <= width - 5 && (y / 9) % 2 == 0) ||
                         (y >= 4 && y <= 6 && (x / 10) % 2 == 0) ||
                         (y >= height - 7 && y <= height - 5 && (x / 10) % 2 == 0));
                    if (edge || stamp || dash)
                        pixels[y * width + x] = new Color32(173, 141, 93, 255);
                    else
                        pixels[y * width + x] = new Color32(
                            (byte)(baseRed + noise), (byte)(baseGreen + noise),
                            (byte)(baseBlue + noise), 255);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            var sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = tex.name + "-Sprite";
            return sprite;
        }
    }
}
