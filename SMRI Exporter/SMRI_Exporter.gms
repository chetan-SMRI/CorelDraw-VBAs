Option Explicit

Private Const EXPORTER_TITLE As String = "SMRI Exporter"

Private Function ChooseSequenceStyle(ByRef sequenceStyle As String) As Boolean
    Dim choice As String

    choice = Trim$(InputBox( _
        "Choose the filename sequence:" & vbCrLf & vbCrLf & _
        "1 = 1, 2, 3..." & vbCrLf & _
        "a = a, b, c..." & vbCrLf & _
        "A = A, B, C...", EXPORTER_TITLE, "a"))

    If choice = "" Then Exit Function

    Select Case choice
        Case "1", "a", "A"
            sequenceStyle = choice
        Case Else
            MsgBox "Enter 1, a, or A.", vbExclamation, EXPORTER_TITLE
            Exit Function
    End Select

    ChooseSequenceStyle = True
End Function

Private Function AlphabeticSequence(ByVal sequence As Long, ByVal useUppercase As Boolean) As String
    Dim result As String
    Dim characterCode As Long

    Do While sequence > 0
        sequence = sequence - 1
        characterCode = sequence Mod 26
        result = Chr$(Asc("a") + characterCode) & result
        sequence = sequence \ 26
    Loop

    If useUppercase Then result = UCase$(result)
    AlphabeticSequence = result
End Function

Private Function SequenceLabel(ByVal sequence As Long, ByVal sequenceStyle As String) As String
    If sequenceStyle = "1" Then
        SequenceLabel = CStr(sequence)
    Else
        SequenceLabel = AlphabeticSequence(sequence, sequenceStyle = "A")
    End If
End Function

Private Function DocumentBaseName() As String
    Dim documentName As String
    Dim dotPosition As Long

    documentName = ActiveDocument.Name
    dotPosition = InStrRev(documentName, ".")

    If dotPosition > 1 Then documentName = Left$(documentName, dotPosition - 1)
    DocumentBaseName = CleanFilePart(documentName)
End Function

Private Function ChooseExportFormat(ByRef formatName As String, ByRef extension As String, _
    ByRef filterType As cdrFilter) As Boolean

    Dim choice As String
    choice = Trim$(InputBox( _
        "Choose export format:" & vbCrLf & vbCrLf & _
        "1 = JPG" & vbCrLf & _
        "2 = PNG" & vbCrLf & _
        "3 = TIFF" & vbCrLf & _
        "4 = PDF", EXPORTER_TITLE, "1"))

    If choice = "" Then Exit Function

    Select Case UCase$(choice)
        Case "1", "JPG", "JPEG"
            formatName = "JPG"
            extension = ".jpg"
            filterType = cdrJPEG
        Case "2", "PNG"
            formatName = "PNG"
            extension = ".png"
            filterType = cdrPNG
        Case "3", "TIF", "TIFF"
            formatName = "TIFF"
            extension = ".tif"
            filterType = cdrTIFF
        Case "4", "PDF"
            formatName = "PDF"
            extension = ".pdf"
        Case Else
            MsgBox "Choose 1, 2, 3, or 4.", vbExclamation, EXPORTER_TITLE
            Exit Function
    End Select

    ChooseExportFormat = True
End Function

Private Function AskForPositiveLong(ByVal promptText As String, ByVal defaultText As String, _
    ByRef result As Long) As Boolean

    Dim valueText As String
    Dim parsedValue As Double

    valueText = Trim$(InputBox(promptText, EXPORTER_TITLE, defaultText))
    If valueText = "" Then Exit Function

    On Error GoTo InvalidValue
    parsedValue = CDbl(valueText)
    If parsedValue < 1 Or parsedValue > 100000 Or parsedValue <> Fix(parsedValue) Then GoTo InvalidValue

    result = CLng(parsedValue)
    AskForPositiveLong = True
    Exit Function

InvalidValue:
    MsgBox "Enter a positive whole number.", vbExclamation, EXPORTER_TITLE
End Function

Private Function AskForJpegQuality(ByRef quality As Long) As Boolean
    Dim valueText As String
    Dim parsedValue As Double

    valueText = Trim$(InputBox("Enter JPG quality from 1 to 100:", EXPORTER_TITLE, "100"))
    If valueText = "" Then Exit Function

    On Error GoTo InvalidQuality
    parsedValue = CDbl(valueText)
    If parsedValue < 1 Or parsedValue > 100 Or parsedValue <> Fix(parsedValue) Then GoTo InvalidQuality

    quality = CLng(parsedValue)
    AskForJpegQuality = True
    Exit Function

InvalidQuality:
    MsgBox "Enter a whole number from 1 to 100.", vbExclamation, EXPORTER_TITLE
End Function

Private Function ChooseFolder() As String
    Dim shellApp As Object
    Dim folder As Object
    Dim baseFolder As String
    Dim requestedFolder As String
    Dim exportFolder As String
    Dim copyNumber As Long

    On Error GoTo FolderError
    baseFolder = GetSetting("SMRI Exporter", "Export", "BaseFolder", "")

    If baseFolder <> "" Then
        If MsgBox("Use this export folder?" & vbCrLf & vbCrLf & baseFolder, _
            vbYesNo + vbQuestion, EXPORTER_TITLE) = vbNo Then
            baseFolder = ""
        End If
    End If

    If baseFolder = "" Then
        Set shellApp = CreateObject("Shell.Application")
        Set folder = shellApp.BrowseForFolder(0, "Choose the base export folder", 1)
        If folder Is Nothing Then Exit Function
        baseFolder = folder.Self.Path
        SaveSetting "SMRI Exporter", "Export", "BaseFolder", baseFolder
    End If

    requestedFolder = JoinPath(baseFolder, Format$(Now, "dd-mm-yy hh-nn"))
    exportFolder = requestedFolder
    copyNumber = 2

    Do While Dir$(exportFolder, vbDirectory) <> ""
        exportFolder = requestedFolder & " (" & CStr(copyNumber) & ")"
        copyNumber = copyNumber + 1
    Loop

    MkDir exportFolder
    ChooseFolder = exportFolder
    Exit Function

FolderError:
    ChooseFolder = ""
End Function

Private Function CleanFilePart(ByVal valueText As String) As String
    Dim invalidChars As Variant
    Dim item As Variant

    invalidChars = Array("\", "/", ":", "*", "?", """", "<", ">", "|")
    valueText = Trim$(valueText)

    For Each item In invalidChars
        valueText = Replace(valueText, CStr(item), "-")
    Next item

    Do While InStr(valueText, "  ") > 0
        valueText = Replace(valueText, "  ", " ")
    Loop

    CleanFilePart = valueText
End Function

Private Function FormatInches(ByVal value As Double) As String
    FormatInches = Trim$(Str$(Round(value, 2)))
End Function

Private Function BuildFileName(ByVal sequenceText As String, ByVal prefix As String, _
    ByVal suffix As String, ByVal widthInches As Double, ByVal heightInches As Double, _
    ByVal extension As String) As String

    Dim fileName As String
    fileName = sequenceText

    If prefix <> "" Then fileName = fileName & " " & prefix

    fileName = fileName & " " & FormatInches(widthInches) & "x" & _
        FormatInches(heightInches) & " Inch"

    If suffix <> "" Then fileName = fileName & " " & suffix

    BuildFileName = fileName & extension
End Function

Private Function JoinPath(ByVal folderPath As String, ByVal fileName As String) As String
    If Right$(folderPath, 1) = "\" Then
        JoinPath = folderPath & fileName
    Else
        JoinPath = folderPath & "\" & fileName
    End If
End Function

Private Function UniqueFilePath(ByVal requestedPath As String) As String
    Dim dotPosition As Long
    Dim basePath As String
    Dim extension As String
    Dim candidate As String
    Dim copyNumber As Long

    If Dir$(requestedPath) = "" Then
        UniqueFilePath = requestedPath
        Exit Function
    End If

    dotPosition = InStrRev(requestedPath, ".")
    If dotPosition > 0 Then
        basePath = Left$(requestedPath, dotPosition - 1)
        extension = Mid$(requestedPath, dotPosition)
    Else
        basePath = requestedPath
        extension = ""
    End If

    copyNumber = 2
    Do
        candidate = basePath & " (" & copyNumber & ")" & extension
        If Dir$(candidate) = "" Then
            UniqueFilePath = candidate
            Exit Function
        End If
        copyNumber = copyNumber + 1
    Loop
End Function

Private Function ExportRasterSelection(ByVal filePath As String, ByVal filterType As cdrFilter, _
    ByVal dpi As Long, ByVal jpegQuality As Long, ByVal formatName As String, _
    ByRef errorText As String) As Boolean

    Dim exportFilter As ExportFilter

    On Error GoTo ExportError

    If formatName = "PNG" Then
        Set exportFilter = ActiveDocument.ExportBitmap(filePath, filterType, cdrSelection, _
            cdrRGBColorImage, 0, 0, dpi, dpi, cdrNormalAntiAliasing, False, True, True)
    ElseIf formatName = "TIFF" Then
        Set exportFilter = ActiveDocument.ExportBitmap(filePath, filterType, cdrSelection, _
            cdrRGBColorImage, 0, 0, dpi, dpi, cdrNormalAntiAliasing, False, False, True, _
            False, cdrCompressionLZW)
    Else
        Set exportFilter = ActiveDocument.ExportBitmap(filePath, filterType, cdrSelection, _
            cdrRGBColorImage, 0, 0, dpi, dpi, cdrNormalAntiAliasing, False, False, True)

        ' Corel's JPG filter uses compression (0 = best), so convert from quality.
        exportFilter.Compression = 100 - jpegQuality
        exportFilter.Smoothing = 0
        exportFilter.Optimized = True
        exportFilter.Progressive = False
    End If

    exportFilter.Finish
    ExportRasterSelection = True
    Exit Function

ExportError:
    errorText = Err.Description
    On Error Resume Next
    If Not exportFilter Is Nothing Then exportFilter.Finish
End Function

Private Function ExportPdfSelection(ByVal filePath As String, ByRef errorText As String) As Boolean
    On Error GoTo ExportError

    ActiveDocument.PDFSettings.PublishRange = pdfSelection
    ActiveDocument.PublishToPDF filePath
    ExportPdfSelection = True
    Exit Function

ExportError:
    errorText = Err.Description
End Function

Public Sub SMRI_ExportSelectedObjects()
    Dim originals As ShapeRange
    Dim currentShape As Shape
    Dim formatName As String
    Dim extension As String
    Dim filterType As cdrFilter
    Dim sequenceStyle As String
    Dim sequenceText As String
    Dim dpi As Long
    Dim jpegQuality As Long
    Dim prefix As String
    Dim suffix As String
    Dim folderPath As String
    Dim fileName As String
    Dim filePath As String
    Dim oldUnit As cdrUnit
    Dim oldPdfRange As pdfExportRange
    Dim pdfRangeSaved As Boolean
    Dim progressStarted As Boolean
    Dim x As Double
    Dim y As Double
    Dim widthInches As Double
    Dim heightInches As Double
    Dim i As Long
    Dim exportedCount As Long
    Dim failedCount As Long
    Dim failedFiles As String
    Dim errorText As String
    Dim exported As Boolean
    Dim summary As String

    If Documents.Count = 0 Then
        MsgBox "Open a CorelDRAW document first.", vbExclamation, EXPORTER_TITLE
        Exit Sub
    End If

    If ActiveSelectionRange.Count = 0 Then
        MsgBox "Select the objects you want to export first.", vbExclamation, EXPORTER_TITLE
        Exit Sub
    End If

    Set originals = ActiveSelectionRange.All

    If Not ChooseSequenceStyle(sequenceStyle) Then Exit Sub
    If Not ChooseExportFormat(formatName, extension, filterType) Then Exit Sub

    dpi = 150
    jpegQuality = 100

    If formatName <> "PDF" Then
        If Not AskForPositiveLong("Enter export DPI:", "150", dpi) Then Exit Sub
    End If

    If formatName = "JPG" Then
        If Not AskForJpegQuality(jpegQuality) Then Exit Sub
    End If

    prefix = CleanFilePart(InputBox("Enter filename prefix (optional):", _
        EXPORTER_TITLE, DocumentBaseName()))
    suffix = CleanFilePart(InputBox("Enter filename suffix (optional):", EXPORTER_TITLE, ""))

    folderPath = ChooseFolder()
    If folderPath = "" Then Exit Sub

    oldUnit = ActiveDocument.Unit
    ActiveDocument.Unit = cdrInch

    If formatName = "PDF" Then
        oldPdfRange = ActiveDocument.PDFSettings.PublishRange
        pdfRangeSaved = True
    End If

    On Error GoTo FatalError

    Application.Status.BeginProgress "Exporting 0/" & originals.Count, True
    progressStarted = True

    For i = 1 To originals.Count
        If Application.Status.Aborted Then Exit For

        Set currentShape = originals(i)
        currentShape.GetBoundingBox x, y, widthInches, heightInches, True

        sequenceText = SequenceLabel(i, sequenceStyle)
        fileName = BuildFileName(sequenceText, prefix, suffix, widthInches, heightInches, extension)
        filePath = UniqueFilePath(JoinPath(folderPath, fileName))

        currentShape.CreateSelection
        errorText = ""

        If formatName = "PDF" Then
            exported = ExportPdfSelection(filePath, errorText)
        Else
            exported = ExportRasterSelection(filePath, filterType, dpi, jpegQuality, formatName, errorText)
        End If

        If exported Then
            exportedCount = exportedCount + 1
        Else
            failedCount = failedCount + 1
            failedFiles = failedFiles & vbCrLf & i & ": " & fileName & " - " & errorText
        End If

        Application.Status.SetProgressMessage i & "/" & originals.Count & " exported"
        Application.Status.Progress = CLng((i * 100) / originals.Count)
        DoEvents
    Next i

CleanUp:
    On Error Resume Next
    If progressStarted Then Application.Status.EndProgress
    If pdfRangeSaved Then ActiveDocument.PDFSettings.PublishRange = oldPdfRange
    ActiveDocument.Unit = oldUnit
    originals.CreateSelection
    On Error GoTo 0

    summary = exportedCount & " of " & originals.Count & " objects exported to:" & vbCrLf & folderPath
    If failedCount > 0 Then
        summary = summary & vbCrLf & vbCrLf & failedCount & " failed:" & failedFiles
    End If
    If exportedCount < originals.Count - failedCount Then
        summary = summary & vbCrLf & vbCrLf & "Export was cancelled."
    End If

    MsgBox summary, IIf(failedCount = 0, vbInformation, vbExclamation), EXPORTER_TITLE
    Exit Sub

FatalError:
    failedCount = failedCount + 1
    failedFiles = failedFiles & vbCrLf & "Unexpected error: " & Err.Description
    Resume CleanUp
End Sub
