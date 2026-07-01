Attribute VB_Name = "SMRI_PanelMaker_Launcher"
Option Explicit

Public Sub SMRI_RunPanelMaker()
    Dim shell As Object
    Set shell = CreateObject("WScript.Shell")
    shell.Run """C:\SMRI\PanelMaker\SMRI.PanelMaker.exe""", 1, False
End Sub
