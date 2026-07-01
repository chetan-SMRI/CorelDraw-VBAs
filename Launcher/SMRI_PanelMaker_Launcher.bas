Attribute VB_Name = "SMRI_PanelMaker_Launcher"
Option Explicit

Public Sub SMRI_RunPanelMaker()
    Dim shell As Object
    Set shell = CreateObject("WScript.Shell")
    shell.Run """C:\SMRI\PanelMaker\SMRI.PanelMaker.exe""", 1, False
End Sub

Public Sub SMRI_PanelMaker_ButtonHelp()
    MsgBox "To create the toolbar button:" & vbCrLf & vbCrLf & _
        "1. Open Tools > Options > Customization." & vbCrLf & _
        "2. Create or show an SMRI toolbar under Command Bars." & vbCrLf & _
        "3. Open Commands, choose Macros, and drag SMRI_RunPanelMaker onto the toolbar." & vbCrLf & vbCrLf & _
        "The button will launch C:\SMRI\PanelMaker\SMRI.PanelMaker.exe.", _
        vbInformation, "SMRI Panel Maker"
End Sub
