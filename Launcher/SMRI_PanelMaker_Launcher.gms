Option Explicit

Sub SMRI_RunPanelMaker()
    Dim shell As Object
    Set shell = CreateObject("WScript.Shell")
    shell.Run """C:\SMRI\PanelMaker\SMRI.PanelMaker.exe""", 1, False
End Sub
