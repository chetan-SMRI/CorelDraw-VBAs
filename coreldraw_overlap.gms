Option Explicit

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
    Dim overlapText As String
    Dim mediaCount As Long
    Dim overlap As Double
    Dim gap As Double

    mediaText = InputBox("Enter available media widths in inches, separated by commas:", "SMRI Panel Maker", "39,49,59")
    If Not ParseMediaWidths(mediaText, mediaWidths, mediaCount) Then
        MsgBox "Please enter valid media widths, like 39,49,59.", vbCritical
        ActiveDocument.Unit = oldUnit
        Exit Sub
    End If

    overlapText = InputBox("Enter overlap in inches:", "SMRI Panel Maker", "0.75")
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

    Dim src As ShapeRange
    Set src = ActiveSelectionRange

    Dim x As Double, y As Double, w As Double, h As Double
    src.GetBoundingBox x, y, w, h, True

    Dim panels As Long
    If Not BuildPanelSuggestions(w, mediaWidths, mediaCount, overlap, panelWidths, panels) Then
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
    Dim remainingW As Double
    Dim destX As Double
    Dim currentSourceX As Double
    Dim currentDestX As Double
    Dim suggestionText As String
    Dim createdPanels As Long

    ActiveDocument.BeginCommandGroup "SMRI Auto Panel PowerClips"

    currentSourceX = x
    currentDestX = startX
    suggestionText = "Panel suggestions:" & vbCrLf
    createdPanels = 0

    For i = 0 To panels - 1

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

        Dim label As Shape
        Set label = ActiveLayer.CreateArtisticText(destX, startY + h + 0.04, _
            "Tile " & (i + 1) & " | Total Width: " & FormatInches(w) & _
            """ | Panel Width: " & FormatInches(panelW) & """")

        label.Text.Story.Size = 18
        label.Text.Story.Font = "Arial"
        label.Text.Story.Bold = True

        currentSourceX = currentSourceX + panelW - overlap
        currentDestX = currentDestX + panelW + gap

    Next i

    ActiveDocument.EndCommandGroup
    ActiveDocument.Unit = oldUnit

    MsgBox createdPanels & " panels created successfully." & vbCrLf & vbCrLf & suggestionText, vbInformation
    Exit Sub

InvalidOverlap:
    MsgBox "Please enter a valid overlap, like 0.75.", vbCritical
    ActiveDocument.Unit = oldUnit

End Sub
