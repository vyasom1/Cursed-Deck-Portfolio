# Cursed Deck Gothic UI Pack

Separate transparent PNG sprites for the gothic Cursed Deck interface. No gameplay scripts are included or modified.

## Contents
- `UI/TitlePlaque.png` — blank title plaque; place TextMeshPro text over the center.
- `Backgrounds/GothicBackground.png` — full-bleed dark cathedral background plate, with a low-detail center for overlaid UI.
- `Panels/InfoPanel.png` — reusable dark HUD panel.
- `Buttons/PrimaryButton.png` — blank ruby primary action button; overlay UI text.
- `Buttons/SecondaryButton.png` — blank neutral secondary button; overlay UI text.
- `Cards/CardBack.png` — portrait card back.
- `Cards/CardFrame.png` — portrait frame overlay.
- `Icons/Suit_Spade.png`, `Suit_Heart.png`, `Suit_Diamond.png`, `Suit_Club.png` — suit emblems.

## Unity import settings
For each PNG, set **Texture Type** to `Sprite (2D and UI)`, **Sprite Mode** to `Single`, and **Alpha Is Transparency** on. Use **Filter Mode** `Bilinear`; keep compression off or at high quality for the ornate edges. A Max Size of `2048` is sufficient for these source dimensions.

Use `Image` components on a Canvas. Overlay text as separate TextMeshPro UI objects so labels remain crisp and localizable. The panel and buttons have transparent padding around their silhouettes. For ornate edges, begin with `Image Type: Simple`; test 9-slicing only on the flatter panel/button edges because the decorative center jewels can stretch.

Unity will create `.meta` files when the folder is imported. Copy this pack folder into `Assets/` to bring it into the project.


## Additional sprites from the UI reference sheet
- `Panels/StatusBar.png`, `ScorePanel.png`, `ResourcePanel.png`, `InstructionPanel.png` — individual HUD and guidance panels.
- `Buttons/PrimaryButton_Hover.png`, `PrimaryButton_Disabled.png` — button states.
- `Icons/GoldCoinStack.png`, `Relic.png`, `Settings.png`, `Candle.png`, `Skull.png`, `CardStack.png` — utility icons.
- `Borders/CornerOrnament.png`, `HorizontalDivider.png`, `VerticalDivider.png`, `RubySeparator.png` — decorative separators and corner trim.

The reference's small gameplay screenshot is a composition preview rather than a reusable sprite; its individual reusable pieces are supplied in these folders.
