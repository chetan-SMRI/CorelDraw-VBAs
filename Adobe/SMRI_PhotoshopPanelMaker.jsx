#target photoshop
app.bringToFront();

/* SMRI Panel Maker for Adobe Photoshop — direct-running, unlicensed JSX. */
(function () {
    var EPS = 0.001;

    function trim(v) { return String(v).replace(/^\s+|\s+$/g, ""); }
    function fmt(v) { return String(Math.round(v * 100) / 100); }
    function yes(v) {
        v = trim(v).toUpperCase();
        return v === "Y" || v === "YES" || v === "1";
    }
    function safeName(v) {
        return String(v).replace(/[\\\/:*?"<>|]/g, "_");
    }
    function parseWidths(text) {
        var raw = String(text).split(","), result = [], i, j, n, found;
        for (i = 0; i < raw.length; i++) {
            if (trim(raw[i]) === "") continue;
            n = Number(trim(raw[i]));
            if (!isFinite(n) || n <= 0) return null;
            found = false;
            for (j = 0; j < result.length; j++) {
                if (Math.abs(result[j] - n) < EPS) found = true;
            }
            if (!found) result.push(n);
        }
        result.sort(function (a, b) { return a - b; });
        return result.length ? result : null;
    }

    function presetFile() {
        var folder = new Folder(Folder.userData + "/SMRI Panel Maker");
        if (!folder.exists) folder.create();
        return new File(folder.fsName + "/photoshop-presets.txt");
    }
    function loadPresets() {
        var result = [null, null, null, null, null], file = presetFile();
        if (!file.exists || !file.open("r")) return result;
        while (!file.eof) {
            var parts = file.readln().split("\t");
            var slot = Number(parts[0]);
            if (slot >= 1 && slot <= 5 && parts.length >= 3) {
                result[slot - 1] = { name: parts[1], widths: parts.slice(2).join("\t") };
            }
        }
        file.close();
        return result;
    }
    function savePresets(presets) {
        var file = presetFile();
        if (!file.open("w")) return false;
        for (var i = 0; i < 5; i++) {
            if (presets[i]) file.writeln((i + 1) + "\t" +
                presets[i].name.replace(/[\t\r\n]/g, " ") + "\t" +
                presets[i].widths.replace(/[\t\r\n]/g, " "));
        }
        file.close();
        return true;
    }
    function chooseWidths() {
        var presets = loadPresets();
        while (true) {
            var text = "Choose width preset:\n", i;
            for (i = 0; i < 5; i++) {
                text += (i + 1) + " = " + (presets[i] ?
                    presets[i].name + " (" + presets[i].widths + ")" :
                    "Preset " + (i + 1) + " (empty)") + "\n";
            }
            text += "N = Create / update preset\nC = Custom one-time widths";
            var choice = prompt(text, "C", "SMRI Photoshop Panel Maker");
            if (choice === null) return null;
            choice = trim(choice).toUpperCase();
            if (choice === "C" || choice === "CUSTOM") {
                return prompt("Available media widths in inches, separated by commas:", "39,49,59", "SMRI Photoshop Panel Maker");
            }
            if (choice === "N" || choice === "NEW") {
                var slotText = prompt("Save to preset slot 1-5:", "1", "SMRI Photoshop Panel Maker");
                if (slotText === null) return null;
                var slot = Number(slotText);
                if (slot < 1 || slot > 5 || Math.floor(slot) !== slot) {
                    alert("Choose a slot from 1 to 5."); continue;
                }
                var name = prompt("Preset name:", "My Preset", "SMRI Photoshop Panel Maker");
                if (name === null || trim(name) === "") return null;
                var widths = prompt("Preset widths in inches, separated by commas:", "39,49,59", "SMRI Photoshop Panel Maker");
                if (widths === null || !parseWidths(widths)) {
                    alert("Enter valid widths, for example 39,49,59."); continue;
                }
                presets[slot - 1] = { name: trim(name), widths: trim(widths) };
                savePresets(presets);
                return widths;
            }
            var selected = Number(choice);
            if (selected >= 1 && selected <= 5 && presets[selected - 1]) return presets[selected - 1].widths;
            alert("Choose a saved preset 1-5, N for new, or C for custom.");
        }
    }

    function bestPanels(span, media, overlap) {
        var minW = media[0], maxW = media[media.length - 1];
        if (overlap >= minW) return null;
        var limit = Math.ceil(span / (minW - overlap)) + 2;

        function findCounts(panelCount, target) {
            var counts = [], best = null, waste = Infinity;
            function visit(index, remaining, total) {
                if (index === media.length - 1) {
                    counts[index] = remaining;
                    var sum = total + remaining * media[index];
                    if (sum >= target && sum - target < waste - EPS) {
                        waste = sum - target; best = counts.slice(0);
                    }
                    return;
                }
                for (var c = 0; c <= remaining; c++) {
                    counts[index] = c;
                    visit(index + 1, remaining - c, total + c * media[index]);
                }
            }
            visit(0, panelCount, 0);
            return best;
        }

        for (var count = 1; count <= limit; count++) {
            if (maxW + (count - 1) * (maxW - overlap) < span) continue;
            var counts = findCounts(count, span + (count - 1) * overlap);
            if (counts) {
                var result = [];
                for (var i = media.length - 1; i >= 0; i--)
                    for (var n = 0; n < counts[i]; n++) result.push(media[i]);
                return result;
            }
        }
        return null;
    }

    function black() {
        var color = new SolidColor();
        color.rgb.red = 0; color.rgb.green = 0; color.rgb.blue = 0;
        return color;
    }
    function fillRect(doc, layer, left, top, right, bottom) {
        doc.activeLayer = layer;
        doc.selection.select([[left, top], [right, top], [right, bottom], [left, bottom]]);
        doc.selection.fill(black(), ColorBlendMode.NORMAL, 100, false);
        doc.selection.deselect();
    }
    function addProductionDetails(doc, horizontal, index, count, overlapInches, infoText, drawMarkers) {
        var dpi = doc.resolution;
        var len = Math.max(4, Math.round(Math.min(overlapInches, 1) * dpi));
        var thick = Math.max(1, Math.round(dpi / 100));
        var artW = Math.round(doc.width.as("px")), artH = Math.round(doc.height.as("px"));
        var margin = Math.max(Math.round(0.55 * dpi), len + Math.round(0.15 * dpi));
        var canvasW = horizontal ? artW + margin * 2 : artW;
        var canvasH = horizontal ? artH : artH + margin * 2;
        doc.resizeCanvas(UnitValue(canvasW, "px"), UnitValue(canvasH, "px"),
            AnchorPosition.MIDDLECENTER);

        var paper = doc.artLayers.add();
        paper.name = "White production margin";
        var white = new SolidColor();
        white.rgb.red = 255; white.rgb.green = 255; white.rgb.blue = 255;
        function fillWhite(left, top, right, bottom) {
            doc.activeLayer = paper;
            doc.selection.select([[left, top], [right, top], [right, bottom], [left, bottom]]);
            doc.selection.fill(white, ColorBlendMode.NORMAL, 100, false);
            doc.selection.deselect();
        }
        // Only create margin where this cut direction actually uses it.
        if (horizontal) {
            fillWhite(0, 0, margin, canvasH);
            fillWhite(margin + artW, 0, canvasW, canvasH);
        } else {
            fillWhite(0, 0, canvasW, margin);
            fillWhite(0, margin + artH, canvasW, canvasH);
        }

        var detailLayer = doc.artLayers.add(); detailLayer.name = "Overlap joining markers";
        var left = horizontal ? margin : 0;
        var top = horizontal ? 0 : margin;
        var right = left + artW, bottom = top + artH;

        function addVerticalPanelSeam(seamX) {
            // Same as Corel AddVerticalPanelSeamMarkers: both T caps point right.
            fillRect(doc, detailLayer, seamX, top - len, seamX + thick, top);
            fillRect(doc, detailLayer, seamX, top - len, seamX + len, top - len + thick);
            fillRect(doc, detailLayer, seamX, bottom, seamX + thick, bottom + len);
            fillRect(doc, detailLayer, seamX, bottom + len - thick, seamX + len, bottom + len);
        }
        function addHorizontalPanelSeam(seamY) {
            // Same as Corel AddHorizontalPanelSeamMarkers: both T caps point down.
            fillRect(doc, detailLayer, left - len, seamY, left, seamY + thick);
            fillRect(doc, detailLayer, left - len, seamY, left - len + thick, seamY + len);
            fillRect(doc, detailLayer, right, seamY, right + len, seamY + thick);
            fillRect(doc, detailLayer, right + len - thick, seamY, right + len, seamY + len);
        }

        if (drawMarkers && overlapInches > 0 && count > 1 && !horizontal) {
            // Incoming join is the left edge. Outgoing join is overlap distance
            // inside the right edge—not on the outside edge.
            if (index > 0) addVerticalPanelSeam(left);
            if (index < count - 1) addVerticalPanelSeam(right - len);
        } else if (drawMarkers && overlapInches > 0 && count > 1) {
            // Incoming join is the top edge. Outgoing join is overlap distance
            // inside the bottom edge.
            if (index > 0) addHorizontalPanelSeam(top);
            if (index < count - 1) addHorizontalPanelSeam(bottom - len);
        }

        var label = doc.artLayers.add();
        label.kind = LayerKind.TEXT;
        label.name = "Panel information";
        label.textItem.contents = infoText;
        if (horizontal) {
            label.textItem.position = [UnitValue(Math.round(margin * 0.65), "px"), UnitValue(bottom, "px")];
        } else {
            label.textItem.position = [UnitValue(left, "px"), UnitValue(Math.round(margin * 0.65), "px")];
        }
        label.textItem.size = UnitValue(18, "pt");
        label.textItem.color = black();
        try { label.textItem.font = "Arial-BoldMT"; } catch (fontError) {}
        if (horizontal) label.rotate(-90, AnchorPosition.BOTTOMLEFT);
    }

    if (app.documents.length === 0) {
        alert("Open a Photoshop document first.", "SMRI Photoshop Panel Maker"); return;
    }
    var source = app.activeDocument;
    var widthIn = source.width.as("in"), heightIn = source.height.as("in");
    if (widthIn <= 0 || heightIn <= 0) {
        alert("The document has no usable canvas size.", "SMRI Photoshop Panel Maker"); return;
    }
    var mediaText = chooseWidths();
    if (mediaText === null) return;
    var media = parseWidths(mediaText);
    if (!media) { alert("Enter valid media widths, for example 39,49,59."); return; }

    var direction = prompt("Cut direction:\nV = Vertical panels across canvas width\nH = Horizontal panels across canvas height", "V", "SMRI Photoshop Panel Maker");
    if (direction === null) return;
    direction = trim(direction).toUpperCase();
    var horizontal = direction === "H" || direction === "2" || direction === "HORIZONTAL";
    var overlapText = prompt("Overlap in inches:", "0.5", "SMRI Photoshop Panel Maker");
    if (overlapText === null) return;
    var overlap = Number(trim(overlapText));
    if (!isFinite(overlap)) { alert("Enter a valid overlap, for example 0.5."); return; }
    if (overlap < 0) overlap = 0;
    if (overlap >= media[0]) { alert("Overlap must be smaller than the smallest media width."); return; }
    var markersText = prompt("Add Corel-style overlap / joining markers? Y/N", "Y", "SMRI Photoshop Panel Maker");
    if (markersText === null) return;

    var span = horizontal ? heightIn : widthIn;
    var panels = bestPanels(span, media, overlap);
    if (!panels) { alert("Could not create panels from these media widths."); return; }

    var oldUnits = app.preferences.rulerUnits;
    app.preferences.rulerUnits = Units.PIXELS;
    var dpi = source.resolution;
    var fullW = Math.round(source.width.as("px")), fullH = Math.round(source.height.as("px"));
    var positionIn = 0, created = 0, summary = "Created panel documents:\n";
    var base = safeName(source.name.replace(/\.[^\.]+$/, ""));

    try {
        for (var i = 0; i < panels.length; i++) {
            var remaining = span - positionIn;
            var panelIn = Math.min(panels[i], remaining);
            if (panelIn <= 0) break;
            var panelNo = (i + 1 < 10 ? "0" : "") + (i + 1);
            var panelName = base + "_Panel_" + panelNo;
            app.activeDocument = source;
            var panelDoc = source.duplicate(panelName, false);
            var startPx = Math.round(positionIn * dpi);
            var endPx = Math.round((positionIn + panelIn) * dpi);
            if (horizontal) panelDoc.crop([0, startPx, fullW, endPx]);
            else panelDoc.crop([startPx, 0, endPx, fullH]);
            var infoText = "Part " + (i + 1) + " | Original: " + fmt(widthIn) + " x " + fmt(heightIn) +
                " in | " + (horizontal ? "Part Height: " : "Part Width: ") + fmt(panelIn) +
                " in | Overlap: " + fmt(overlap) + " in";
            addProductionDetails(panelDoc, horizontal, i, panels.length, overlap, infoText, yes(markersText));
            created++;
            summary += panelName + " — " + fmt(panelIn) + " in\n";
            positionIn += panelIn - overlap;
        }
    } catch (error) {
        alert("Stopped while creating panels:\n" + error.message + "\nLine: " + error.line,
            "SMRI Photoshop Panel Maker");
        return;
    } finally {
        app.preferences.rulerUnits = oldUnits;
    }
    app.activeDocument = source;
    alert(created + " panels created. The original document was not changed.\n\n" + summary +
        "\nSave the new panel documents when ready.", "SMRI Photoshop Panel Maker");
}());
