using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Echo0317.UI
{
    /// <summary>
    /// Uses the original generated PNG art pack under StreamingAssets/Investigation.
    /// Every visual also has a native fallback, so a missing OPTIONAL art pack
    /// never prevents the player from investigating scenes in an internal build.
    /// </summary>
    public sealed class EchoInvestigationArt
    {
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        public Sprite Get(string name)
        {
            if (sprites.TryGetValue(name, out var sprite)) return sprite;
            Sprite loaded = null;
            try
            {
                // Never permit arbitrary filesystem paths from data-driven icon names.
                if (string.IsNullOrEmpty(name)) return null;
                foreach (char character in name)
                    if (!char.IsLetterOrDigit(character) && character != '-')
                        throw new InvalidDataException("Invalid sprite name");
                var path = Path.Combine(Application.streamingAssetsPath, "Investigation", name + ".png");
                if (File.Exists(path))
                {
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(File.ReadAllBytes(path), true))
                    {
                        texture.name = "Investigation-" + name;
                        loaded = Sprite.Create(texture,
                            new Rect(0f, 0f, texture.width, texture.height),
                            new Vector2(.5f, .5f), 100f);
                    }
                    else UnityEngine.Object.Destroy(texture);
                }
            }
            catch (Exception e) { Debug.LogWarning("[Echo0317] Optional investigation art: " + e.Message); }
            if (loaded == null)
            {
                if (name.StartsWith("hotspot-", StringComparison.Ordinal))
                    loaded = DrawFrame(name);
                else if (name == "inspect-result" || name.StartsWith("notice-", StringComparison.Ordinal))
                    loaded = EchoDossierArtwork.Card;
                // No need to show arbitrary misleading icon if its actual file is missing.
            }
            sprites[name] = loaded;
            return loaded;
        }

        static Sprite DrawFrame(string name)
        {
            const int width = 256, height = 144;
            var pixels = new Color32[width * height];
            var accent = name == "hotspot-focus"
                ? new Color32(237,195,132,240)
                : name == "hotspot-complete" ? new Color32(153,206,173,200)
                : new Color32(141,171,189,180);
            for (int y=0; y<height; y++)
                for (int x=0; x<width; x++)
                {
                    bool nearCorner = (x < 35 || x > width-36) &&
                        (y < 27 || y > height-28);
                    bool border = (x < 3 || x > width-4 || y < 3 || y > height-4);
                    bool bracket = nearCorner &&
                        ((x > 8 && x < 12 && (y < 24 || y > height-25)) ||
                         (y > 8 && y < 12 && (x < 33 || x > width-34)) ||
                         (y > height-13 && y < height-9 && (x < 33 || x > width-34)));
                    pixels[y*width+x] = (border || bracket) ? accent :
                        new Color32(0,0,0,0);
                }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Fallback-" + name, wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0,0,width,height),
                new Vector2(.5f,.5f),100f);
        }
    }
}
