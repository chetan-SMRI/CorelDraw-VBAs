# SMRI Panel Maker for Adobe Photoshop

The `SMRI_PhotoshopPanelMaker_RawCode.jsx` file is a directly runnable Photoshop script. It requires no license, installer, compilation, Illustrator, or third-party extension.

## What it does

- Uses the entire open Photoshop canvas as the source artwork.
- Calculates efficient vertical or horizontal panels from one or more available media widths.
- Supports overlap in inches and five reusable media-width presets.
- Duplicates the original Photoshop document for every panel and crops each duplicate to its panel area.
- Preserves the original document and its layers.
- Adds production margin only on the sides used by the label and joining markers: top/bottom for vertical cuts, left/right for horizontal cuts.
- Adds visible production text above every panel.
- Optionally adds black Corel-style joining markers: incoming seams sit on the first panel edge, outgoing seams sit one overlap-length inside the opposite edge, and every T-cap points in the same direction.

Photoshop does not place independent printable pages beside one another like CorelDRAW. Therefore, each resulting panel is created as a separate open Photoshop document. Save each panel as PSD, TIFF, PDF, or another required production format after checking it.

## Run it without installing

1. Open the artwork in desktop Adobe Photoshop.
2. Confirm the artwork occupies the complete canvas. Use **Image > Canvas Size** if necessary.
3. Choose **File > Scripts > Browse…**.
4. Select `SMRI_PhotoshopPanelMaker.jsx`.
5. Answer the prompts:
   - Enter `C` for one-time media widths, `N` to save a preset, or choose preset `1` to `5`.
   - Choose `V` for vertical panels across the canvas width or `H` for horizontal panels across its height.
   - Enter overlap in inches.
   - Choose whether to add seam markers.
6. Photoshop opens one new document for each panel. The source remains open and unchanged.

No object or layer selection is required. The script always processes the complete canvas.

## Add it permanently to Photoshop's Scripts menu

Quit Photoshop, then copy `SMRI_PhotoshopPanelMaker.jsx` into Photoshop's `Presets/Scripts` folder.

### Windows

Usually:

`C:\Program Files\Adobe\Adobe Photoshop [version]\Presets\Scripts\`

### macOS

Usually:

`/Applications/Adobe Photoshop [version]/Presets/Scripts/`

Restart Photoshop. Run it from **File > Scripts > SMRI_PhotoshopPanelMaker**.

## Important production checks

- Confirm **Image > Image Size > Resolution** before running. Physical inches are calculated from the canvas pixel dimensions and this resolution.
- For example, a 10,000-pixel-wide image at 100 PPI is treated as 100 inches wide.
- Run the first test on a copy of a small document.
- Inspect panel dimensions using **Image > Image Size**.
- Check that neighboring panels repeat the requested overlap area.
- No black border is added to the artwork. Marker stems extend into the direction-specific margin, while their positions identify the real joining seam on or inside the artwork exactly as in the CorelDRAW macro.
- The script leaves panel documents unsaved so you can inspect them and choose the required file format and destination.

## Presets

The five presets are saved in the current user's application-data folder under `SMRI Panel Maker/photoshop-presets.txt`. Replacing the script normally does not remove saved presets.

## Distributable and basic activation

Give the customer only:

`SMRI_PhotoShop_PanelMaker.jsx`

Keep these private:

- `TOTP_gen.py` — generates the current activation code.
- `SMRI_PhotoshopPanelMaker.jsx` — readable development source.

On first use, the distributable asks for an 8-digit activation code. On your computer, run:

`python3 TOTP_gen.py`

Send the displayed code immediately; it changes every minute. The script accepts a one-minute clock difference in either direction. After successful activation it writes a machine/user-bound marker under the current user's `SMRI Panel Maker` application-data folder and does not ask again on that computer.

This is casual deterrence, not strong cryptographic licensing. The formula must exist in the distributed JavaScript and can be recovered by a determined reverse engineer. Do not give customers the Python generator.

## Make it a convenient Photoshop tool

The best simple setup for this JSX version is:

1. Copy `Distributable_SMRI_PanelMaker.jsx` into Photoshop's `Presets/Scripts` directory listed above.
2. Restart Photoshop.
3. It will appear under **File > Scripts > Distributable_SMRI_PanelMaker**; Browse is no longer required.

For one-key operation, create a Photoshop Action:

1. Open **Window > Actions**.
2. Create a new action named `SMRI Panel Maker` and assign an available function key such as `F4`.
3. Start recording.
4. Choose **File > Scripts > Distributable_SMRI_PanelMaker** and cancel at its first prompt after it starts.
5. Stop recording.

Photoshop records the script menu command in the action. The user can then press the assigned function key to launch the tool. If a Photoshop version does not retain a cancelled script step, complete one small test run while recording instead.
