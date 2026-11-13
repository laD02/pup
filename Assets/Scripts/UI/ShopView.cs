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

        void Rebuild()
        {
            if (content != null) Destroy(content.gameObject);
            var overlay = UiKit.Overlay(canvas.transform, "Overlay");
            overlay.GetComponent<UnityEngine.UI.Image>().color = new Color(0.03f, 0.08f, 0.18f, 1f); // full screen, hides the map
            content = overlay;
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
                BuildRow(overlay, upgrades[i], i < 3 ? new Vector2((i - 1) * 600, 170) : new Vector2((i - 3.5f) * 600, -70), data, i);
        }

        TextMeshProUGUI description;

        /// <summary>One list row: icon · name + level pips · price button. Tapping the row explains it.</summary>
        void BuildRow(Transform parent, UpgradeDef def, Vector2 position, SaveData data, int index)
        {
            int level = Shop.Level(data, def.id);
            int effect = Mathf.Max(1, Mathf.Min(level + 1, UpgradeData.MaxLevel));
            var row = UiKit.Place(UiKit.Node(parent, "Row " + def.id), UiKit.Center, position, new Vector2(580, 220));
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
            string label = cost < 0 ? Loc.T("maxed") : $"{Loc.T("buy")} {cost}";
            var buy = UiKit.Button(row, label, canBuy ? ButtonStyle.Gold : ButtonStyle.Teal, new Vector2(1f, 0.5f), new Vector2(-14, -34),
                new Vector2(180, 90), () => Buy(def));
            buy.interactable = canBuy;
            if (!canBuy) buy.GetComponent<UnityEngine.UI.Image>().color = new Color(0.75f, 0.75f, 0.8f);
            if (cost >= 0)
            {
                // Keep the price clear of the star: the label shrinks into the space left of it (two-digit prices
                // were hidden under the star, QA 12/11).
                UiKit.Image(buy.transform, "star_full", new Vector2(1f, 0.5f), new Vector2(-10, 4), new Vector2(40, 40));
                var text = buy.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                var rt = text.rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - 22f, rt.anchoredPosition.y);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x - 44f, rt.sizeDelta.y);
            }
        }

        void Buy(UpgradeDef def)
        {
            if (!Shop.TryBuy(SaveSystem.Data, def)) return;
            SaveSystem.Save();
            AudioManager.Sfx("star", 0f);
            onChanged?.Invoke();
            Rebuild();
        }
    }
}
