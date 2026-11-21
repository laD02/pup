using System;
using SealCatapult.Core;
using SealCatapult.Progress;
using TMPro;
using UnityEngine;

namespace SealCatapult.UI
{
    /// <summary>Upgrade shop: four cards with current level, effect, price and a Buy button.</summary>
    public class ShopView : MonoBehaviour
    {
        Canvas canvas;
        Action onChanged;
        Transform content;

        public static ShopView Create(Action onChanged)
        {
            var canvas = UiKit.CreateCanvas("Shop", 20);
            var view = canvas.gameObject.AddComponent<ShopView>();
            view.canvas = canvas;
            view.onChanged = onChanged;
            canvas.enabled = false;
            return view;
        }

        public void Open()
        {
            canvas.enabled = true;
            Rebuild();
        }

        void Close()
        {
            canvas.enabled = false;
            if (content != null) Destroy(content.gameObject);   // a disabled canvas still runs its children's animations
            content = null;
        }

        /// <summary>Android Back: closes the shop when it is open. False when there was nothing to close.</summary>
        public bool HandleBack()
        {
            if (!canvas.enabled) return false;
            Close();
            return true;
        }

        /// <param name="animate">False after a purchase: the cards stay put instead of sliding in again.</param>
        void Rebuild(bool animate = true)
        {
            if (content != null) Destroy(content.gameObject);
            var dim = UiKit.Overlay(canvas.transform, "Overlay");
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0.03f, 0.08f, 0.18f, 1f); // full screen, hides the map
            content = dim;
            // A painted screen like every other (it was the one flat navy page): the home painting under a deep veil.
            UiKit.PaintedBackdrop(dim, 0.82f);
            // Corner controls stay clear of notches and rounded corners, like every other screen.
            var overlay = UiKit.SafeRoot(canvas);
            overlay.SetParent(dim, false);
            var data = SaveSystem.Data;

            // Landscape: title top-left, star balance top-right, 2×2 cards, caption bar and Back along the bottom.
            // Layout A top bar, the same as the map's: Back, title, stars to spend.
            UiKit.TopBar(overlay, Loc.T("shop"), Close, Shop.AvailableStars(data).ToString(), out _);

            // Description bar at the bottom, like a caption for the selected upgrade.
            var bar = UiKit.Place(UiKit.Node(overlay, "Description"), UiKit.Bottom, new Vector2(0, 30), new Vector2(1100, 100));
            var barBg = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
            barBg.sprite = Art.Get("ui_pill");
            barBg.type = UnityEngine.UI.Image.Type.Sliced;
            description = UiKit.Text(bar, Loc.T("hint_shop"), 34, UiKit.Center, new Vector2(0, 2), new Vector2(1060, 90), Color.white,
                TextAlignmentOptions.Center, true);

            var upgrades = GameDatabase.Instance.upgrades.upgrades;
            for (int i = 0; i < upgrades.Count; i++)
                BuildRow(overlay, upgrades[i], i < 3 ? new Vector2((i - 1) * RowPitch, 170) : new Vector2((i - 3.5f) * RowPitch, -70), data, i, animate);
        }

        /// <summary>"Ice Spikes · Lv 2/3: Smash through 3 ice blocks": the level the next purchase gives (the top level
        /// once maxed) and what it really does.</summary>
        static string Describe(UpgradeDef def, SaveData data)
        {
            int shown = Mathf.Min(Shop.Level(data, def.id) + 1, UpgradeData.MaxLevel);
            int effect = Shop.Effect(def.id, shown, GameDatabase.Instance.tuning.basePierce);
            string key = "upd_" + def.id;
            if (def.id == UpgradeId.Blaze && effect >= Shop.BlazeAll) key += "_all";
            else if (effect == 1 && Loc.Has(key + "_one")) key += "_one";   // "+1 bounce", not "+1 bounces"
            return Loc.T("shop_line", Loc.T("up_" + def.id), Loc.T("lv", shown, UpgradeData.MaxLevel), Loc.T(key, effect));
        }

        TextMeshProUGUI description;
        // Three cards span ±800: inside the 4:3 canvas (±831), the narrowest screen (600/580 cut the outer cards there).
        const float RowPitch = 540f, RowWidth = 520f;

        /// <summary>One list row: icon · name + level pips · price button. Tapping the row explains it.</summary>
        void BuildRow(Transform parent, UpgradeDef def, Vector2 position, SaveData data, int index, bool animate)
        {
            int level = Shop.Level(data, def.id);
            var row = UiKit.Place(UiKit.Node(parent, "Row " + def.id), UiKit.Center, position, new Vector2(RowWidth, 220));
            var bg = row.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = Art.Get("ui_panel");
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            var select = row.gameObject.AddComponent<UnityEngine.UI.Button>();
            select.transition = UnityEngine.UI.Selectable.Transition.None;
            select.onClick.AddListener(() =>
            {
                AudioManager.Sfx("click");
                description.text = Describe(def, SaveSystem.Data);
            });
            if (animate) row.gameObject.AddComponent<SlideIn>().delay = 0.06f * index;

            UiKit.Image(row, def.icon, new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(150, 150));
            var name = UiKit.Text(row, Loc.T("up_" + def.id), 40, new Vector2(0f, 0.5f), new Vector2(170, 40), new Vector2(240, 70), null,
                TextAlignmentOptions.Left);
            name.enableAutoSizing = true;
            name.fontSizeMin = 24;
            name.fontSizeMax = 40;
            for (int s = 0; s < UpgradeData.MaxLevel; s++)
            {
                // A level not bought yet is a small faint gem: size as well as shade (colour is never the only signal).
                bool got = s < level;
                var pip = UiKit.Image(row, "gem", new Vector2(0f, 0.5f), new Vector2(174 + s * 48 + (got ? 0 : 8), -30), got ? new Vector2(40, 40) : new Vector2(24, 24));
                pip.color = got ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }

            int cost = Shop.NextCost(data, def);
            bool canBuy = Shop.CanBuy(data, def);
            // The price alone ("3 ★"): with the word Buy the label ran into the star in German and French (QA 15/11).
            if (cost < 0)
            {
                // Nothing left to buy: a plain label, not a button (it looked and squashed like one, and it kept a tap
                // on it from reaching the card, so no description showed).
                var max = UiKit.Image(row, "ui_pill", new Vector2(1f, 0.5f), new Vector2(-14, -34), new Vector2(180, 90));
                max.type = UnityEngine.UI.Image.Type.Sliced;
                max.preserveAspect = false;
                UiKit.Text(max.transform, Loc.T("maxed"), 38, UiKit.Center, new Vector2(0, 2), new Vector2(160, 80), Color.white,
                    TextAlignmentOptions.Center, true);
                return;
            }
            var buy = UiKit.Button(row, cost.ToString(), canBuy ? ButtonStyle.Gold : ButtonStyle.Teal, new Vector2(1f, 0.5f), new Vector2(-14, -34),
                new Vector2(180, 90), () => Buy(def));
            UiKit.Image(buy.transform, "star_full", new Vector2(1f, 0.5f), new Vector2(-22, 4), new Vector2(44, 44));
            var text = buy.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            var rt = text.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - 26f, rt.anchoredPosition.y);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x - 52f, rt.sizeDelta.y);
            if (canBuy) return;
            // Too dear: greyed AND a lock (shape, not only colour). It stays tappable so the tap can say what is missing.
            buy.GetComponent<UnityEngine.UI.Image>().color = new Color(0.75f, 0.75f, 0.8f);
            text.alpha = 0.6f;
            UiKit.Image(buy.transform, "glyph_lock", UiKit.TopLeft, new Vector2(-10, 12), new Vector2(44, 44));
        }

        void Buy(UpgradeDef def)
        {
            var data = SaveSystem.Data;
            if (!Shop.TryBuy(data, def))
            {
                int missing = Shop.NextCost(data, def) - Shop.AvailableStars(data);
                if (missing > 0) description.text = Loc.T("need_stars", missing);
                return;
            }
            SaveSystem.Save();
            AudioManager.Sfx("star", 0f);
            onChanged?.Invoke();
            Rebuild(animate: false);
            description.text = Describe(def, data);   // the card just bought stays explained (it fell back to the hint)
        }
    }
}
