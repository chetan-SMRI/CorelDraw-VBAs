Option Explicit

Private Const BLEND_MARKER_LENGTH As Double = 1

Private Function CeilD(ByVal x As Double) As Long
    If x <= Int(x) Then
        CeilD = CLng(Int(x))
    Else
        CeilD = CLng(Int(x) + 1)
    End If
End Function

Private Function FormatInches(ByVal x As Double) As String
    FormatInches = Trim$(Str$(Round(x, 2)))
End Function

Private Sub SortWidthsAscending(ByRef widths() As Double, ByVal widthCount As Long)
    Dim i As Long
    Dim j As Long
    Dim tmp As Double

    For i = 1 To widthCount - 1
        For j = i + 1 To widthCount
            If widths(i) > widths(j) Then
                tmp = widths(i)
                widths(i) = widths(j)
                widths(j) = tmp
            End If
        Next j
    Next i
End Sub

Private Function ParseMediaWidths(ByVal widthText As String, ByRef widths() As Double, ByRef widthCount As Long) As Boolean
    Dim parts() As String
    Dim i As Long
    Dim j As Long
    Dim w As Double
    Dim exists As Boolean

    parts = Split(widthText, ",")
    ReDim widths(1 To UBound(parts) + 1)
    widthCount = 0

    On Error GoTo InvalidWidth

    For i = LBound(parts) To UBound(parts)
        If Trim$(parts(i)) <> "" Then
            w = CDbl(Trim$(parts(i)))
            If w <= 0 Then GoTo InvalidWidth

            exists = False
            For j = 1 To widthCount
                If Abs(widths(j) - w) < 0.001 Then
                    exists = True
                    Exit For
                End If
            Next j

            If Not exists Then
                widthCount = widthCount + 1
                widths(widthCount) = w
            End If
        End If
    Next i

    If widthCount = 0 Then GoTo InvalidWidth

    ReDim Preserve widths(1 To widthCount)
    SortWidthsAscending widths, widthCount
    ParseMediaWidths = True
    Exit Function

InvalidWidth:
    ParseMediaWidths = False
End Function

Private Function IsHorizontalCut(ByVal directionText As String) As Boolean
    directionText = UCase$(Trim$(directionText))
    IsHorizontalCut = (directionText = "H" Or directionText = "2" Or directionText = "HORIZONTAL")
End Function

Private Function MarkerLength(ByVal overlap As Double) As Double
    If overlap > 0 And overlap < BLEND_MARKER_LENGTH Then
        MarkerLength = overlap
    Else
        MarkerLength = BLEND_MARKER_LENGTH
    End If
End Function

Private Sub CreateBlendMarker(ByVal x1 As Double, ByVal y1 As Double, ByVal x2 As Double, ByVal y2 As Double)
    Dim marker As Shape
    Set marker = ActiveLayer.CreateLineSegment(x1, y1, x2, y2)
    marker.Name = "Blend_Marker"
    marker.Outline.Width = 0.01
End Sub

Private Function IsYes(ByVal valueText As String) As Boolean
    valueText = UCase$(Trim$(valueText))
    IsYes = (valueText = "Y" Or valueText = "YES" Or valueText = "1")
End Function

Private Sub CreateHorizontalTMarker(ByVal markerX As Double, ByVal edgeY As Double, _
    ByVal outsideDir As Double, ByVal alongDir As Double, ByVal markerLen As Double)

    Dim outerY As Double
    outerY = edgeY + (outsideDir * markerLen)

    CreateBlendMarker markerX, edgeY, markerX, outerY
    CreateBlendMarker markerX, outerY, markerX + (alongDir * markerLen), outerY
End Sub

Private Sub CreateVerticalTMarker(ByVal edgeX As Double, ByVal markerY As Double, _
    ByVal outsideDir As Double, ByVal alongDir As Double, ByVal markerLen As Double)

    Dim outerX As Double
    outerX = edgeX + (outsideDir * markerLen)

    CreateBlendMarker edgeX, markerY, outerX, markerY
    CreateBlendMarker outerX, markerY, outerX, markerY + (alongDir * markerLen)
End Sub

Private Sub AddVerticalPanelSeamMarkers(ByVal seamX As Double, ByVal startY As Double, ByVal panelH As Double, _
    ByVal alongDir As Double, ByVal markerLen As Double)

    CreateHorizontalTMarker seamX, startY, -1, alongDir, markerLen

    If panelH > 0 Then
        CreateHorizontalTMarker seamX, startY + panelH, 1, alongDir, markerLen
    End If
End Sub

Private Sub AddHorizontalPanelSeamMarkers(ByVal startX As Double, ByVal seamY As Double, ByVal panelW As Double, _
    ByVal alongDir As Double, ByVal markerLen As Double)

    CreateVerticalTMarker startX, seamY, -1, alongDir, markerLen

    If panelW > 0 Then
        CreateVerticalTMarker startX + panelW, seamY, 1, alongDir, markerLen
    End If
End Sub

Private Sub AddBlendMarkers(ByVal destX As Double, ByVal destY As Double, ByVal panelW As Double, _
    ByVal panelH As Double, ByVal panelIndex As Long, ByVal panelCount As Long, _
    ByVal horizontalCut As Boolean, ByVal overlap As Double)

    Dim markerLen As Double
    markerLen = MarkerLength(overlap)

    If markerLen <= 0 Then Exit Sub

    If horizontalCut Then
        If panelIndex > 0 Then
            AddHorizontalPanelSeamMarkers destX, destY, panelW, 1, markerLen
        End If

        If panelIndex < panelCount - 1 Then
            AddHorizontalPanelSeamMarkers destX, destY + panelH - markerLen, panelW, 1, markerLen
        End If
    Else
        If panelIndex > 0 Then
            AddVerticalPanelSeamMarkers destX, destY, panelH, 1, markerLen
        End If

        If panelIndex < panelCount - 1 Then
            AddVerticalPanelSeamMarkers destX + panelW - markerLen, destY, panelH, 1, markerLen
        End If
    End If
End Sub

Private Function SavedPresetLabel(ByVal slot As Long) As String
    Dim presetName As String
    Dim presetWidths As String

    presetName = GetSetting("SMRI", "PanelMaker", "Preset" & slot & "Name", "")
    presetWidths = GetSetting("SMRI", "PanelMaker", "Preset" & slot & "Widths", "")

    If presetName = "" Then presetName = "Preset " & slot

    If presetWidths = "" Then
        SavedPresetLabel = slot & " = " & presetName & " (empty)"
    Else
        SavedPresetLabel = slot & " = " & presetName & " (" & presetWidths & ")"
    End If
End Function

Private Function PresetMenuText() As String
    Dim i As Long
    Dim txt As String

    txt = "Choose width preset:" & vbCrLf
    For i = 1 To 5
        txt = txt & SavedPresetLabel(i) & vbCrLf
    Next i

    txt = txt & "N = Create / update preset" & vbCrLf & _
        "C = Custom one-time widths"

    PresetMenuText = txt
End Function

Private Function LoadPresetWidths(ByVal slot As Long) As String
    LoadPresetWidths = GetSetting("SMRI", "PanelMaker", "Preset" & slot & "Widths", "")
End Function

Private Sub SavePreset(ByVal slot As Long, ByVal presetName As String, ByVal presetWidths As String)
    SaveSetting "SMRI", "PanelMaker", "Preset" & slot & "Name", presetName
    SaveSetting "SMRI", "PanelMaker", "Preset" & slot & "Widths", presetWidths
End Sub

Private Function TryChooseMediaText(ByRef mediaText As String) As Boolean
    Dim presetChoice As String
    Dim presetSlot As Long
    Dim presetName As String
    Dim presetWidths As String

ChooseAgain:
    presetChoice = InputBox(PresetMenuText(), "SMRI Panel Maker", "1")
    If Trim$(presetChoice) = "" Then
        TryChooseMediaText = False
        Exit Function
    End If

    presetChoice = UCase$(Trim$(presetChoice))

    If presetChoice = "C" Or presetChoice = "CUSTOM" Then
        mediaText = InputBox("Enter available media widths in inches, separated by commas:", "SMRI Panel Maker", "39,49,59")
        TryChooseMediaText = (Trim$(mediaText) <> "")
        Exit Function
    End If

    If presetChoice = "N" Or presetChoice = "NEW" Then
        presetSlot = CLng(Val(InputBox("Save preset to slot number 1-5:", "SMRI Panel Maker", "1")))
        If presetSlot < 1 Or presetSlot > 5 Then
            MsgBox "Please choose a slot from 1 to 5.", vbExclamation
            GoTo ChooseAgain
        End If

        presetName = InputBox("Preset name:", "SMRI Panel Maker", "My Preset")
        If Trim$(presetName) = "" Then
            TryChooseMediaText = False
            Exit Function
        End If

        presetWidths = InputBox("Preset widths in inches, separated by commas:", "SMRI Panel Maker", "39,49,59")
        If Trim$(presetWidths) = "" Then
            TryChooseMediaText = False
            Exit Function
        End If

        SavePreset presetSlot, presetName, presetWidths
        mediaText = presetWidths
        TryChooseMediaText = True
        Exit Function
    End If

    presetSlot = CLng(Val(presetChoice))
    If presetSlot >= 1 And presetSlot <= 5 Then
        mediaText = LoadPresetWidths(presetSlot)
        If Trim$(mediaText) = "" Then
            MsgBox "That preset slot is empty. Create a preset first or choose Custom.", vbExclamation
            GoTo ChooseAgain
        End If

        TryChooseMediaText = True
        Exit Function
    End If

    MsgBox "Choose preset 1-5, N for new preset, or C for custom.", vbExclamation
    GoTo ChooseAgain
End Function

Private Sub FindBestCounts(ByRef mediaWidths() As Double, ByVal mediaCount As Long, ByVal idx As Long, _
    ByVal remainingPanels As Long, ByVal currentSum As Double, ByVal targetSum As Double, _
    ByRef counts() As Long, ByRef bestCounts() As Long, ByRef bestWaste As Double, ByRef bestFound As Boolean)

    Dim c As Long
    Dim totalSum As Double
    Dim waste As Double
    Dim i As Long

    If idx = mediaCount Then
        counts(idx) = remainingPanels
        totalSum = currentSum + (remainingPanels * mediaWidths(idx))

        If totalSum >= targetSum Then
            waste = totalSum - targetSum
            If Not bestFound Or waste < bestWaste - 0.001 Then
                bestFound = True
                bestWaste = waste
                For i = 1 To mediaCount
                    bestCounts(i) = counts(i)
                Next i
            End If
        End If
        Exit Sub
    End If

    For c = 0 To remainingPanels
        counts(idx) = c
        FindBestCounts mediaWidths, mediaCount, idx + 1, remainingPanels - c, _
            currentSum + (c * mediaWidths(idx)), targetSum, counts, bestCounts, bestWaste, bestFound
    Next c
End Sub

Private Function BuildPanelSuggestions(ByVal artworkW As Double, ByRef mediaWidths() As Double, _
    ByVal mediaCount As Long, ByVal overlap As Double, ByRef panelWidths() As Double, _
    ByRef panelCount As Long) As Boolean

    Dim maxMediaW As Double
    Dim minMediaW As Double
    Dim maxPanels As Long
    Dim tryPanels As Long
    Dim targetSum As Double
    Dim counts() As Long
    Dim bestCounts() As Long
    Dim bestWaste As Double
    Dim bestFound As Boolean
    Dim i As Long
    Dim n As Long

    minMediaW = mediaWidths(1)
    maxMediaW = mediaWidths(mediaCount)

    If overlap >= minMediaW Then
        BuildPanelSuggestions = False
        Exit Function
    End If

    maxPanels = CeilD(artworkW / (minMediaW - overlap)) + 2
    ReDim counts(1 To mediaCount)
    ReDim bestCounts(1 To mediaCount)

    For tryPanels = 1 To maxPanels
        If maxMediaW + ((tryPanels - 1) * (maxMediaW - overlap)) >= artworkW Then
            targetSum = artworkW + ((tryPanels - 1) * overlap)
            bestFound = False
            bestWaste = 0

            FindBestCounts mediaWidths, mediaCount, 1, tryPanels, 0, targetSum, _
                counts, bestCounts, bestWaste, bestFound

            If bestFound Then
                panelCount = tryPanels
                ReDim panelWidths(0 To panelCount - 1)
                n = 0

                For i = mediaCount To 1 Step -1
                    Do While bestCounts(i) > 0
                        panelWidths(n) = mediaWidths(i)
                        n = n + 1
                        bestCounts(i) = bestCounts(i) - 1
                    Loop
                Next i

                BuildPanelSuggestions = True
                Exit Function
            End If
        End If
    Next tryPanels

    BuildPanelSuggestions = False
End Function


Sub SMRI_AutoPanelPowerClips()

    If ActiveSelectionRange.Count = 0 Then
        MsgBox "Please select your artwork/image first.", vbExclamation
        Exit Sub
    End If

    Dim oldUnit As cdrUnit
    oldUnit = ActiveDocument.Unit
    ActiveDocument.Unit = cdrInch

    Dim mediaWidths() As Double
    Dim panelWidths() As Double
    Dim mediaText As String
    Dim directionText As String
    Dim overlapText As String
    Dim markerText As String
    Dim horizontalCut As Boolean
    Dim addBleedMarkers As Boolean
    Dim mediaCount As Long
    Dim overlap As Double
    Dim gap As Double

    If Not TryChooseMediaText(mediaText) Then
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    If Not ParseMediaWidths(mediaText, mediaWidths, mediaCount) Then
        MsgBox "Please enter valid media widths, like 39,49,59.", vbCritical
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    directionText = InputBox("Choose cut direction:" & vbCrLf & _
        "V = Vertical panels, split by artwork width" & vbCrLf & _
        "H = Horizontal panels, split by artwork height", "SMRI Panel Maker", "V")

    If Trim$(directionText) = "" Then
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    horizontalCut = IsHorizontalCut(directionText)

    overlapText = InputBox("Enter overlap in inches:", "SMRI Panel Maker", "0.5")
    On Error GoTo InvalidOverlap
    overlap = CDbl(overlapText)
    On Error GoTo 0
    gap = 1

    If overlap < 0 Then overlap = 0
    If overlap >= mediaWidths(1) Then
        MsgBox "Overlap must be smaller than the smallest media width.", vbCritical
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    markerText = InputBox("Add outside bleeding / overlap markers? Y/N", "SMRI Panel Maker", "Y")
    If Trim$(markerText) = "" Then
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If
    addBleedMarkers = IsYes(markerText)

    Dim src As ShapeRange
    Set src = ActiveSelectionRange

    Dim x As Double, y As Double, w As Double, h As Double
    src.GetBoundingBox x, y, w, h, True

    Dim panels As Long
    Dim artworkSpan As Double

    If horizontalCut Then
        artworkSpan = h
    Else
        artworkSpan = w
    End If

    If Not BuildPanelSuggestions(artworkSpan, mediaWidths, mediaCount, overlap, panelWidths, panels) Then
        MsgBox "Could not build panel suggestions from the entered media widths.", vbCritical
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    Dim startX As Double
    Dim startY As Double

    startX = x
    startY = y - h - gap

    Dim i As Long
    Dim panelLeft As Double
    Dim panelW As Double
    Dim panelTop As Double
    Dim panelH As Double
    Dim remainingW As Double
    Dim remainingH As Double
    Dim destX As Double
    Dim destY As Double
    Dim currentSourceX As Double
    Dim currentSourceY As Double
    Dim currentDestX As Double
    Dim currentDestY As Double
    Dim suggestionText As String
    Dim createdPanels As Long
    Dim horizontalLabelX As Double
    Dim horizontalLabelY As Double

    ActiveDocument.BeginCommandGroup "SMRI Auto Panel PowerClips"

    currentSourceX = x
    currentSourceY = y
    currentDestX = startX
    currentDestY = y
    suggestionText = "Panel suggestions:" & vbCrLf
    createdPanels = 0

    For i = 0 To panels - 1

        If horizontalCut Then
            panelTop = currentSourceY
            panelH = panelWidths(i)
            remainingH = (y + h) - panelTop
            If panelH > remainingH Then panelH = remainingH
            If panelH <= 0 Then Exit For

            destX = x + w + gap
            destY = currentDestY
            createdPanels = createdPanels + 1
            suggestionText = suggestionText & "Panel " & (i + 1) & " " & FormatInches(panelH) & """" & vbCrLf

            Dim hBox As Shape
            Set hBox = ActiveLayer.CreateRectangle2(destX, destY, w, panelH)

            hBox.Name = "Panel_" & Format(i + 1, "00")
            hBox.Fill.ApplyNoFill
            hBox.Outline.Width = 0.01

            Dim hDup As ShapeRange
            Set hDup = src.Duplicate

            hDup.Move destX - x, destY - panelTop

            hDup.AddToPowerClip hBox, cdrFalse
            If addBleedMarkers Then
                AddBlendMarkers destX, destY, w, panelH, i, panels, True, overlap
            End If

            Dim hLabel As Shape
            horizontalLabelX = destX - 0.35
            horizontalLabelY = destY

            Set hLabel = ActiveLayer.CreateArtisticText(horizontalLabelX, horizontalLabelY, _
                "Part " & (i + 1) & " | Size: " & FormatInches(w) & _
                """x" & FormatInches(h) & """ | Part Height: " & FormatInches(panelH) & _
                """ | Overlap " & FormatInches(overlap) & """")

            hLabel.Text.Story.Size = 18
            hLabel.Text.Story.Font = "Arial"
            hLabel.Text.Story.Bold = True
            hLabel.RotationCenterX = horizontalLabelX
            hLabel.RotationCenterY = horizontalLabelY
            hLabel.Rotate 90

            currentSourceY = currentSourceY + panelH - overlap
            currentDestY = currentDestY + panelH + gap
        Else
            panelLeft = currentSourceX
            panelW = panelWidths(i)
            remainingW = (x + w) - panelLeft
            If panelW > remainingW Then panelW = remainingW
            If panelW <= 0 Then Exit For

            destX = currentDestX
            createdPanels = createdPanels + 1
            suggestionText = suggestionText & "Panel " & (i + 1) & " " & FormatInches(panelW) & """" & vbCrLf

            Dim box As Shape
            Set box = ActiveLayer.CreateRectangle2(destX, startY, panelW, h)

            box.Name = "Panel_" & Format(i + 1, "00")
            box.Fill.ApplyNoFill
            box.Outline.Width = 0.01

            Dim dup As ShapeRange
            Set dup = src.Duplicate

            dup.Move destX - panelLeft, startY - y

            dup.AddToPowerClip box, cdrFalse
            If addBleedMarkers Then
                AddBlendMarkers destX, startY, panelW, h, i, panels, False, overlap
            End If

            Dim label As Shape
            Set label = ActiveLayer.CreateArtisticText(destX, startY + h + 0.14, _
                "Part " & (i + 1) & " | Size: " & FormatInches(w) & _
                """x" & FormatInches(h) & """ | Part Width: " & FormatInches(panelW) & _
                """ | Overlap " & FormatInches(overlap) & """")

            label.Text.Story.Size = 18
            label.Text.Story.Font = "Arial"
            label.Text.Story.Bold = True

            currentSourceX = currentSourceX + panelW - overlap
            currentDestX = currentDestX + panelW + gap
        End If

    Next i

    ActiveDocument.EndCommandGroup
    ActiveDocument.Unit = oldUnit

    MsgBox createdPanels & " panels created successfully." & vbCrLf & vbCrLf & suggestionText, vbInformation
    Exit Sub

InvalidOverlap:
    MsgBox "Please enter a valid overlap, like 0.5.", vbCritical
    ActiveDocument.Unit = oldUnit

End Sub
