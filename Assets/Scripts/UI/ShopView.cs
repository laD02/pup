using System;
using PufferLaunch.Core;
using PufferLaunch.Progress;
using TMPro;
using UnityEngine;

namespace PufferLaunch.UI
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

        void Close() => canvas.enabled = false;

        /// <summary>Android Back: closes the shop when it is open. False when there was nothing to close.</summary>
        public bool HandleBack()
        {
            if (!canvas.enabled) return false;
            Close();
            return true;
        }

        void Rebuild()
        {
            if (content != null) Destroy(content.gameObject);
            var dim = UiKit.Overlay(canvas.transform, "Overlay");
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0.03f, 0.08f, 0.18f, 1f); // full screen, hides the map
            content = dim;
            // Corner controls stay clear of notches and rounded corners, like every other screen.
            var overlay = UiKit.SafeRoot(canvas);
            overlay.SetParent(dim, false);
            var data = SaveSystem.Data;

            // Landscape: title top-left, star balance top-right, 2×2 cards, caption bar and Back along the bottom.
            UiKit.Text(overlay, Loc.T("shop"), 72, UiKit.TopLeft, new Vector2(60, -30), new Vector2(700, 100), Color.white,
                TextAlignmentOptions.Left, true);
            UiKit.Image(overlay, "star_full", UiKit.TopRight, new Vector2(-180, -40), new Vector2(70, 70));
            UiKit.Text(overlay, Shop.AvailableStars(data).ToString(), 60, UiKit.TopRight, new Vector2(-40, -42), new Vector2(130, 70),
                Color.white, TextAlignmentOptions.Left, true);
            UiKit.Button(overlay, Loc.T("back"), ButtonStyle.Teal, UiKit.BottomLeft, new Vector2(40, 30), new Vector2(300, 100),
                Close, "glyph_back");

            // Description bar at the bottom, like a caption for the selected upgrade.
            var bar = UiKit.Place(UiKit.Node(overlay, "Description"), UiKit.Bottom, new Vector2(80, 30), new Vector2(1100, 100));
            var barBg = bar.gameObject.AddComponent<UnityEngine.UI.Image>();
            barBg.sprite = Art.Get("ui_pill");
            barBg.type = UnityEngine.UI.Image.Type.Sliced;
            description = UiKit.Text(bar, Loc.T("hint_shop"), 34, UiKit.Center, new Vector2(0, 2), new Vector2(1060, 90), Color.white,
                TextAlignmentOptions.Center, true);

            var upgrades = GameDatabase.Instance.upgrades.upgrades;
            for (int i = 0; i < upgrades.Count; i++)
                BuildRow(overlay, upgrades[i], i < 3 ? new Vector2((i - 1) * RowPitch, 170) : new Vector2((i - 3.5f) * RowPitch, -70), data, i);
        }

        TextMeshProUGUI description;
        // Three cards span ±800: inside the 4:3 canvas (±831), the narrowest screen (600/580 cut the outer cards there).
        const float RowPitch = 540f, RowWidth = 520f;

        /// <summary>One list row: icon · name + level pips · price button. Tapping the row explains it.</summary>
        void BuildRow(Transform parent, UpgradeDef def, Vector2 position, SaveData data, int index)
        {
            int level = Shop.Level(data, def.id);
            int effect = Mathf.Max(1, Mathf.Min(level + 1, UpgradeData.MaxLevel));
            var row = UiKit.Place(UiKit.Node(parent, "Row " + def.id), UiKit.Center, position, new Vector2(RowWidth, 220));
            var bg = row.gameObject.AddComponent<UnityEngine.UI.Image>();
            bg.sprite = Art.Get("ui_panel");
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            var select = row.gameObject.AddComponent<UnityEngine.UI.Button>();
            select.transition = UnityEngine.UI.Selectable.Transition.None;
            select.onClick.AddListener(() =>
            {
                AudioManager.Sfx("click");
                description.text = $"{Loc.T("up_" + def.id)}: {Loc.T("upd_" + def.id, effect)}";
            });
            row.gameObject.AddComponent<SlideIn>().delay = 0.06f * index;

            UiKit.Image(row, def.icon, new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(150, 150));
            var name = UiKit.Text(row, Loc.T("up_" + def.id), 40, new Vector2(0f, 0.5f), new Vector2(170, 40), new Vector2(240, 70), null,
                TextAlignmentOptions.Left);
            name.enableAutoSizing = true;
            name.fontSizeMin = 24;
            name.fontSizeMax = 40;
            for (int s = 0; s < UpgradeData.MaxLevel; s++)
            {
                var pip = UiKit.Image(row, "gem", new Vector2(0f, 0.5f), new Vector2(174 + s * 48, -30), new Vector2(40, 40));
                pip.color = s < level ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            }

            int cost = Shop.NextCost(data, def);
            bool canBuy = Shop.CanBuy(data, def);
            // The price alone ("3 ★"): with the word Buy the label ran into the star in German and French (QA 15/11).
            string label = cost < 0 ? Loc.T("maxed") : cost.ToString();
            var buy = UiKit.Button(row, label, canBuy ? ButtonStyle.Gold : ButtonStyle.Teal, new Vector2(1f, 0.5f), new Vector2(-14, -34),
                new Vector2(180, 90), () => Buy(def));
            if (cost < 0)
            {
                buy.interactable = false;
                return;
            }
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
            Rebuild();
        }
    }
}
